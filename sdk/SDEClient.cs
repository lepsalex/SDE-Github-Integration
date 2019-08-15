using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SDEIntegration.sdk.dto;
using SDEIntegration.sdk.proto;
using SDEIntegration.sdk.events;
using Confluent.Kafka;
using EtcdNet;
using Serilog;

using SDETaskProto = SDEIntegration.sdk.proto.Task;
using SDETaskNoteProto = SDEIntegration.sdk.proto.TaskNote;
using Task = System.Threading.Tasks.Task;

namespace SDEIntegration.sdk
{
    public class SDEClient
    {
        string serviceUrl;
        IConsumer<Ignore, SDETaskProto> taskConsumer;
        IConsumer<Ignore, SDETaskNoteProto> taskNoteConsumer;
        IProducer<Ignore, SDETaskProto> taskProducer;
        IIntegrationClient<SDETask, SDETaskNote> integrationClient;
        EtcdClient etcdClient;
        TaskEvents sdeClientEvents;

        private const string CREATE_TOPIC_NAME = "task-new";
        private const string UPDATE_TOPIC_NAME = "task-update";
        private const string REMOVE_TOPIC_NAME = "task-delete";
        private const string ADD_NOTE_TOPIC_NAME = "task-note-new";

        public SDEClient(IIntegrationClient<SDETask, SDETaskNote> integrationClient)
        {
            this.integrationClient = integrationClient;

            if (integrationClient.GroupId == null)
            {
                throw new MissingFieldException("GroupId must be defined in the IIntegrationClient implementing class!");
            }

            serviceUrl = Environment.GetEnvironmentVariable("SERVICE_URL");
            if (serviceUrl == null)
            {
                Log.Warning("SERVICE_URL is required in env, defaulting to localhost:9092");
                serviceUrl = "localhost:9092";
            }

            ConnectETCD();
            ConnectKafka();
        }

        private void ConnectETCD()
        {
            try
            {
                var etcdOptions = new EtcdClientOpitions()
                {
                    Urls = new string[] { Environment.GetEnvironmentVariable("ETCD_URL") },
                };
                this.etcdClient = new EtcdClient(etcdOptions);

            }
            catch (EtcdGenericException ex)
            {
                Log.Error("Error connecting service to ETCD registry: ", ex.Message);
            }
        }

        private void ConnectKafka()
        {
            var consumerConfig = new ConsumerConfig
            {
                GroupId = integrationClient.GroupId,
                BootstrapServers = serviceUrl,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoOffsetStore = false
            };

            this.taskConsumer = new ConsumerBuilder<Ignore, SDETaskProto>(consumerConfig)
                                    .SetValueDeserializer(new ProtobufDeserializer<SDETaskProto>())
                                    .Build();

            this.taskNoteConsumer = new ConsumerBuilder<Ignore, SDETaskNoteProto>(consumerConfig)
                                        .SetValueDeserializer(new ProtobufDeserializer<SDETaskNoteProto>())
                                        .Build();

            // Use HashSet as we want to ensure no duplicate topic registration
            taskConsumer.Subscribe(new HashSet<string>() {
                CREATE_TOPIC_NAME,
                UPDATE_TOPIC_NAME,
                REMOVE_TOPIC_NAME
            });

            taskNoteConsumer.Subscribe(ADD_NOTE_TOPIC_NAME);

            if (integrationClient.HooksEnabled)
            {
                ConnectHooks();
            }
        }

        private void ConnectHooks()
        {
            // get events component instance (component)
            sdeClientEvents = TaskEvents.Instance;

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = serviceUrl
            };

            this.taskProducer = new ProducerBuilder<Ignore, SDETaskProto>(producerConfig)
                                    .SetValueSerializer(new ProtobufSerializer<SDETaskProto>())
                                    .Build();

            // register callback when events are triggered
            sdeClientEvents.onTaskCreateHook += OnTaskCreateHook;
            sdeClientEvents.onTaskUpdateHook += OnTaskUpdateHook;
            sdeClientEvents.onTaskCreateHook += OnTaskCreateHook;
        }

        public void run()
        {
            Log.Information("SDE Client Start!");

            CancellationTokenSource cts = new CancellationTokenSource();
            var ct = cts.Token;

            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true; // prevent the process from terminating.
                cts.Cancel();
            };

            var tasks = new List<Task>();
            tasks.Add(startTaskConsumer(ct));
            tasks.Add(startTaskNoteConsumer(ct));
            tasks.Add(phoneHome(ct));

            Task.WaitAll(tasks.ToArray());

            // https://github.com/confluentinc/confluent-kafka-dotnet/issues/310
            // When we close our two consumers we have a brief idle period in the
            // main thread which throws a `1/1 brokers are down` error, this is
            // normal and will eventually be changed to INFO instead of ERROR
        }

        private Task startTaskConsumer(CancellationToken ct)
        {
            return Task.Factory.StartNew(async () =>
            {
                Log.Information("Task Consumer listening ...");

                try
                {
                    while (true)
                    {
                        Log.Information("Waiting for Task message ...");

                        try
                        {
                            var taskMsg = taskConsumer.Consume(ct);
                            SDETask sdeTask = null;

                            if (taskMsg.Topic.Equals(CREATE_TOPIC_NAME))
                            {
                                sdeTask = await integrationClient.CreateTask(taskMsg.Value);
                            }
                            else if (taskMsg.Topic.Equals(UPDATE_TOPIC_NAME))
                            {
                                sdeTask = await integrationClient.UpdateTask(taskMsg.Value);
                            }
                            else if (taskMsg.Topic.Equals(REMOVE_TOPIC_NAME))
                            {
                                sdeTask = await integrationClient.RemoveTask(taskMsg.Value);
                            }

                            if (sdeTask == null)
                            {
                                Log.Error($"Could not save issue for message: '{taskMsg.Value}' at: '{taskMsg.TopicPartitionOffset}'");
                                // do something more with this than just error (dead-letter queue?)
                            }

                            taskConsumer.StoreOffset(taskMsg);
                            Log.Information($"Consumed message '{taskMsg.Value}' at: '{taskMsg.TopicPartitionOffset}'.");
                        }
                        catch (ConsumeException e)
                        {
                            Log.Error($"Error occurred: {e.Error.Reason}");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Ensure the consumer leaves the group cleanly and final offsets are committed.
                    taskConsumer.Close();
                    Log.Information("Task Consumer closed!");
                }
            });
        }

        private Task startTaskNoteConsumer(CancellationToken ct)
        {
            return Task.Factory.StartNew(async () =>
            {
                Log.Information("Task Note Consumer listening ...");

                try
                {
                    while (true)
                    {
                        Log.Information("Waiting for Task Note message ...");

                        try
                        {
                            var taskMsg = taskNoteConsumer.Consume(ct);
                            var savedNote = await integrationClient.CreateTaskNote(taskMsg.Value);

                            if (savedNote == null)
                            {
                                Log.Error($"Could not save note for message: '{taskMsg.Value}' at: '{taskMsg.TopicPartitionOffset}'");
                                // do something more with this than just error (dead-letter queue?)
                            }

                            taskNoteConsumer.StoreOffset(taskMsg);
                            Log.Information($"Consumed message '{taskMsg.Value}' at: '{taskMsg.TopicPartitionOffset}'.");
                        }
                        catch (ConsumeException e)
                        {
                            Log.Error($"Error occurred: {e.Error.Reason}");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Ensure the consumer leaves the group cleanly and final offsets are committed.
                    taskNoteConsumer.Close();
                    Log.Information("Task Note Consumer closed!");
                }
            });
        }

        private async Task<Task> phoneHome(CancellationToken ct)
        {
            // Register
            try
            {
                await etcdClient.SetNodeAsync("/_etcd/registry/github", "{\"name\": \"GitHub\", \"type\": \"Issue Tracker\"}");
                Log.Information("ETCD Registration Complete!");
            }
            catch (EtcdGenericException ex)
            {
                Log.Error("Error registering service with ETCD registry: ", ex.Message);
            }

            // Heartbeat
            return Task.Factory.StartNew(async () =>
            {

                const string KEY = "/heartbeat/github";
                const string VAL = "1";
                const int TTL = 10;

                while (!ct.IsCancellationRequested)
                {
                    Log.Information("Heartbeat ...");

                    try
                    {
                        await etcdClient.SetNodeAsync(KEY, VAL, ttl: TTL);
                    }
                    catch (EtcdGenericException ex)
                    {
                        Log.Error("Error sending heartbeat to ETCD registry: ", ex.Message);
                    }

                    await Task.Delay(TTL / 2 * 1000);
                }
            }, ct);
        }

        private void OnTaskCreateHook(SDETask task)
        {

        }

        private void OnTaskUpdateHook(SDETask task)
        {

        }

        private void OnTaskNoteCreateHook(SDETaskNote note)
        {

        }

    }
}
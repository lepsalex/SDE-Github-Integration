using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using EtcdNet;
using SDEIntegration.sdk.dto;
using SDEIntegration.sdk.proto;
using Serilog;

namespace SDEIntegration
{
    public class SDEClient
    {
        ConsumerConfig consumerConfig;
        IConsumer<Ignore, sdk.proto.Task> taskConsumer;
        IConsumer<Ignore, sdk.proto.TaskNote> taskNoteConsumer;
        IIntegrationClient<Task<SDEIssue>, Task<SDENote>> integrationClient;
        EtcdClient etcdClient;

        private const string CREATE_TOPIC_NAME = "task-new";
        private const string UPDATE_TOPIC_NAME = "task-update";
        private const string ADD_NOTE_TOPIC_NAME = "task-note-new";

        public SDEClient(IIntegrationClient<Task<SDEIssue>, Task<SDENote>> integrationClient)
        {
            this.integrationClient = integrationClient;

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
            var serviceUrl = Environment.GetEnvironmentVariable("SERVICE_URL");

            this.consumerConfig = new ConsumerConfig
            {
                GroupId = "github-integration",
                BootstrapServers = serviceUrl != null ? serviceUrl : "localhost:9092",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoOffsetStore = false
            };

            this.taskConsumer = new ConsumerBuilder<Ignore, sdk.proto.Task>(consumerConfig)
                            .SetValueDeserializer(new ProtobufDeserializer<sdk.proto.Task>())
                            .Build();

            this.taskNoteConsumer = new ConsumerBuilder<Ignore, sdk.proto.TaskNote>(consumerConfig)
                             .SetValueDeserializer(new ProtobufDeserializer<sdk.proto.TaskNote>())
                             .Build();

            // Use HashSet as we want to ensure no duplicate topic registration
            taskConsumer.Subscribe(new HashSet<string>() { CREATE_TOPIC_NAME, UPDATE_TOPIC_NAME });
            taskNoteConsumer.Subscribe(ADD_NOTE_TOPIC_NAME);
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

            var tasks = new List<System.Threading.Tasks.Task>();
            tasks.Add(startTaskConsumer(ct));
            tasks.Add(startTaskNoteConsumer(ct));
            tasks.Add(phoneHome(ct));

            System.Threading.Tasks.Task.WaitAll(tasks.ToArray());

            // https://github.com/confluentinc/confluent-kafka-dotnet/issues/310
            // When we close our two consumers we have a brief idle period in the
            // main thread which throws a `1/1 brokers are down` error, this is
            // normal and will eventually be changed to INFO instead of ERROR
        }

        private System.Threading.Tasks.Task startTaskConsumer(CancellationToken ct)
        {
            return System.Threading.Tasks.Task.Factory.StartNew(async () =>
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
                            SDEIssue savedIssue = null;

                            if (taskMsg.Topic.Equals(CREATE_TOPIC_NAME))
                            {
                                savedIssue = await integrationClient.CreateIssue(taskMsg.Value);
                            }
                            else if (taskMsg.Topic.Equals(UPDATE_TOPIC_NAME))
                            {
                                savedIssue = await integrationClient.UpdateIssue(taskMsg.Value);
                            }

                            if (savedIssue == null)
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

        private System.Threading.Tasks.Task startTaskNoteConsumer(CancellationToken ct)
        {
            return System.Threading.Tasks.Task.Factory.StartNew(async () =>
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
                            var savedNote = await integrationClient.CreateIssueNote(taskMsg.Value);

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

        private async System.Threading.Tasks.Task<System.Threading.Tasks.Task> phoneHome(CancellationToken ct)
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
            return System.Threading.Tasks.Task.Factory.StartNew(async () =>
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

                    await System.Threading.Tasks.Task.Delay(TTL / 2 * 1000);
                }
            }, ct);
        }
    }
}
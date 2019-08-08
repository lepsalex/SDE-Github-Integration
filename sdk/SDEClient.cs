using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
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

        private const string CREATE_TOPIC_NAME = "task-new";
        private const string UPDATE_TOPIC_NAME = "task-update";
        private const string ADD_NOTE_TOPIC_NAME = "task-note-new";

        public SDEClient(IIntegrationClient<Task<SDEIssue>, Task<SDENote>> integrationClient)
        {
            this.integrationClient = integrationClient;

            var serviceUrl = Environment.GetEnvironmentVariable("SERVICE_URL");

            consumerConfig = new ConsumerConfig
            {
                GroupId = "github-integration",
                BootstrapServers = serviceUrl != null ? serviceUrl : "localhost:9092",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            taskConsumer = new ConsumerBuilder<Ignore, sdk.proto.Task>(consumerConfig)
                            .SetValueDeserializer(new ProtobufDeserializer<sdk.proto.Task>())
                            .Build();

            taskNoteConsumer = new ConsumerBuilder<Ignore, sdk.proto.TaskNote>(consumerConfig)
                            .SetValueDeserializer(new ProtobufDeserializer<sdk.proto.TaskNote>())
                            .Build();

            taskConsumer.Subscribe(new List<string>() { CREATE_TOPIC_NAME, UPDATE_TOPIC_NAME });
            taskNoteConsumer.Subscribe(ADD_NOTE_TOPIC_NAME);
        }

        public void run()
        {
            Log.Information("SDE Client Start!");

            CancellationTokenSource cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true; // prevent the process from terminating.
                cts.Cancel();
            };

            var consumers = new System.Threading.Tasks.Task[] { startTaskConsumer(cts), startTaskNoteConsumer(cts) };

            System.Threading.Tasks.Task.WaitAll(consumers);
        }

        private System.Threading.Tasks.Task startTaskConsumer(CancellationTokenSource cts)
        {
            Log.Information("Task Consumer listening ...");

            return System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    while (true)
                    {
                        Log.Information("Waiting for Task message ...");

                        try
                        {
                            var taskMsg = taskConsumer.Consume(cts.Token);

                            if (taskMsg.Topic.Equals(CREATE_TOPIC_NAME))
                            {
                                integrationClient.CreateIssue(taskMsg.Value);
                            }
                            else if (taskMsg.Topic.Equals(UPDATE_TOPIC_NAME))
                            {
                                integrationClient.UpdateIssue(taskMsg.Value);
                            }

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

        private System.Threading.Tasks.Task startTaskNoteConsumer(CancellationTokenSource cts)
        {
            Log.Information("Task Note Consumer listening ...");

            return System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    while (true)
                    {
                        Log.Information("Waiting for Task Note message ...");

                        try
                        {
                            var taskMsg = taskNoteConsumer.Consume(cts.Token);
                            integrationClient.CreateIssueNote(taskMsg.Value);
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
    }
}
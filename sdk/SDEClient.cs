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
        IIntegrationClient<Task<SDEIssue>, Task<SDENote>> integrationClient;

        private const string CREATE_TOPIC_NAME = "task-new";
        private const string UPDATE_TOPIC_NAME = "task-update";
        private const string ADD_NOTE_TOPIC_NAME = "task-note-new";

        public SDEClient(IIntegrationClient<Task<SDEIssue>, Task<SDENote>> integrationClient)
        {
            this.integrationClient = integrationClient;

            var serviceUrl = Environment.GetEnvironmentVariable("SERVICE_URL");

            consumerConfig = new ConsumerConfig {
              GroupId = "github-integration",
              BootstrapServers = serviceUrl != null ? serviceUrl : "localhost:9092",
              AutoOffsetReset = AutoOffsetReset.Earliest
            };

            taskConsumer = new ConsumerBuilder<Ignore, sdk.proto.Task>(consumerConfig)
                            .SetValueDeserializer(new ProtobufDeserializer<sdk.proto.Task>())
                            .Build();
            taskConsumer.Subscribe(new List<string>(){CREATE_TOPIC_NAME, UPDATE_TOPIC_NAME});
        }

        public void run()
        {
            Log.Information("SDE Client Start ...");

            CancellationTokenSource cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => {
                e.Cancel = true; // prevent the process from terminating.
                cts.Cancel();
            };

            try
            {
                while (true)
                {
                    try
                    {
                        var taskMsg = taskConsumer.Consume(cts.Token);

                        if (taskMsg.Topic.Equals(CREATE_TOPIC_NAME)) {
                            integrationClient.CreateIssue(taskMsg.Value);
                        } else if (taskMsg.Topic.Equals(UPDATE_TOPIC_NAME)) {
                            integrationClient.UpdateIssue(taskMsg.Value);
                        } else if (taskMsg.Topic.Equals(ADD_NOTE_TOPIC_NAME)) {
                            integrationClient.CreateIssueNote(taskMsg.Value);
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
            }
        }

        private System.Threading.Tasks.Task startConsumer(List<string> topics, ProtobufDeserializer<?> deserializer) {

        }
    }
}
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
        IConsumer<int, sdk.proto.Task> taskConsumer;
        IConsumer<int, sdk.proto.Task> updateConsumer;
        IIntegrationClient<Task<SDEIssue>> integrationClient;

        private const string CREATE_TOPIC_NAME = "task-new";
        private const string UPDATE_TOPIC_NAME = "task-update";

        public SDEClient(IIntegrationClient<Task<SDEIssue>> integrationClient)
        {
            this.integrationClient = integrationClient;

            var serviceUrl = Environment.GetEnvironmentVariable("SERVICE_URL");

            consumerConfig = new ConsumerConfig {
              GroupId = "github-integration",
              BootstrapServers = serviceUrl != null ? serviceUrl : "localhost:9092",
              AutoOffsetReset = AutoOffsetReset.Earliest
            };

            taskConsumer = new ConsumerBuilder<int, sdk.proto.Task>(consumerConfig)
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
                        var cr = taskConsumer.Consume(cts.Token);
                        Log.Information($"Consumed message '{cr.Value}' at: '{cr.TopicPartitionOffset}'.");
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
    }
}
using System;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace custom_skills
{
    public class SignalRMonitor
    {
        private readonly ILogger _logger;

        public SignalRMonitor(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SignalRMonitor>();
        }

        [Function("SignalRMonitor")]
        public void Run([CosmosDBTrigger(
            databaseName: "deid",
            containerName: "metadata",
            Connection = "CosmosDbConnection",
            LeaseContainerName = "leases",
            CreateLeaseContainerIfNotExists = true)] IReadOnlyList<MyDocument> input)
        {
            if (input != null && input.Count > 0)
            {
                _logger.LogInformation("Documents modified: " + input.Count);
                _logger.LogInformation("First document Id: " + input[0].id);
            }
        }
    }

    public class MyDocument
    {
        public string id { get; set; }

        public string Text { get; set; }

        public int Number { get; set; }

        public bool Boolean { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Ruuvi.Hot.Functions.Functions
{
    public class IsOpenFunction
    {
        private readonly IConfiguration _config;

        public IsOpenFunction(IConfiguration config)
        {
            _config = config;
        }

        [FunctionName("IsOpenFunction")]
        public async Task Run([EventHubTrigger("%EventHubName%", Connection = "EventHubConnection")] EventData[] events, ILogger log)
        {
            var sqlConnectionString = _config.GetConnectionString("RuuviDatabase");

            var exceptions = new List<Exception>();
            foreach (EventData eventData in events)
            {
                try
                {
                    string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);

                    JObject jObject = JObject.Parse(messageBody);
                    bool movement = jObject["ruuvi_mqtt_movement_delta"].Value<int>() > 0;

                    if (movement)
                    {
                        using (SqlConnection connection = new SqlConnection(sqlConnectionString))
                        {
                            var query = "UPDATE [dbo].[DoorTable] SET IsOpen = 1 ^ (SELECT TOP 1 IsOpen FROM [dbo].[DoorTable])";
                            using (SqlCommand command = new SqlCommand(query, connection))
                            {
                                connection.Open();
                                int result = command.ExecuteNonQuery();

                                // Check Error
                                if (result < 0)
                                {
                                    Console.WriteLine("Error updating movement status to database!");
                                }
                            }
                        }
                    }

                    await Task.Yield();
                }
                catch (Exception e)
                {
                    // We need to keep processing the rest of the batch - capture this exception and continue.
                    // Also, consider capturing details of the message that failed processing so it can be processed again later.
                    exceptions.Add(e);
                }
            }

            // Once processing of the batch is complete, if any messages in the batch failed processing throw an exception so that there is a record of the failure.

            if (exceptions.Count > 1)
                throw new AggregateException(exceptions);

            if (exceptions.Count == 1)
                throw exceptions.Single();
        }
    }
}

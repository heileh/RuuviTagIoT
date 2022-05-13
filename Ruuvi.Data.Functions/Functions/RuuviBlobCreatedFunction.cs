using System;
using System.Data.SqlClient;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Ruuvi.Data.Functions.Misc;
using Ruuvi.Data.Functions.Models;

namespace Ruuvi.Data.Functions.Functions
{
    public class RuuviBlobCreatedFunction
    {
        private readonly IConfiguration _config;

        public RuuviBlobCreatedFunction(IConfiguration config)
        {
            _config = config;
        }

        [FunctionName("RuuviBlobCreated")]
        public void Run([BlobTrigger("%RuuviContainerName%/{name}", Connection = "RuuviStorageAccount")] Stream myBlob, string name, ILogger log)
        {
            if (!name.EndsWith(".json"))
            {
                return;
            }

            string blobContent;
            using (StreamReader reader = new StreamReader(myBlob))
            {
                blobContent = reader.ReadToEnd();
            } 

            var measurementJsons = blobContent.Split(Environment.NewLine);

            foreach (var json in measurementJsons)
            {
                JObject jObject = JObject.Parse(json);

                var m = new Measurement()
                {
                    Name = jObject["Body"]["ruuvi_mqtt_name"].Value<string>(),
                    Time = jObject["Body"]["ruuvi_mqtt_timestamp"].Value<long>().FromUnixTimeToDateTimeUtc(),
                    Movement = jObject["Body"]["ruuvi_mqtt_movement_delta"].Value<int>() > 0,
                    Temperature = jObject["Body"]["temperature"].Value<double>(),

                };
                

                var sqlConnectionString = _config.GetConnectionString("RuuviDatabase");
                using (SqlConnection connection = new SqlConnection(sqlConnectionString))
                {
                    var query = "INSERT INTO dbo.RuuviTable (Name,Time,Movement,Temperature) VALUES (@Name,@Time,@Movement, @Temperature)";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Name", m.Name);
                        command.Parameters.AddWithValue("@Time", m.Time);
                        command.Parameters.AddWithValue("@Movement", m.Movement);
                        command.Parameters.AddWithValue("@Temperature", m.Temperature);

                        connection.Open();
                        int result = command.ExecuteNonQuery();

                        // Check Error
                        if (result < 0)
                        {
                            Console.WriteLine("Error inserting data into Database!");
                        }
                    }
                }
            }
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
        }

    }
}

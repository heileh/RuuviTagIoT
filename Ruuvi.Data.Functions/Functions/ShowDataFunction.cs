using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Ruuvi.Data.Functions.Misc;
using Ruuvi.Data.Functions.Models;

namespace Ruuvi.Data.Functions.Functions
{
    public class ShowDataFunction
    {

        private readonly IConfiguration _config;

        public ShowDataFunction(IConfiguration config)
        {
            _config = config;
        }
        
        /// <summary>
        /// Test function to show all measurements from container 
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("ShowData")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            // Get a connection string to our Azure Storage account.
            string storageAccountConnection = _config["RuuviStorageAccount"];
            string containerName = _config["RuuviContainerName"];

            // Get a reference to a container named "sample-container" and then create it
            BlobContainerClient container = new BlobContainerClient(storageAccountConnection, containerName);

            var measurements = new List<Measurement>();

            // Print out all the blob names
            foreach (BlobItem blobItem in container.GetBlobs())
            {
                if (!blobItem.Name.EndsWith(".json"))
                {
                    continue;
                }

                BlobClient blobClient = container.GetBlobClient(blobItem.Name);

                var blobDownloadResult = blobClient.DownloadContent();

                var blobBytes = blobDownloadResult.Value.Content.ToArray();
                string blobContent = System.Text.Encoding.UTF8.GetString(blobBytes);

                var measurementJsons = blobContent.Split(Environment.NewLine);

                foreach (var json in measurementJsons)
                {
                    JObject jObject = JObject.Parse(json);

                    measurements.Add(new Measurement()
                    {
                        Name = jObject["Body"]["ruuvi_mqtt_name"].Value<string>(),
                        Time = jObject["Body"]["ruuvi_mqtt_timestamp"].Value<long>().FromUnixTimeToDateTimeUtc(),
                        Movement = jObject["Body"]["ruuvi_mqtt_movement_delta"].Value<int>() > 0,
                        Temperature = jObject["Body"]["temperature"].Value<double>(),

                    });
                }
            }

            return new OkObjectResult(measurements);
        }

    }
}

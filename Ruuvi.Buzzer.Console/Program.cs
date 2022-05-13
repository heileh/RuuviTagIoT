// See https://aka.ms/new-console-template for more information

using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>()
    .Build();

var sqlConnectionString = config.GetConnectionString("RuuviDatabase");

bool isOpen = false;

while (true)
{

    using (SqlConnection connection = new SqlConnection(sqlConnectionString))
    {
        var query = "SELECT TOP 1 IsOpen FROM [dbo].[DoorTable]";
        using (SqlCommand command = new SqlCommand(query, connection))
        {
            connection.Open();
            using (SqlDataReader reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    isOpen = reader.GetBoolean(0);
                }
            }
        }
    }
    
    if (isOpen)
    {
        // Beep at 5000 Hz for 1 second, in a separate thread
        new Thread(() => Console.Beep(2000, 1000)).Start();
    }
    
    // Sleep 1s
    Thread.Sleep(1000);

}

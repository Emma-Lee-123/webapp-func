using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Data.SqlClient;

namespace Company.Function
{
    public static class HttpUser
    {
        [FunctionName("HttpGetUser")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
            var email = string.Empty;
            log.LogInformation($"Connection string: {connectionString}");
            
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string query = "SELECT email FROM Users WHERE name = @name";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", name);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                email = reader["email"].ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Database query failed: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
            
            if (string.IsNullOrEmpty(email))
            {
                return new NotFoundObjectResult($"User with name {name} not found.");
            }

            // string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            // dynamic data = JsonConvert.DeserializeObject(requestBody);
            // name = name ?? data?.name;

            // string responseMessage = string.IsNullOrEmpty(name)
            //     ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
            //     : $"Hello, {name}. This HTTP triggered function executed successfully.";
            string responseMessage = $"{{\"email\":\"{email}\"}}";//"{\"email\":" + "\"" + email + "\"}";
            return new OkObjectResult(responseMessage);
        }
    }
}

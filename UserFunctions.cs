
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using System.Runtime.Serialization;
using System;

namespace Company.Function
{
    public static class UserFunctions
    {
        [FunctionName("AddUser")]
        public static async Task<IActionResult> Add(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "users/add")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("AddUser started.");

            string connectionString = Util.GetConnectionString();
            if (string.IsNullOrEmpty(connectionString))
            {
                return new BadRequestObjectResult("Connection string is missing.");
            }

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            log.LogInformation("requestBody: " + requestBody);
            var user = JsonSerializer.Deserialize<User>(requestBody);
            
            var name = user?.Name;
            string email = user?.Email;
            string password = user?.Password;

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                return new BadRequestObjectResult("Name, Email and Password are required.");
            }

            bool userExists = await Util.UserExistsAsync(email);
            if (userExists)
            {
                return new ConflictObjectResult($"User {email} already exists.");
            }
            
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                string query = "INSERT INTO Users (name, email, password) VALUES (@Name, @Email, @Password); SELECT SCOPE_IDENTITY();";
                log.LogInformation($"Query: {query}");
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", name);
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@Password", password);

                    var result = await cmd.ExecuteScalarAsync();
                    user.Id = Convert.ToInt32(result);
//                    await cmd.ExecuteNonQueryAsync();
                }
            }

            if (user.Id == 0)
            {
                return new BadRequestObjectResult($"User {name} with email {email} not added.");
            }
        
            return new OkObjectResult(user);
        }

        [FunctionName("IsAuthenticated")]
        public static async Task<IActionResult> IsAuthenticated(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "users")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("IsAuthenticated started.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var userParameter = JsonSerializer.Deserialize<User>(requestBody);
            string email = userParameter?.Email;
            string password = userParameter?.Password;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                return new BadRequestObjectResult("Email and Password are required.");
            }
            log.LogInformation($"email:{email}; password:{password}");

            var user = await GetUserAsync(email, password);
            if (user == null)
            {
                return new NotFoundObjectResult($"User {email} not found or invalid credentials.");
            }

            return new OkObjectResult(user);
        }

        [FunctionName("UserExisits")]
        public static async Task<IActionResult> UserExistsAsync([HttpTrigger(AuthorizationLevel.Function, "get", Route = "users/{email}")] HttpRequest req,
        string email, ILogger log)
        {
            log.LogInformation($"UserExistsAsync started. email-{email}");

            string connectionString = Util.GetConnectionString();
            if (string.IsNullOrEmpty(connectionString))
            {
                return new BadRequestObjectResult("Connection string is missing.");
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                string query = "SELECT 1 FROM Users WHERE email = @Email";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    int count = (int)await cmd.ExecuteScalarAsync();
                    return new OkObjectResult(count > 0);
                }
            }
        }

        private static async Task<User> GetUserAsync(string email, string password)
        {
            string connectionString = Util.GetConnectionString();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                string query = "SELECT id, name, email, password FROM Users WHERE email = @Email AND password = @Password";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@Password", password);
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new User
                            {
                                Id = Convert.ToInt32(reader["id"]),
                                Name = reader["name"].ToString(),
                                Email = reader["email"].ToString(),
                                Password = reader["password"].ToString()
                            };
                        }
                    }
                }
            }
            return null;
        }
    }
}

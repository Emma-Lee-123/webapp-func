using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

namespace Company.Function
{
    public static class TaskFunctions
    {
        [FunctionName("GetTasksByEmail")]
        public static async Task<IActionResult> GetByEmail(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "tasks/{email}")] HttpRequest req, string email,
            ILogger log)
        {
            log.LogInformation($"GetTasks started. for user email {email}");
            if (string.IsNullOrEmpty(email))
            {
                return new BadRequestObjectResult("Email is required.");
            }
            var user = new User();

            // string email = req.Query["email"];
            string connectionString = Util.GetConnectionString();
            if (string.IsNullOrEmpty(connectionString))
            {
                return new BadRequestObjectResult("Connection string is missing.");
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                string query = "SELECT u.id userid, u.name userName, t.id taskid, t.category, t.name taskName, t.completed, createdat, modifiedat FROM Users u join tasks t on u.id = t.userid WHERE u.email = @Email";
                log.LogInformation($"Query: {query}");
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            if(string.IsNullOrEmpty(user.Name)) // Only set user details once
                            {
                                user.Id = Convert.ToInt32(reader["userid"]);
                                user.Name = reader["username"].ToString();
                            }

                            user.Tasks.Add(new Company.Function.Task
                            {
                                Id = Convert.ToInt32(reader["taskid"]),
                                Category = reader["category"].ToString(),
                                Name = reader["taskname"].ToString(),
                                Completed = Convert.ToBoolean(reader["completed"]),
                                CreatedAt = reader["createdat"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["createdat"]),
                                ModifiedAt = reader["modifiedat"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["modifiedat"])
                            });
                        }
                    }
                }
            }

            if(string.IsNullOrEmpty(user.Name))
            {
                return new NotFoundObjectResult($"User with email {email} not found.");
            }   

            user.Email = email;
            return new OkObjectResult(user);
        }

        [FunctionName("GetTasksByUser")]
        public static async Task<IActionResult> GetByUser(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "tasks/{userid}")] HttpRequest req, int userid,
            ILogger log)
        {
            log.LogInformation($"GetTaskById started. for user id {userid}");

            string connectionString = Util.GetConnectionString();
            if (string.IsNullOrEmpty(connectionString))
            {
                return new BadRequestObjectResult("Connection string is missing.");
            }
            var tasks = new List<Company.Function.Task>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                string query = "SELECT t.id, t.category, t.name, t.completed, createdat, modifiedat FROM tasks t WHERE t.id = @userid";
                log.LogInformation($"Query: {query}");
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@userid", userid);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            tasks.Add(new Company.Function.Task
                            {
                                Id = Convert.ToInt32(reader["id"]),
                                Category = reader["category"].ToString(),
                                Name = reader["name"].ToString(),
                                Completed = Convert.ToBoolean(reader["completed"]),
                                CreatedAt = reader["createdat"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["createdat"]),
                                ModifiedAt = reader["modifiedat"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["modifiedat"])
                            });
                        }
                    }
                }
            }

            if(tasks.Count > 0)
            {
                return new OkObjectResult(tasks);
            }
            return new NotFoundObjectResult($"Task with userid {userid} not found.");
        }

        [FunctionName("AddTask")]
        public static async Task<IActionResult> Add(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "tasks")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("PostTasks started.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var task = JsonSerializer.Deserialize<Company.Function.Task>(requestBody);
            
            var name = task?.Name;
            string category = task?.Category;
            var userId = task?.UserId;

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(category) || userId == null || userId.Value == 0)
            {
                return new BadRequestObjectResult("Task Name, Category and UserId are required.");
            }
            log.LogInformation($"AddTask - name:{name}; category:{category}; userId:{userId}");

            var connectionString = Util.GetConnectionString();
            if (string.IsNullOrEmpty(connectionString))
            {
                return new BadRequestObjectResult("Connection string is missing.");
            }

            var userExists = await Util.UserExistsByIdAsync(userId.Value);
            if (!userExists)
            {
                return new NotFoundObjectResult($"User with id {userId} not found.");
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                string query = "INSERT INTO tasks (name, category, userid) VALUES (@Name, @Category, @UserId)";
                log.LogInformation($"Query: {query}");
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", name);
                    cmd.Parameters.AddWithValue("@Category", category);
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        
            return new OkObjectResult($"Task {name} added.");
        }

        [FunctionName("UpdateTask")]
        public static async Task<IActionResult> Update(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "tasks")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation($"UpdateTask started.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var task = JsonSerializer.Deserialize<Company.Function.Task>(requestBody);
            
            var id = task?.Id;
            if (id == null || id == 0)
            {
                return new BadRequestObjectResult("Task Id is required.");
            }
            var name = task?.Name;
            string category = task?.Category;
            var userId = task?.UserId;
            bool completed = task?.Completed ?? false;

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(category) || userId==null || userId.Value == 0)
            {
                return new BadRequestObjectResult("Task Name, Category and UserId are required.");
            }
            log.LogInformation($"UpdateTask - id:{id}; name:{name}; category:{category}; userId:{userId}; completed:{completed.ToString()}");

            var connectionString = Util.GetConnectionString();
            if (string.IsNullOrEmpty(connectionString))
            {
                return new BadRequestObjectResult("Connection string is missing.");
            }

            int affectedRows;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                string query = "UPDATE tasks SET name=@Name, category=@Category, completed=@Completed WHERE id=@Id AND userid=@UserId";
                log.LogInformation($"Query: {query}");
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", name);
                    cmd.Parameters.AddWithValue("@Category", category);
                    cmd.Parameters.AddWithValue("@Completed", completed);
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    affectedRows = await cmd.ExecuteNonQueryAsync();
                }
            }
            if (affectedRows == 0)
            {
                return new NotFoundObjectResult($"Task with id {id} not found or not owned by user {userId}.");
            }
            return new OkObjectResult($"Task {name} updated.");
        }

        [FunctionName ("DeleteTask")]
        public static async Task<IActionResult> Delete(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "tasks/{id}")] HttpRequest req, int id,
            ILogger log)
        {
            log.LogInformation($"DeleteTask started. for task id {id}");

            string connectionString = Util.GetConnectionString();
            if (string.IsNullOrEmpty(connectionString))
            {
                return new BadRequestObjectResult("Connection string is missing.");
            }

            int affectedRows;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                string query = "DELETE FROM tasks WHERE id=@Id";
                log.LogInformation($"Query: {query}");
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);

                    affectedRows = await cmd.ExecuteNonQueryAsync();
                }
            }
            if (affectedRows == 0)
            {
                return new NotFoundObjectResult($"Task with id {id} not found.");
            }
            return new OkObjectResult($"Task with id {id} deleted.");
        }
    }
}

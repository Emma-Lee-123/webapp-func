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
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "tasks/email")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation($"GetTasks started.");
            var email = req.Query["email"];
            log.LogInformation($"GetTasks - email: {email}");
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
                string query = "SELECT u.id userid, u.name userName, t.id taskid, t.category, t.name taskName, t.completed, t.createdat, t.modifiedat FROM Users u join tasks t on u.id = t.userid WHERE u.email = @Email";
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
        public static async Task<IActionResult> GetByUserId(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "tasks")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation($"GetTasksByUser started.");

            string userId = req.Query["userId"];

            if (string.IsNullOrEmpty(userId))
            {
                return new BadRequestObjectResult("Missing userId.");
            }
            if (!int.TryParse(userId, out int userid))
            {
                return new BadRequestObjectResult("Invalid userId.");
            }
            log.LogInformation($"GetTasksByUser started. for user id {userid}");

            string connectionString = Util.GetConnectionString();
            if (string.IsNullOrEmpty(connectionString))
            {
                return new BadRequestObjectResult("Connection string is missing.");
            }
            var tasks = new List<Company.Function.Task>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                string query = "SELECT id, category, name, completed, createdat, modifiedat FROM tasks WHERE userid = @userid";
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
                                ModifiedAt = reader["modifiedat"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["modifiedat"]),
                                UserId = userid
                            });
                        }
                    }
                }
            }

            log.LogInformation($"GetTasksByUser - tasks count: {tasks.Count}");

            return new OkObjectResult(tasks);
        }


        // [FunctionName("GetTasksByUserId")]
        // public static async Task<IActionResult> GetByUser(
        //     [HttpTrigger(AuthorizationLevel.Function, "get", Route = "tasks/{userid}")] HttpRequest req, int userid,
        //     ILogger log)
        // {
        //     log.LogInformation($"GetTasksByUser started. for user id {userid}");

        //     string connectionString = Util.GetConnectionString();
        //     if (string.IsNullOrEmpty(connectionString))
        //     {
        //         return new BadRequestObjectResult("Connection string is missing.");
        //     }
        //     var tasks = new List<Company.Function.Task>();

        //     using (SqlConnection conn = new SqlConnection(connectionString))
        //     {
        //         await conn.OpenAsync();
        //         string query = "SELECT t.id, t.category, t.name, t.completed, createdat, modifiedat FROM tasks t WHERE t.userid = @userid";
        //         log.LogInformation($"Query: {query}");
        //         using (SqlCommand cmd = new SqlCommand(query, conn))
        //         {
        //             cmd.Parameters.AddWithValue("@userid", userid);

        //             using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
        //             {
        //                 while (await reader.ReadAsync())
        //                 {
        //                     tasks.Add(new Company.Function.Task
        //                     {
        //                         Id = Convert.ToInt32(reader["id"]),
        //                         Category = reader["category"].ToString(),
        //                         Name = reader["name"].ToString(),
        //                         Completed = Convert.ToBoolean(reader["completed"]),
        //                         CreatedAt = reader["createdat"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["createdat"]),
        //                         ModifiedAt = reader["modifiedat"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["modifiedat"]),
        //                         UserId = userid
        //                     });
        //                 }
        //             }
        //         }
        //     }

        //     log.LogInformation($"GetTasksByUser - tasks count: {tasks.Count}");
        //     if(tasks.Count > 0)
        //     {
        //         return new OkObjectResult(tasks);
        //     }
        //     return new NotFoundObjectResult($"Task with userid {userid} not found.");
        // }

        [FunctionName("AddTask")]
        public static async Task<IActionResult> Add(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "tasks")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("AddTask started.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var task = JsonSerializer.Deserialize<Company.Function.Task>(requestBody, Util.JsonSerializerOptions());
            
            var name = task?.Name;
            string category = task?.Category;
            var userId = task?.UserId;
            log.LogInformation($"AddTask - name:{name}; category:{category}; userId:{userId}");
            
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(category) || userId == null || userId.Value == 0)
            {
                return new BadRequestObjectResult("Task Name, Category and UserId are required.");
            }
            
            var connectionString = Util.GetConnectionString();
            if (string.IsNullOrEmpty(connectionString))
            {
                return new BadRequestObjectResult("Connection string is missing.");
            }

            var userExists = await Util.UserExistsByIdAsync(log, userId.Value);
            if (!userExists)
            {
                return new NotFoundObjectResult($"User with id {userId} not found.");
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                string query = "INSERT INTO tasks (name, category, userid) VALUES (@Name, @Category, @UserId); SELECT SCOPE_IDENTITY();";
                log.LogInformation($"Query: {query}");
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", name);
                    cmd.Parameters.AddWithValue("@Category", category);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    var result = await cmd.ExecuteScalarAsync();
                    task.Id = Convert.ToInt32(result);
                    // await cmd.ExecuteNonQueryAsync();
                }
            }

            if (task.Id == 0)
            {
                log.LogInformation($"Task {name} not added.");
                return new BadRequestObjectResult($"Task {name} not added.");
            }
            return new OkObjectResult(task);
        }

        [FunctionName("UpdateTask")]
        public static async Task<IActionResult> Update(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "tasks")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation($"UpdateTask started.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var task = JsonSerializer.Deserialize<Company.Function.Task>(requestBody, Util.JsonSerializerOptions());
            log.LogInformation($"UpdateTask - requestBody: {requestBody}");
            
            var id = task?.Id;
            var userId = task?.UserId;
            var name = task?.Name;
            string category = task?.Category;
            //bool completed = task?.Completed ?? false;
            bool? comletedNullable = task?.Completed;
            log.LogInformation($"UpdateTask - id:{id}; name:{name}; category:{category}; userId:{userId}; completed:{comletedNullable.Value.ToString()}");
            
            if (id == null || id == 0 || userId == null || userId.Value == 0)
            {
                return new BadRequestObjectResult("Task id and userId are required.");
            }
            if(string.IsNullOrEmpty(name) && string.IsNullOrEmpty(category) && comletedNullable == null)
            {
                return new BadRequestObjectResult("At least one of Task Name, Category or Completed is required.");
            }

            var connectionString = Util.GetConnectionString();
            if (string.IsNullOrEmpty(connectionString))
            {
                return new BadRequestObjectResult("Connection string is missing.");
            }

            int affectedRows;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                string query = "UPDATE tasks SET ";//name=@Name, category=@Category, completed=@Completed WHERE id=@Id AND userid=@UserId; select @@ROWCOUNT;";
                if(!string.IsNullOrEmpty(name))
                {
                    query = query + " name=@Name,";
                }
                if(!string.IsNullOrEmpty(category))
                {
                    query = query + " category=@Category,";
                }
                if(comletedNullable != null)
                {
                    query = query + " completed=@Completed,";
                }
                query = query.TrimEnd(',');
                query = query + " WHERE id=@Id AND userid=@UserId; select @@ROWCOUNT;";

                log.LogInformation($"Query: {query}");
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if(!string.IsNullOrEmpty(name))
                    {
                        cmd.Parameters.AddWithValue("@Name", name);
                    }
                    if(!string.IsNullOrEmpty(category))
                    {
                        cmd.Parameters.AddWithValue("@Category", category);
                    }
                    if(comletedNullable != null)
                    {
                        cmd.Parameters.AddWithValue("@Completed", comletedNullable.Value);
                    }
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    //affectedRows = await cmd.ExecuteNonQueryAsync();
                    var result = await cmd.ExecuteScalarAsync();
                    affectedRows = Convert.ToInt32(result);
                }
            }
            if (affectedRows == 0)
            {
                return new NotFoundObjectResult($"Task with id {id} not found or not owned by user {userId}.");
            }
            return new OkObjectResult($"Task {name} updated.");
        }

        [FunctionName("SetTaskCompleted")]
        public static async Task<IActionResult> SetCompleted(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "tasks/id")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation($"SetTaskCompleted started.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var task = JsonSerializer.Deserialize<Company.Function.Task>(requestBody, Util.JsonSerializerOptions());
            var id = task.Id;
            var completed = task?.Completed;
            log.LogInformation($"SetTaskCompleted - requestBody: {requestBody}; completed: {completed.ToString()}");

            if (completed == null)
            {
                return new BadRequestObjectResult("Task Completed is required.");
            }

            string connectionString = Util.GetConnectionString();
            if (string.IsNullOrEmpty(connectionString))
            {
                return new BadRequestObjectResult("Connection string is missing.");
            }

            int affectedRows;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                string query = "UPDATE tasks SET completed=@Completed WHERE id=@Id; select @@ROWCOUNT;";
                log.LogInformation($"Query: {query}");
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@Completed", completed.Value);

                    //affectedRows = await cmd.ExecuteNonQueryAsync();
                    var result = await cmd.ExecuteScalarAsync();
                    affectedRows = Convert.ToInt32(result);
                }
            }
            if (affectedRows == 0)
            {
                return new NotFoundObjectResult($"Task with id {id} not found.");
            }
            return new OkObjectResult($"Task with id {id} updated to completed {completed}.");
        }

        [FunctionName ("DeleteTask")]
        public static async Task<IActionResult> Delete(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "tasks/id")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation($"DeleteTask started.");
            string id = req.Query["id"];
            string connectionString = Util.GetConnectionString();
            if (string.IsNullOrEmpty(connectionString))
            {
                return new BadRequestObjectResult("Connection string is missing.");
            }

            int affectedRows;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                string query = "DELETE FROM tasks WHERE id=@Id; select @@ROWCOUNT;";
                log.LogInformation($"Query: {query}");
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);

                    //affectedRows = await cmd.ExecuteNonQueryAsync();
                    var result = await cmd.ExecuteScalarAsync();
                    affectedRows = Convert.ToInt32(result);
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

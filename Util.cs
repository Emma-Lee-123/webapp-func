using System;
using System.Net.NetworkInformation;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;

namespace Company.Function
{
    public static class Util
    {
        public static string GetConnectionString()
        {
            return Environment.GetEnvironmentVariable("SqlConnectionString");
        }

        public static async System.Threading.Tasks.Task<bool> UserExistsAsync(ILogger log, string email)
        {
            string connectionString = Util.GetConnectionString();
            if (string.IsNullOrEmpty(connectionString))
            {
                return false;
            }
            log.LogInformation($"Util.UserExistsAsync-Connection string: {connectionString}");
            log.LogInformation($"Util.UserExistsAsync-Email: {email}");
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                string query = "SELECT 1 FROM Users WHERE email = @Email";
                log.LogInformation($"Util.UserExistsAsync-Query: {query}");
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    //int count = (int)await cmd.ExecuteScalarAsync();
                    var result = await cmd.ExecuteScalarAsync();
                    int count = result != null ? Convert.ToInt32(result) : 0;
                    log.LogInformation($"Util.UserExistsAsync-Count: {count}");
                    return count > 0;
                }
            }
        }

        public static async System.Threading.Tasks.Task<bool> UserExistsByIdAsync(ILogger log, int id)
        {
            string connectionString = Util.GetConnectionString();
            if (string.IsNullOrEmpty(connectionString))
            {
                return false;
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                string query = "SELECT 1 FROM Users WHERE id = @Id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    //int count = (int)await cmd.ExecuteScalarAsync();
                    var result = await cmd.ExecuteScalarAsync();
                    int count = result != null ? Convert.ToInt32(result) : 0;
                    log.LogInformation($"Util.UserExistsByIdAsync-Count: {count}");
                    return count > 0;
                }
            }
        }

        public static JsonSerializerOptions JsonSerializerOptions(){
            return new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }
    }
}
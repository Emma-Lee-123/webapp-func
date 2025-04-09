using System;
using System.Net.NetworkInformation;
using Microsoft.Data.SqlClient;

namespace Company.Function
{
    public static class Util
    {
        public static string GetConnectionString()
        {
            return Environment.GetEnvironmentVariable("SqlConnectionString");
        }

        public static async System.Threading.Tasks.Task<bool> UserExistsAsync(string email)
        {
            string connectionString = Util.GetConnectionString();
            if (string.IsNullOrEmpty(connectionString))
            {
                return false;
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                string query = "SELECT 1 FROM Users WHERE email = @Email";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    int count = (int)await cmd.ExecuteScalarAsync();
                    return count > 0;
                }
            }
        }

        public static async System.Threading.Tasks.Task<bool> UserExistsByIdAsync(int id)
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
                    int count = (int)await cmd.ExecuteScalarAsync();
                    return count > 0;
                }
            }
        }
    }
}
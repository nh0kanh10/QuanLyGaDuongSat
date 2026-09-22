using System.Data;
using Microsoft.Data.SqlClient;

namespace DAL.Connection
{
    public static class DatabaseHelper
    {
        private static string _connectionString =
            @"Server=.\SQLEXPRESS;Database=QuanLyDuongSatV2;Trusted_Connection=True;TrustServerCertificate=True;";

        public static void SetConnectionString(string connStr)
        {
            _connectionString = connStr;
        }

        // SELECT -> DataTable
        public static DataTable ExecuteQuery(string query, SqlParameter[]? parameters = null)
        {
            var dt = new DataTable();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(query, conn);
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            using var adapter = new SqlDataAdapter(cmd);
            adapter.Fill(dt);
            return dt;
        }

        // INSERT, UPDATE, DELETE -> so dong anh huong
        public static int ExecuteNonQuery(string query, SqlParameter[]? parameters = null)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(query, conn);
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            conn.Open();
            return cmd.ExecuteNonQuery();
        }

        // Tra ve 1 gia tri (COUNT, MAX, SCOPE_IDENTITY...)
        public static object? ExecuteScalar(string query, SqlParameter[]? parameters = null)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(query, conn);
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            conn.Open();
            return cmd.ExecuteScalar();
        }

        // Goi Stored Procedure
        public static DataTable ExecuteStoredProcedure(string spName, SqlParameter[]? parameters = null)
        {
            var dt = new DataTable();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(spName, conn) { CommandType = CommandType.StoredProcedure };
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            using var adapter = new SqlDataAdapter(cmd);
            adapter.Fill(dt);
            return dt;
        }

        // Test ket noi
        public static bool TestConnection()
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                conn.Open();
                return true;
            }
            catch { return false; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace ShippingAppPallet.Class
{
    public class User
    {
        private static readonly string ConnectionString =
            ConfigurationManager.ConnectionStrings["ShippingConnection"]?.ConnectionString
            ?? throw new InvalidOperationException("Connection string 'ShippingConnection' tidak ditemukan.");

        public bool IsUserAuthorized(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM udt_CDUsersShipping WHERE [Username] = @Username AND [Enable] = 1",
                conn))
            {
                // Tentukan tipe & panjang kolom Username (samakan dengan DDL Anda; di sini contoh VARCHAR(100))
                cmd.Parameters.Add(new SqlParameter("@Username", SqlDbType.VarChar, 100) { Value = username.Trim() });

                conn.Open();
                var result = cmd.ExecuteScalar();
                var count = (result == null || result == DBNull.Value) ? 0 : Convert.ToInt32(result);
                return count > 0;
            }
        }

        public void UpdateLastLogin(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return;

            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand(
                "UPDATE udt_CDUsersShipping SET [LastLogin] = GETDATE() WHERE [Username] = @Username",
                conn))
            {
                cmd.Parameters.Add(new SqlParameter("@Username", SqlDbType.VarChar, 100) { Value = username.Trim() });

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public string GetUserRole(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return "";

            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand(
                "SELECT [Role] FROM udt_CDUsersShipping WHERE [Username] = @Username AND [Enable] = 1", conn))
            {
                cmd.Parameters.Add(new SqlParameter("@Username", SqlDbType.VarChar, 100) { Value = username.Trim() });
                conn.Open();
                var result = cmd.ExecuteScalar();
                return result != null ? result.ToString() : "";
            }
        }
    }
}

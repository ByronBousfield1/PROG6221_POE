using System;
using MySql.Data.MySqlClient;

namespace CyberAwarenessBot.Services
{
    public static class DatabaseHelper
    {
        // XAMPP default is root with no password. If you set a root password
        // during MySQL install, put it in DbPassword below.
        private const string DbHost = "localhost";
        private const string DbPort = "3306";
        private const string DbUser = "root";
        private const string DbPassword = "";
        private const string DbName = "cyberbot";

        // Connection without a database, used to create the database the first time.
        private static string ServerConnectionString =>
            $"Server={DbHost};Port={DbPort};Uid={DbUser};Pwd={DbPassword};" +
            "SslMode=Preferred;AllowPublicKeyRetrieval=True;";

        // Connection that targets the cyberbot database.
        public static string ConnectionString =>
            $"Server={DbHost};Port={DbPort};Database={DbName};Uid={DbUser};Pwd={DbPassword};" +
            "SslMode=Preferred;AllowPublicKeyRetrieval=True;";

        public static bool TryInitialize(out string error)
        {
            error = string.Empty;
            try
            {
                using var conn = new MySqlConnection(ServerConnectionString);
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"CREATE DATABASE IF NOT EXISTS {DbName};";
                cmd.ExecuteNonQuery();

                conn.ChangeDatabase(DbName);

                cmd.CommandText =
                    "CREATE TABLE IF NOT EXISTS tasks (" +
                    "id INT AUTO_INCREMENT PRIMARY KEY, " +
                    "title VARCHAR(255) NOT NULL, " +
                    "description TEXT, " +
                    "reminder_date DATETIME NULL, " +
                    "is_completed TINYINT(1) NOT NULL DEFAULT 0, " +
                    "created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP" +
                    ");";
                cmd.ExecuteNonQuery();

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static MySqlConnection GetConnection()
        {
            return new MySqlConnection(ConnectionString);
        }
    }
}

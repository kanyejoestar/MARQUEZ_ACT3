using System;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SIGN_IN
{
    internal static class Database
    {
        private static string DatabasePath
        {
            get
            {
                string[] candidates =
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Users.accdb"),
                    Path.Combine(Directory.GetCurrentDirectory(), "Users.accdb"),
                    Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\Users.accdb")),
                    Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Users.accdb"))
                };
                return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
            }
        }

        private static string ConnectionString
        {
            get { return "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + DatabasePath + ";Persist Security Info=False;"; }
        }

        public static void EnsureReady()
        {
            if (!File.Exists(DatabasePath))
                throw new FileNotFoundException("Users.accdb was not found. Place Users.accdb in the project folder or beside the application.", DatabasePath);

            using (OleDbConnection connection = new OleDbConnection(ConnectionString))
            {
                connection.Open();
                DataTable tables = connection.GetSchema("Tables");
                bool hasUsers = tables.Rows.Cast<DataRow>().Any(r => string.Equals(Convert.ToString(r["TABLE_NAME"]), "Users", StringComparison.OrdinalIgnoreCase));
                if (!hasUsers)
                {
                    using (OleDbCommand command = new OleDbCommand("CREATE TABLE Users (ID COUNTER CONSTRAINT PK_Users PRIMARY KEY, FullName TEXT(100), Username TEXT(100), PasswordHash TEXT(255), PasswordSalt TEXT(255))", connection))
                        command.ExecuteNonQuery();
                }
                else
                {
                    DataTable columns = connection.GetSchema("Columns", new string[] { null, null, "Users", null });
                    string[] names = columns.Rows.Cast<DataRow>().Select(r => Convert.ToString(r["COLUMN_NAME"])).ToArray();
                    if (FindColumn(names, "PasswordHash", "Password", "PassWord", "UserPassword") != null && FindColumn(names, "PasswordSalt", "Salt") == null)
                    {
                        using (OleDbCommand command = new OleDbCommand("ALTER TABLE Users ADD COLUMN PasswordSalt TEXT(255)", connection))
                            command.ExecuteNonQuery();
                    }
                }
            }
        }

        public static bool Register(string fullName, string username, string password, out string message)
        {
            EnsureReady();
            using (OleDbConnection connection = new OleDbConnection(ConnectionString))
            {
                connection.Open();
                DataTable columns = connection.GetSchema("Columns", new string[] { null, null, "Users", null });
                string[] names = columns.Rows.Cast<DataRow>().Select(r => Convert.ToString(r["COLUMN_NAME"])).ToArray();
                string userColumn = FindColumn(names, "Username", "UserName", "User", "Email");
                string nameColumn = FindColumn(names, "FullName", "Name", "Full_Name");
                string hashColumn = FindColumn(names, "PasswordHash", "Password", "PassWord", "UserPassword");
                string saltColumn = FindColumn(names, "PasswordSalt", "Salt");
                if (userColumn == null || hashColumn == null)
                {
                    message = "The Users table needs Username and Password or PasswordHash fields.";
                    return false;
                }

                using (OleDbCommand check = new OleDbCommand("SELECT COUNT(*) FROM [Users] WHERE [" + userColumn + "] = ?", connection))
                {
                    check.Parameters.AddWithValue("@username", username);
                    if (Convert.ToInt32(check.ExecuteScalar()) > 0)
                    {
                        message = "That username is already registered.";
                        return false;
                    }
                }

                byte[] salt = new byte[16];
                using (RandomNumberGenerator rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
                string encodedSalt = Convert.ToBase64String(salt);
                string encodedHash = HashPassword(password, salt);
                var fields = new System.Collections.Generic.List<string>();
                var placeholders = new System.Collections.Generic.List<string>();
                using (OleDbCommand insert = new OleDbCommand())
                {
                    insert.Connection = connection;
                    if (nameColumn != null) { fields.Add("[" + nameColumn + "]"); placeholders.Add("?"); insert.Parameters.AddWithValue("@name", fullName); }
                    fields.Add("[" + userColumn + "]"); placeholders.Add("?"); insert.Parameters.AddWithValue("@user", username);
                    fields.Add("[" + hashColumn + "]"); placeholders.Add("?"); insert.Parameters.AddWithValue("@hash", encodedHash);
                    if (saltColumn != null) { fields.Add("[" + saltColumn + "]"); placeholders.Add("?"); insert.Parameters.AddWithValue("@salt", encodedSalt); }
                    insert.CommandText = "INSERT INTO [Users] (" + string.Join(",", fields) + ") VALUES (" + string.Join(",", placeholders) + ")";
                    insert.ExecuteNonQuery();
                }
            }
            message = "Account created. You can now log in.";
            return true;
        }

        public static bool ValidateLogin(string username, string password, out string fullName, out string message)
        {
            EnsureReady();
            fullName = username;
            using (OleDbConnection connection = new OleDbConnection(ConnectionString))
            {
                connection.Open();
                DataTable columns = connection.GetSchema("Columns", new string[] { null, null, "Users", null });
                string[] names = columns.Rows.Cast<DataRow>().Select(r => Convert.ToString(r["COLUMN_NAME"])).ToArray();
                string userColumn = FindColumn(names, "Username", "UserName", "User", "Email");
                string nameColumn = FindColumn(names, "FullName", "Name", "Full_Name");
                string hashColumn = FindColumn(names, "PasswordHash", "Password", "PassWord", "UserPassword");
                string saltColumn = FindColumn(names, "PasswordSalt", "Salt");
                if (userColumn == null || hashColumn == null)
                {
                    message = "The Users table needs Username and Password or PasswordHash fields.";
                    return false;
                }
                string query = "SELECT [" + hashColumn + "]" + (nameColumn == null ? "" : ", [" + nameColumn + "]") + (saltColumn == null ? "" : ", [" + saltColumn + "]") + " FROM [Users] WHERE [" + userColumn + "] = ?";
                using (OleDbCommand command = new OleDbCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", username);
                    using (OleDbDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.Read()) { message = "Username or password is incorrect."; return false; }
                        string stored = Convert.ToString(reader[0]);
                        int index = 1;
                        if (nameColumn != null && reader[index] != DBNull.Value) { fullName = Convert.ToString(reader[index]); index++; }
                        string saltText = saltColumn == null ? null : Convert.ToString(reader[index]);
                        bool valid;
                        if (!string.IsNullOrEmpty(saltText))
                        {
                            byte[] salt = Convert.FromBase64String(saltText);
                            valid = SlowEquals(Convert.FromBase64String(stored), Convert.FromBase64String(HashPassword(password, salt)));
                        }
                        else
                        {
                            valid = string.Equals(stored, password, StringComparison.Ordinal);
                        }
                        message = valid ? "Login successful." : "Username or password is incorrect.";
                        return valid;
                    }
                }
            }
        }

        private static string FindColumn(string[] columns, params string[] options)
        {
            foreach (string option in options)
            {
                string match = columns.FirstOrDefault(c => string.Equals(c, option, StringComparison.OrdinalIgnoreCase));
                if (match != null) return match;
            }
            return null;
        }

        private static string HashPassword(string password, byte[] salt)
        {
            using (Rfc2898DeriveBytes derive = new Rfc2898DeriveBytes(password, salt, 100000))
                return Convert.ToBase64String(derive.GetBytes(32));
        }

        private static bool SlowEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int difference = 0;
            for (int i = 0; i < a.Length; i++) difference |= a[i] ^ b[i];
            return difference == 0;
        }
    }
}

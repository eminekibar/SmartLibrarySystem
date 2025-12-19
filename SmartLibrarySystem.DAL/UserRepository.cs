using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using SmartLibrarySystem.Models;

namespace SmartLibrarySystem.DAL
{
    public class UserRepository
    {
        private SqlConnection GetConnection() => Database.Instance.Connection;

        public IEnumerable<User> GetAll()
        {
            var users = new List<User>();
            var conn = GetConnection();
            var shouldClose = conn.State != ConnectionState.Open;
            if (shouldClose) conn.Open();

            try
            {
                using (var cmd = new SqlCommand("SELECT * FROM Users WHERE IsActive = 1", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(MapUser(reader));
                    }
                }
            }
            finally
            {
                if (shouldClose) conn.Close();
            }

            return users;
        }

        public User GetByEmail(string email)
        {
            var conn = GetConnection();
            var shouldClose = conn.State != ConnectionState.Open;
            if (shouldClose) conn.Open();
            try
            {
                using (var cmd = new SqlCommand("SELECT * FROM Users WHERE Email = @Email AND IsActive = 1", conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    using (var reader = cmd.ExecuteReader())
                    {
                        return reader.Read() ? MapUser(reader) : null;
                    }
                }
            }
            finally
            {
                if (shouldClose) conn.Close();
            }
        }

        public User GetById(int userId)
        {
            var conn = GetConnection();
            var shouldClose = conn.State != ConnectionState.Open;
            if (shouldClose) conn.Open();
            try
            {
                using (var cmd = new SqlCommand("SELECT * FROM Users WHERE UserId = @UserId AND IsActive = 1", conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        return reader.Read() ? MapUser(reader) : null;
                    }
                }
            }
            finally
            {
                if (shouldClose) conn.Close();
            }
        }

        public IEnumerable<User> GetByRole(string role)
        {
            var users = new List<User>();
            var conn = GetConnection();
            var shouldClose = conn.State != ConnectionState.Open;
            if (shouldClose) conn.Open();

            try
            {
                using (var cmd = new SqlCommand("SELECT * FROM Users WHERE Role = @Role AND IsActive = 1", conn))
                {
                    cmd.Parameters.AddWithValue("@Role", role);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            users.Add(MapUser(reader));
                        }
                    }
                }
            }
            finally
            {
                if (shouldClose) conn.Close();
            }

            return users;
        }

        public void Add(User user)
        {
            var conn = GetConnection();
            var shouldClose = conn.State != ConnectionState.Open;
            if (shouldClose) conn.Open();

            try
            {
                const string sql = @"INSERT INTO Users(FullName, Email, PasswordHash, SchoolNumber, Phone, Role, IsActive)
                                     VALUES(@FullName, @Email, @PasswordHash, @SchoolNumber, @Phone, @Role, 1)";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@FullName", user.FullName);
                    cmd.Parameters.AddWithValue("@Email", user.Email);
                    cmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
                    cmd.Parameters.AddWithValue("@SchoolNumber", (object)user.SchoolNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Phone", (object)user.Phone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Role", (object)user.Role ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
            finally
            {
                if (shouldClose) conn.Close();
            }
        }

        public void Update(User user)
        {
            var conn = GetConnection();
            var shouldClose = conn.State != ConnectionState.Open;
            if (shouldClose) conn.Open();

            try
            {
                const string sql = @"UPDATE Users SET FullName = @FullName, Email = @Email, PasswordHash = @PasswordHash,
                                        SchoolNumber = @SchoolNumber, Phone = @Phone, Role = @Role
                                     WHERE UserId = @UserId AND IsActive = 1";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@FullName", user.FullName);
                    cmd.Parameters.AddWithValue("@Email", user.Email);
                    cmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
                    cmd.Parameters.AddWithValue("@SchoolNumber", (object)user.SchoolNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Phone", (object)user.Phone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Role", (object)user.Role ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UserId", user.UserId);
                    cmd.ExecuteNonQuery();
                }
            }
            finally
            {
                if (shouldClose) conn.Close();
            }
        }

        public void Delete(int userId)
        {
            var conn = GetConnection();
            var shouldClose = conn.State != ConnectionState.Open;
            if (shouldClose) conn.Open();

            try
            {
                using (var cmd = new SqlCommand("UPDATE Users SET IsActive = 0 WHERE UserId = @UserId", conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
            finally
            {
                if (shouldClose) conn.Close();
            }
        }

        public bool EmailExists(string email, int? excludeUserId = null)
        {
            var conn = GetConnection();
            var shouldClose = conn.State != ConnectionState.Open;
            if (shouldClose) conn.Open();

            try
            {
                var sql = "SELECT COUNT(1) FROM Users WHERE Email = @Email AND IsActive = 1";
                if (excludeUserId.HasValue)
                {
                    sql += " AND UserId <> @ExcludeUserId";
                }

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    if (excludeUserId.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@ExcludeUserId", excludeUserId.Value);
                    }
                    var count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
            finally
            {
                if (shouldClose) conn.Close();
            }
        }

        private static User MapUser(IDataRecord record)
        {
            return new User
            {
                UserId = Convert.ToInt32(record["UserId"]),
                FullName = record["FullName"].ToString(),
                Email = record["Email"].ToString(),
                PasswordHash = record["PasswordHash"].ToString(),
                SchoolNumber = record["SchoolNumber"] == DBNull.Value ? null : record["SchoolNumber"].ToString(),
                Phone = record["Phone"] == DBNull.Value ? null : record["Phone"].ToString(),
                Role = record["Role"].ToString(),
                CreatedAt = Convert.ToDateTime(record["CreatedAt"]),
                IsActive = record["IsActive"] == DBNull.Value ? true : (bool)record["IsActive"]
            };
        }
    }
}

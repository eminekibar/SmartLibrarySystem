using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using SmartLibrarySystem.Models;

namespace SmartLibrarySystem.DAL
{
    public class BorrowRequestRepository
    {
        private SqlConnection GetConnection() => Database.Instance.Connection;

        public void Add(BorrowRequest request)
        {
            var conn = GetConnection();
            var shouldClose = conn.State != ConnectionState.Open;
            if (shouldClose) conn.Open();
            try
            {
                const string sql = @"INSERT INTO BorrowRequests(UserId, BookId, Status, RequestDate)
                                     VALUES(@UserId, @BookId, @Status, @RequestDate)";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", request.UserId);
                    cmd.Parameters.AddWithValue("@BookId", request.BookId);
                    cmd.Parameters.AddWithValue("@Status", request.Status);
                    cmd.Parameters.AddWithValue("@RequestDate", request.RequestDate);
                    cmd.ExecuteNonQuery();
                }
            }
            finally
            {
                if (shouldClose) conn.Close();
            }
        }

        public string GetActiveRequestStatus(int userId, int bookId)
        {
            var conn = GetConnection();
            var shouldClose = conn.State != ConnectionState.Open;
            if (shouldClose) conn.Open();
            try
            {
                const string sql = @"SELECT TOP 1 Status
                                     FROM BorrowRequests
                                     WHERE UserId = @UserId AND BookId = @BookId AND Status <> @Returned
                                     ORDER BY RequestDate DESC";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@BookId", bookId);
                    cmd.Parameters.AddWithValue("@Returned", RequestStatus.Returned);
                    var result = cmd.ExecuteScalar();
                    return result == null ? null : result.ToString();
                }
            }
            finally
            {
                if (shouldClose) conn.Close();
            }
        }

        public void UpdateStatus(int requestId, string status, DateTime? deliveryDate, DateTime? returnDate)
        {
            var conn = GetConnection();
            var shouldClose = conn.State != ConnectionState.Open;
            if (shouldClose) conn.Open();
            try
            {
                const string sql = @"UPDATE BorrowRequests
                                     SET Status=@Status, DeliveryDate=@DeliveryDate, ReturnDate=@ReturnDate
                                     WHERE RequestId = @RequestId";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Status", status);
                    cmd.Parameters.AddWithValue("@DeliveryDate", (object)deliveryDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ReturnDate", (object)returnDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@RequestId", requestId);
                    cmd.ExecuteNonQuery();
                }
            }
            finally
            {
                if (shouldClose) conn.Close();
            }
        }

        public IEnumerable<BorrowRequest> GetByUser(int userId)
        {
            var requests = new List<BorrowRequest>();
            var conn = GetConnection();
            var shouldClose = conn.State != ConnectionState.Open;
            if (shouldClose) conn.Open();
            try
            {
                const string sql = @"SELECT br.*, b.Title AS BookTitle, b.Author AS BookAuthor
                                     FROM BorrowRequests br
                                     INNER JOIN Books b ON br.BookId = b.BookId
                                     WHERE br.UserId = @UserId
                                     ORDER BY br.RequestDate DESC";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            requests.Add(MapRequest(reader));
                        }
                    }
                }
            }
            finally
            {
                if (shouldClose) conn.Close();
            }

            return requests;
        }

        public IEnumerable<BorrowRequest> GetAll(string status = null)
        {
            var requests = new List<BorrowRequest>();
            var conn = GetConnection();
            var shouldClose = conn.State != ConnectionState.Open;
            if (shouldClose) conn.Open();
            try
            {
                var sql = @"SELECT br.*, u.FullName AS UserName, b.Title AS BookTitle, b.Author AS BookAuthor
                            FROM BorrowRequests br
                            INNER JOIN Users u ON br.UserId = u.UserId
                            INNER JOIN Books b ON br.BookId = b.BookId";
                if (!string.IsNullOrWhiteSpace(status))
                {
                    sql += " WHERE br.Status = @Status";
                }
                sql += " ORDER BY br.RequestDate DESC";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    if (!string.IsNullOrWhiteSpace(status))
                    {
                        cmd.Parameters.AddWithValue("@Status", status);
                    }
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            requests.Add(MapRequest(reader));
                        }
                    }
                }
            }
            finally
            {
                if (shouldClose) conn.Close();
            }

            return requests;
        }

        public BorrowRequest GetById(int requestId)
        {
            var conn = GetConnection();
            var shouldClose = conn.State != ConnectionState.Open;
            if (shouldClose) conn.Open();
            try
            {
                const string sql = @"SELECT br.*, u.FullName AS UserName, b.Title AS BookTitle, b.Author AS BookAuthor
                                     FROM BorrowRequests br
                                     INNER JOIN Users u ON br.UserId = u.UserId
                                     INNER JOIN Books b ON br.BookId = b.BookId
                                     WHERE br.RequestId = @RequestId";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@RequestId", requestId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        return reader.Read() ? MapRequest(reader) : null;
                    }
                }
            }
            finally
            {
                if (shouldClose) conn.Close();
            }
        }

        public int GetDailyBorrowCount(DateTime date)
        {
            var next = date.Date.AddDays(1);
            var conn = GetConnection();
            var shouldClose = conn.State != ConnectionState.Open;
            if (shouldClose) conn.Open();
            try
            {
                using (var cmd = new SqlCommand("SELECT COUNT(1) FROM BorrowRequests WHERE RequestDate >= @From AND RequestDate < @To", conn))
                {
                    cmd.Parameters.AddWithValue("@From", date.Date);
                    cmd.Parameters.AddWithValue("@To", next);
                    return (int)cmd.ExecuteScalar();
                }
            }
            finally
            {
                if (shouldClose) conn.Close();
            }
        }

        public int GetReturnCount(DateTime date)
        {
            var next = date.Date.AddDays(1);
            var conn = GetConnection();
            var shouldClose = conn.State != ConnectionState.Open;
            if (shouldClose) conn.Open();
            try
            {
                using (var cmd = new SqlCommand("SELECT COUNT(1) FROM BorrowRequests WHERE ReturnDate >= @From AND ReturnDate < @To", conn))
                {
                    cmd.Parameters.AddWithValue("@From", date.Date);
                    cmd.Parameters.AddWithValue("@To", next);
                    var result = cmd.ExecuteScalar();
                    return result == null ? 0 : Convert.ToInt32(result);
                }
            }
            finally
            {
                if (shouldClose) conn.Close();
            }
        }

        public IEnumerable<BorrowRequest> GetOverdue(DateTime date, int allowedDays = 14)
        {
            var overdueList = new List<BorrowRequest>();
            var conn = GetConnection();
            var shouldClose = conn.State != ConnectionState.Open;
            if (shouldClose) conn.Open();
            try
            {
                const string sql = @"SELECT br.*, u.FullName AS UserName, b.Title AS BookTitle, b.Author AS BookAuthor
                                     FROM BorrowRequests br
                                     INNER JOIN Users u ON br.UserId = u.UserId
                                     INNER JOIN Books b ON br.BookId = b.BookId
                                     WHERE br.Status = 'Delivered' AND br.DeliveryDate IS NOT NULL
                                           AND br.ReturnDate IS NULL AND br.DeliveryDate < @CutOff";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@CutOff", date.Date.AddDays(-allowedDays));
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            overdueList.Add(MapRequest(reader));
                        }
                    }
                }
            }
            finally
            {
                if (shouldClose) conn.Close();
            }

            return overdueList;
        }

        public IEnumerable<KeyValuePair<string, int>> GetTopBorrowedBooks(int top)
        {
            var list = new List<KeyValuePair<string, int>>();
            var conn = GetConnection();
            var shouldClose = conn.State != ConnectionState.Open;
            if (shouldClose) conn.Open();
            try
            {
                const string sql = @"SELECT TOP (@Top) b.Title, COUNT(*) AS BorrowCount
                                     FROM BorrowRequests br
                                     INNER JOIN Books b ON br.BookId = b.BookId
                                     GROUP BY b.Title
                                     ORDER BY BorrowCount DESC";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Top", top);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new KeyValuePair<string, int>(reader["Title"].ToString(), Convert.ToInt32(reader["BorrowCount"])));
                        }
                    }
                }
            }
            finally
            {
                if (shouldClose) conn.Close();
            }

            return list;
        }

        public bool Delete(int requestId, int userId)
        {
            var conn = GetConnection();
            var shouldClose = conn.State != ConnectionState.Open;
            if (shouldClose) conn.Open();
            try
            {
                const string sql = @"DELETE FROM BorrowRequests WHERE RequestId = @RequestId AND UserId = @UserId AND Status = @Pending";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@RequestId", requestId);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@Pending", RequestStatus.Pending);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            finally
            {
                if (shouldClose) conn.Close();
            }
        }

        public IDictionary<string, int> GetBorrowStats(DateTime from, DateTime to)
        {
            var stats = new Dictionary<string, int>();
            var conn = GetConnection();
            var shouldClose = conn.State != ConnectionState.Open;
            if (shouldClose) conn.Open();
            try
            {
                const string sql = @"SELECT CAST(RequestDate AS DATE) AS RequestDay, COUNT(*) AS Cnt
                                     FROM BorrowRequests
                                     WHERE RequestDate >= @From AND RequestDate < @To
                                     GROUP BY CAST(RequestDate AS DATE)";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@From", from);
                    cmd.Parameters.AddWithValue("@To", to);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var key = Convert.ToDateTime(reader["RequestDay"]).ToString("yyyy-MM-dd");
                            stats[key] = Convert.ToInt32(reader["Cnt"]);
                        }
                    }
                }
            }
            finally
            {
                if (shouldClose) conn.Close();
            }

            return stats;
        }

        private static BorrowRequest MapRequest(IDataRecord record)
        {
            return new BorrowRequest
            {
                RequestId = Convert.ToInt32(record["RequestId"]),
                UserId = Convert.ToInt32(record["UserId"]),
                BookId = Convert.ToInt32(record["BookId"]),
                Status = record["Status"].ToString(),
                RequestDate = Convert.ToDateTime(record["RequestDate"]),
                DeliveryDate = record["DeliveryDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(record["DeliveryDate"]),
                ReturnDate = record["ReturnDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(record["ReturnDate"]),
                UserName = record is IDataRecord r1 && HasField(r1, "UserName") && record["UserName"] != DBNull.Value ? record["UserName"].ToString() : null,
                BookTitle = record is IDataRecord r2 && HasField(r2, "BookTitle") && record["BookTitle"] != DBNull.Value ? record["BookTitle"].ToString() : null,
                BookAuthor = record is IDataRecord r3 && HasField(r3, "BookAuthor") && record["BookAuthor"] != DBNull.Value ? record["BookAuthor"].ToString() : null
            };
        }

        private static bool HasField(IDataRecord record, string name)
        {
            for (var i = 0; i < record.FieldCount; i++)
            {
                if (record.GetName(i).Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }
}

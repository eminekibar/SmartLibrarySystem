using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using SmartLibrarySystem.Models;

namespace SmartLibrarySystem.DAL
{
    public class BookRepository
    {
        private SqlConnection GetConnection() => Database.Instance.Connection;

        public IEnumerable<Book> GetAll()
        {
            var books = new List<Book>();
            var conn = GetConnection();
            var shouldClose = conn.State != ConnectionState.Open;
            if (shouldClose) conn.Open();
            try
            {
                using (var cmd = new SqlCommand("SELECT * FROM Books", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        books.Add(MapBook(reader));
                    }
                }
            }
            finally
            {
                if (shouldClose) conn.Close();
            }
            return books;
        }

        public Book GetById(int bookId)
        {
            var conn = GetConnection();
            var shouldClose = conn.State != ConnectionState.Open;
            if (shouldClose) conn.Open();
            try
            {
                using (var cmd = new SqlCommand("SELECT * FROM Books WHERE BookId = @BookId", conn))
                {
                    cmd.Parameters.AddWithValue("@BookId", bookId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        return reader.Read() ? MapBook(reader) : null;
                    }
                }
            }
            finally
            {
                if (shouldClose) conn.Close();
            }
        }

        public IEnumerable<Book> Search(string category, string author, int? publishYear, string keyword)
        {
            var results = new List<Book>();
            var conn = GetConnection();
            var shouldClose = conn.State != ConnectionState.Open;
            if (shouldClose) conn.Open();

            try
            {
                var sqlBuilder = new StringBuilder("SELECT * FROM Books WHERE 1=1");
                var cmd = new SqlCommand { Connection = conn };

                if (!string.IsNullOrWhiteSpace(category))
                {
                    sqlBuilder.Append(" AND Category LIKE @Category");
                    cmd.Parameters.AddWithValue("@Category", $"%{category}%");
                }
                if (!string.IsNullOrWhiteSpace(author))
                {
                    sqlBuilder.Append(" AND Author LIKE @Author");
                    cmd.Parameters.AddWithValue("@Author", $"%{author}%");
                }
                if (publishYear.HasValue)
                {
                    sqlBuilder.Append(" AND PublishYear = @PublishYear");
                    cmd.Parameters.AddWithValue("@PublishYear", publishYear.Value);
                }
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    sqlBuilder.Append(" AND (Title LIKE @Keyword OR Author LIKE @Keyword OR Category LIKE @Keyword)");
                    cmd.Parameters.AddWithValue("@Keyword", $"%{keyword}%");
                }

                cmd.CommandText = sqlBuilder.ToString();
                using (cmd)
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(MapBook(reader));
                    }
                }
            }
            finally
            {
                if (shouldClose) conn.Close();
            }

            return results;
        }

        public void Add(Book book)
        {
            var conn = GetConnection();
            var shouldClose = conn.State != ConnectionState.Open;
            if (shouldClose) conn.Open();
            try
            {
                const string sql = @"INSERT INTO Books(Title, Author, Category, PublishYear, Stock, Shelf)
                                     VALUES(@Title, @Author, @Category, @PublishYear, @Stock, @Shelf)";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Title", book.Title);
                    cmd.Parameters.AddWithValue("@Author", book.Author);
                    cmd.Parameters.AddWithValue("@Category", book.Category);
                    cmd.Parameters.AddWithValue("@PublishYear", book.PublishYear);
                    cmd.Parameters.AddWithValue("@Stock", book.Stock);
                    cmd.Parameters.AddWithValue("@Shelf", book.Shelf);
                    cmd.ExecuteNonQuery();
                }
            }
            finally
            {
                if (shouldClose) conn.Close();
            }
        }

        public void Update(Book book)
        {
            var conn = GetConnection();
            var shouldClose = conn.State != ConnectionState.Open;
            if (shouldClose) conn.Open();
            try
            {
                const string sql = @"UPDATE Books SET Title=@Title, Author=@Author, Category=@Category,
                                        PublishYear=@PublishYear, Stock=@Stock, Shelf=@Shelf
                                     WHERE BookId = @BookId";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Title", book.Title);
                    cmd.Parameters.AddWithValue("@Author", book.Author);
                    cmd.Parameters.AddWithValue("@Category", book.Category);
                    cmd.Parameters.AddWithValue("@PublishYear", book.PublishYear);
                    cmd.Parameters.AddWithValue("@Stock", book.Stock);
                    cmd.Parameters.AddWithValue("@Shelf", book.Shelf);
                    cmd.Parameters.AddWithValue("@BookId", book.BookId);
                    cmd.ExecuteNonQuery();
                }
            }
            finally
            {
                if (shouldClose) conn.Close();
            }
        }

        public void Delete(int bookId)
        {
            var conn = GetConnection();
            var shouldClose = conn.State != ConnectionState.Open;
            if (shouldClose) conn.Open();
            try
            {
                using (var cmd = new SqlCommand("DELETE FROM Books WHERE BookId = @BookId", conn))
                {
                    cmd.Parameters.AddWithValue("@BookId", bookId);
                    cmd.ExecuteNonQuery();
                }
            }
            finally
            {
                if (shouldClose) conn.Close();
            }
        }

        public void UpdateStock(int bookId, int newStock)
        {
            var conn = GetConnection();
            var shouldClose = conn.State != ConnectionState.Open;
            if (shouldClose) conn.Open();

            try
            {
                using (var cmd = new SqlCommand("UPDATE Books SET Stock = @Stock WHERE BookId = @BookId", conn))
                {
                    cmd.Parameters.AddWithValue("@Stock", newStock);
                    cmd.Parameters.AddWithValue("@BookId", bookId);
                    cmd.ExecuteNonQuery();
                }
            }
            finally
            {
                if (shouldClose) conn.Close();
            }
        }

        public void AdjustStock(int bookId, int delta)
        {
            var conn = GetConnection();
            var shouldClose = conn.State != ConnectionState.Open;
            if (shouldClose) conn.Open();

            try
            {
                using (var cmd = new SqlCommand("UPDATE Books SET Stock = Stock + @Delta WHERE BookId = @BookId", conn))
                {
                    cmd.Parameters.AddWithValue("@Delta", delta);
                    cmd.Parameters.AddWithValue("@BookId", bookId);
                    cmd.ExecuteNonQuery();
                }
            }
            finally
            {
                if (shouldClose) conn.Close();
            }
        }

        private static Book MapBook(IDataRecord record)
        {
            return new Book
            {
                BookId = Convert.ToInt32(record["BookId"]),
                Title = record["Title"].ToString(),
                Author = record["Author"].ToString(),
                Category = record["Category"].ToString(),
                PublishYear = Convert.ToInt32(record["PublishYear"]),
                Stock = Convert.ToInt32(record["Stock"]),
                Shelf = record["Shelf"].ToString()
            };
        }
    }
}

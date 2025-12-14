using System.Data.SqlClient;

namespace SmartLibrarySystem.DAL
{
    public class Database
    {
        private static readonly object padlock = new object();
        private static Database instance;
        private readonly SqlConnection connection;

        private Database()
        {
            connection = new SqlConnection(
                "Server=DESKTOP-DG4KB31\\SQLEXPRESS;Database=SmartLibraryDB;Trusted_Connection=True;Encrypt=False;"
            );
        }

        public static Database Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (padlock)
                    {
                        if (instance == null)
                        {
                            instance = new Database();
                        }
                    }
                }
                return instance;
            }
        }

        public SqlConnection Connection => connection;
    }
}

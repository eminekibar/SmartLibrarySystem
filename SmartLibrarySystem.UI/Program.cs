using System;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace SmartLibrarySystem.UI
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, args) => HandleException(args.Exception);
            AppDomain.CurrentDomain.UnhandledException += (_, args) => HandleException(args.ExceptionObject as Exception);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new LoginForm());
        }

        private static void HandleException(Exception exception)
        {
            if (exception == null) return;

            if (exception is SqlException)
            {
                MessageBox.Show(
                    "Veritabanı bağlantı hatası oluştu. Lütfen sistem yöneticisine başvurun.",
                    "Bağlantı Hatası",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            MessageBox.Show(
                "Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.",
                "Hata",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}

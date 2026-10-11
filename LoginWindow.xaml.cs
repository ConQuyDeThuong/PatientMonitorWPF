using System;
using System.Windows;
using DataAccess;
using Microsoft.Data.SqlClient;

namespace PatientMonitorWPF
{
    public partial class LoginWindow : Window
    {
        private readonly string _connectionString = @"Server=.\SQLEXPRESS01;Database=PatientMonitorDB;Integrated Security=True;TrustServerCertificate=True;Encrypt=False;";

        public LoginWindow()
        {
            InitializeComponent();

            rbAdmin.Checked += (_, __) => { txtUsername.Text = "admin"; };
            rbBacSi.Checked += (_, __) => { txtUsername.Text = "bsan"; };
            rbBenhNhan.Checked += (_, __) => { txtUsername.Text = "bnbinh"; };
        }

        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin đăng nhập!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string query = "SELECT FullName, Role, RefId FROM Users";
                var dt = App.SqlHelper.Select(query, $"Username='{username}' AND PasswordHash='{password}'", null);

                if (dt.Rows.Count > 0)
                {
                    Session.User = SqlHelper.FromDataRow<User>(dt.Rows[0]);

                    MainWindow main = new MainWindow();
                    main.Show();

                    this.Close();
                }
                else
                {
                    MessageBox.Show("Tên đăng nhập hoặc mật khẩu không chính xác!", "Lỗi đăng nhập", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối SQL Server: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
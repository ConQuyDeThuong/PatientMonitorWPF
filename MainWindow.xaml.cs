using Microsoft.Data.SqlClient;
using MQTTnet;
using System;
using System.Buffers; // Dùng cho ReadOnlySequence.ToArray() trong MQTTnet v5.x
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PatientMonitorWPF
{
    public partial class MainWindow : Window
    {
        private IMqttClient? _mqttClient;
        private readonly string _connectionString = @"Server=.\SQLEXPRESS01;Database=PatientMonitorDB;Integrated Security=True;TrustServerCertificate=True;Encrypt=False;";
        private readonly Random _random = new Random();
        private const string MQTT_TOPIC = "hust/patient/blood_pressure";

        public MainWindow()
        {
            InitializeComponent();

            if (string.IsNullOrEmpty(Session.User?.Role))
            {
                Session.User = new User {
                    Username = "admin",
                    FullName = "Quản Trị Viên System",
                    Role = "Admin"
                };
            }

            ApplyRolePermissions();
        }

        private void ApplyRolePermissions()
        {
            string fullName = Session.User?.FullName ?? "Người dùng";
            string role = Session.User?.Role ?? "Guest";

            txtUserWelcome.Text = $"Xin chào: {fullName} ({role})";
            this.Title = $"Hệ Thống Giám Sát - {fullName} [{role}]";

            if (role == "Patient")
            {
                panelControls.Visibility = Visibility.Collapsed;
            }
            else
            {
                panelControls.Visibility = Visibility.Visible;
            }
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            DoubleAnimation fadeOut = new DoubleAnimation { From = 1.0, To = 0.0, Duration = TimeSpan.FromSeconds(0.4) };
            fadeOut.Completed += (s, args) =>
            {
                GridIntro.Visibility = Visibility.Collapsed;
                GridMain.Visibility = Visibility.Visible;

                DoubleAnimation fadeIn = new DoubleAnimation { From = 0.0, To = 1.0, Duration = TimeSpan.FromSeconds(0.4) };
                GridMain.BeginAnimation(Grid.OpacityProperty, fadeIn);

                AppendLog($"[HỆ THỐNG] Phiên làm việc: {(Session.User?.Role ?? "").ToUpper()} ({Session.User?.FullName})\n", Brushes.Yellow);
            };
            GridIntro.BeginAnimation(Grid.OpacityProperty, fadeOut);
        }

        private async void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            lblStatus.Text = "Trạng thái: Đang kết nối Broker...";
            lblStatus.Foreground = Brushes.Orange;
            btnConnect.IsEnabled = false;

            try
            {
                // SỬ DỤNG MqttClientFactory THAY CHO MqttFactory TRÊN MQTTnet v5.x
                var factory = new MqttClientFactory();
                _mqttClient = factory.CreateMqttClient();

                // Đăng ký nhận tin nhắn từ MQTT Broker
                _mqttClient.ApplicationMessageReceivedAsync += OnMqttMessageReceivedAsync;

                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer("broker.hivemq.com", 1883)
                    .WithClientId($"WPF_Patient_Monitor_{Guid.NewGuid()}")
                    .Build();

                var result = await _mqttClient.ConnectAsync(options);
                if (result.ResultCode == MqttClientConnectResultCode.Success)
                {
                    // Subscribe chính topic này để tự nhận dữ liệu giả lập
                    await _mqttClient.SubscribeAsync(MQTT_TOPIC);

                    lblStatus.Text = "Trạng thái: Đã kết nối MQTT Broker (Ready)";
                    lblStatus.Foreground = Brushes.LightGreen;
                    AppendLog("[MQTT v5] Đã kết nối Broker trực tuyến & Subcribe Topic thành công!\n", Brushes.Cyan);
                }
                else
                {
                    lblStatus.Text = "Trạng thái: Kết nối thất bại!";
                    lblStatus.Foreground = Brushes.Red;
                    btnConnect.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Trạng thái: Lỗi kết nối!";
                lblStatus.Foreground = Brushes.Red;
                btnConnect.IsEnabled = true;
                MessageBox.Show($"Lỗi kết nối MQTT: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Xử lý dữ liệu khi nhận tin nhắn từ MQTT Broker trong MQTTnet v5.x
        private async Task OnMqttMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            // Chuyển ReadOnlySequence<byte> sang byte[]
            byte[] payloadBytes = e.ApplicationMessage.Payload.ToArray();
            string payload = Encoding.UTF8.GetString(payloadBytes);

            await Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    var data = JsonSerializer.Deserialize<BloodPressureData>(payload);
                    if (data != null)
                    {
                        string prefix = data.Systolic >= 140 ? "[RECEIVE ALERT! CAO HUYẾT ÁP]" : "[RECEIVE - NORMAL]";
                        Brush color = data.Systolic >= 140 ? Brushes.OrangeRed : Brushes.LightGreen;

                        AppendLog($"[MQTT <- RECV] {prefix} {data}\n", color);
                        await SaveToDatabaseAsync(data);
                    }
                }
                catch (Exception ex)
                {
                    AppendLog($"[LỖI PARSE JSON]: {ex.Message}\n", Brushes.Red);
                }
            });
        }

        private async void btnSendNormal_Click(object sender, RoutedEventArgs e)
        {
            if (_mqttClient == null || !_mqttClient.IsConnected)
            {
                MessageBox.Show("Vui lòng bấm 'Kết nối Giả lập MQTT' trước!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var data = new BloodPressureData
            {
                PatientId = Session.User?.RefId ?? "BN-101",
                Systolic = _random.Next(100, 120),
                Diastolic = _random.Next(65, 80),
                HeartRate = _random.Next(60, 85),
                Timestamp = DateTime.Now
            };

            await PublishMqttDataAsync(data);
        }

        private async void btnSendAlert_Click(object sender, RoutedEventArgs e)
        {
            if (_mqttClient == null || !_mqttClient.IsConnected)
            {
                MessageBox.Show("Vui lòng bấm 'Kết nối Giả lập MQTT' trước!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var data = new BloodPressureData
            {
                PatientId = Session.User?.RefId ?? "BN-101",
                Systolic = _random.Next(145, 180),
                Diastolic = _random.Next(92, 110),
                HeartRate = _random.Next(90, 115),
                Timestamp = DateTime.Now
            };

            await PublishMqttDataAsync(data);
        }

        private async Task PublishMqttDataAsync(BloodPressureData data)
        {
            string jsonPayload = JsonSerializer.Serialize(data);
            byte[] payloadBytes = Encoding.UTF8.GetBytes(jsonPayload);

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(MQTT_TOPIC)
                .WithPayload(payloadBytes)
                .Build();

            if (_mqttClient != null && _mqttClient.IsConnected)
            {
                AppendLog($"[MQTT -> PUBLISH] Đã bắn gói tin giả lập lên Topic '{MQTT_TOPIC}'...\n", Brushes.Gray);
                await _mqttClient.PublishAsync(message);
            }
        }

        private async Task SaveToDatabaseAsync(BloodPressureData data)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    string query = @"INSERT INTO BloodPressureRecords (DeviceId, PatientId, Systolic, Diastolic, HeartRate, Status, RecordedAt) 
                                    VALUES (@DeviceId, @PatientId, @Systolic, @Diastolic, @HeartRate, @Status, @RecordedAt)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@DeviceId", data.DeviceId);
                        cmd.Parameters.AddWithValue("@PatientId", data.PatientId);
                        cmd.Parameters.AddWithValue("@Systolic", data.Systolic);
                        cmd.Parameters.AddWithValue("@Diastolic", data.Diastolic);
                        cmd.Parameters.AddWithValue("@HeartRate", data.HeartRate);
                        cmd.Parameters.AddWithValue("@Status", (data.Systolic >= 140) ? "Cảnh báo" : "Bình thường");
                        cmd.Parameters.AddWithValue("@RecordedAt", data.Timestamp);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                AppendLog($"[SQL SERVER] -> Đã lưu thành công bản ghi vào CSDL `PatientMonitorDB`.\n\n", Brushes.Khaki);
            }
            catch (Exception ex)
            {
                AppendLog($"[SQL SERVER LỖI] -> {ex.Message}\n\n", Brushes.Yellow);
            }
        }

        private void AppendLog(string text, Brush color)
        {
            TextRange tr = new TextRange(rtbLog.Document.ContentEnd, rtbLog.Document.ContentEnd) { Text = text };
            tr.ApplyPropertyValue(TextElement.ForegroundProperty, color);
            rtbLog.ScrollToEnd();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PatientMonitorWPF
{
    public class BloodPressureData
    {
        public string DeviceId { get; set; } = "ESP32-SIMULATOR";
        public string PatientId { get; set; } = "BN-101";
        public int Systolic { get; set; }
        public int Diastolic { get; set; }
        public int HeartRate { get; set; }
        public DateTime Timestamp { get; set; }

        public override string ToString()
        {
            return $"[Bệnh nhân: {PatientId}] Tâm thu: {Systolic} mmHg | Tâm trương: {Diastolic} mmHg | Nhịp tim: {HeartRate} bpm";
        }
    }

}

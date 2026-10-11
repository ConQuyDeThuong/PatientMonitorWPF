using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PatientMonitorWPF
{
    public class User
    {
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? RefId { get; set; }
    }
    public static class Session
    {
        static public User? User { get; set; }
    }
}

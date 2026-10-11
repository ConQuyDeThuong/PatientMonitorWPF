using System.Configuration;
using System.Data;
using System.Windows;

namespace PatientMonitorWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        public static DataAccess.SqlHelper SqlHelper { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            SqlHelper = new DataAccess.SqlHelper(null);
        }
    }

}

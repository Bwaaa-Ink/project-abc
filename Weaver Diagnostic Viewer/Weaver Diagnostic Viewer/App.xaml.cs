using System.Configuration;
using System.Data;
using System.Windows;

namespace Weaver_Diagnostic_Viewer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static PrimaryWindow? primaryWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            primaryWindow = new PrimaryWindow();
            primaryWindow.Show();
            base.OnStartup(e);
        }
    }

}

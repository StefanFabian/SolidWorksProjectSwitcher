using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Windows;
using Microsoft.Shell;

namespace SolidWorksProjectSwitcher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, ISingleInstanceApp
    {
        private const string UniqueName = "StefanFabian.SolidWorksProjectSwitcher";

        [STAThread]
        public static void Main(string[] args)
        {
            // For testing
            //var overrideCultureInfo = new CultureInfo("en-US");
            //Thread.CurrentThread.CurrentUICulture = overrideCultureInfo;
            //Thread.CurrentThread.CurrentCulture = overrideCultureInfo;
            if (SingleInstance<App>.InitializeAsFirstInstance(UniqueName))
            {
                var application = new App();
                application.InitializeComponent();
                application.Run();

                SingleInstance<App>.Cleanup();
            }
        }

        public bool SignalExternalCommandLineArgs(IList<string> args)
        {
            if (MainWindow.WindowState == WindowState.Minimized)
            {
                MainWindow.WindowState = WindowState.Normal;
            }

            MainWindow.Activate();
            return true;
        }
    }
}

using NE4ZUGFeRD.Views;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace NE4ZUGFeRD
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow wnd = new MainWindow();
            if (e.Args.Length == 2)
            {
                switch (e.Args[0])
                {
                    case "WRITE": break;
                    case "READ": break;
                    default: break;

                }
                Current.Shutdown();
            }
            wnd.Show();
        }
    }
}

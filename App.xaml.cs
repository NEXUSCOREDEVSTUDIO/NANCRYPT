using System;
using System.Windows;

namespace NanCrypt.UI
{
    public partial class App : Application
    {
        public App()
        {
            this.DispatcherUnhandledException += (s, e) =>
            {
                Console.WriteLine("\n\nCRITICAL APP CRASH:");
                Console.WriteLine(e.Exception.ToString());
                e.Handled = true; 
            };
        }
    }
}

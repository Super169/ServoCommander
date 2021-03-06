﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace ServoCommander
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public string locTitle;
        public static System.Version version = Assembly.GetExecutingAssembly().GetName().Version;
        public static DateTime buildDateTime = new DateTime(2000, 1, 1).Add(new TimeSpan(
                                               TimeSpan.TicksPerDay * version.Build + // days since 1 January 2000
                                               TimeSpan.TicksPerSecond * 2 * version.Revision)); // seconds since midnight, (multiply by 2 to get original) 
        public string winTitle;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            locTitle = (string)Application.Current.FindResource("WindowTitle");
            winTitle = string.Format("{0} v{1}  [{2:yyyy-MM-dd HH:mm}]  (No Copyright © 2018 Super169)", locTitle, version, buildDateTime);
            MyUtil.UTIL.KEY.AppName = "ServoCommander";
            MainWindow = new MainWindow();
            MainWindow.Title = winTitle;
            MainWindow.Closing += MainWindow_Closing;
            ShowMainWindow();
        }

        public void ShowMainWindow()
        {
            if (MainWindow.IsVisible)
            {
                if (MainWindow.WindowState == WindowState.Minimized)
                {
                    MainWindow.WindowState = WindowState.Normal;
                }
                MainWindow.Activate();
            }
            else
            {
                MainWindow.Show();
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {

        }

        public void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            ((MainWindow)MainWindow).OnWindowClosing(sender, e);
        }
    }
}

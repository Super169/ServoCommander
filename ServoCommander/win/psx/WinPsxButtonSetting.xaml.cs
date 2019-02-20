using MyUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ServoCommander
{
    /// <summary>
    /// Interaction logic for WinPsxButtonSetting.xaml
    /// </summary>
    public partial class WinPsxButtonSetting : Window
    {
        private RobotConnection robot = new RobotConnection();

        private void UpdateInfo(string msg = "", MyUtil.UTIL.InfoType iType = MyUtil.UTIL.InfoType.message, bool async = false)
        {
            if (System.Windows.Threading.Dispatcher.FromThread(Thread.CurrentThread) == null)
            {
                if (async)
                {
                    Application.Current.Dispatcher.BeginInvoke(
                      System.Windows.Threading.DispatcherPriority.Normal,
                      (Action)(() => UpdateInfo(msg, iType, async)));
                    return;
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(
                      System.Windows.Threading.DispatcherPriority.Normal,
                      (Action)(() => UpdateInfo(msg, iType, async)));
                    return;
                }
            }
            // Update UI is allowed here
            switch (iType)
            {
                case MyUtil.UTIL.InfoType.message:
                    statusBar.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x7A, 0xCC));
                    break;
                case MyUtil.UTIL.InfoType.alert:
                    statusBar.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xCA, 0x51, 0x00));
                    break;
                case MyUtil.UTIL.InfoType.error:
                    statusBar.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x00, 0x00));
                    break;

            }
            statusInfoTextBlock.Text = msg;
        }

        private void SetStatus()
        {
            bool connected = robot.isConnected;
            cboPorts.IsEnabled = !connected;
            btnRefresh.IsEnabled = !connected;
            SetButtonLabel();
        }

        private void SetButtonLabel()
        {
            bool connected = robot.isConnected;
            btnConnect.Content = LocUtil.FindResource(connected ? "btnConnectOff" : "btnConnect");
        }

        public WinPsxButtonSetting()
        {
            InitializeComponent();
            LocUtil.SetDefaultLanguage(this);
            robot.InitObject(UpdateInfo);
            robot.SetSerialPorts(cboPorts);
            SetStatus();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            robot.SetSerialPorts(cboPorts, (string)cboPorts.SelectedValue);
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            UpdateInfo();
            if (robot.isConnected)
            {
                robot.Disconnect();
            }
            else
            {
                robot.Connect((string)cboPorts.SelectedValue);
            }
            SetStatus();
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (robot.isConnected) robot.Disconnect();
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (robot.isConnected) robot.Disconnect();
            this.Close();
        }
    }
}

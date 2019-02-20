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
        private TextBox[] txtPsx = new TextBox[16];
        private byte devType = 0x01;
        private byte devId = 0x00;
        private byte cmdPing = 0x01;
        private byte cmdRead = 0x02;
        private byte cmdWrite = 0x03;
        private byte cmdStartPos = 0x46;
        private byte cmdDataLen = 0xA0;

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
            SetButtonLabel();
            btnConnect.IsEnabled = (cboPorts.Items.Count > 0);
            cboPorts.IsEnabled = !connected;
            btnRefresh.IsEnabled = !connected;
        }

        private void SetButtonLabel()
        {
            bool connected = robot.isConnected;
            btnConnect.Content = LocUtil.FindResource(connected ? "btnConnectOff" : "btnConnect");
        }

        public WinPsxButtonSetting()
        {
            InitializeComponent();
            InitObjects();
        }

        private void InitObjects()
        {
            LocUtil.SetDefaultLanguage(this);

            txtPsx[0] = txtPsx_00;
            txtPsx[1] = txtPsx_01;
            txtPsx[2] = txtPsx_02;
            txtPsx[3] = txtPsx_03;
            txtPsx[4] = txtPsx_04;
            txtPsx[5] = txtPsx_05;
            txtPsx[6] = txtPsx_06;
            txtPsx[7] = txtPsx_07;
            txtPsx[8] = txtPsx_08;
            txtPsx[9] = txtPsx_09;
            txtPsx[10] = txtPsx_10;
            txtPsx[11] = txtPsx_11;
            txtPsx[12] = txtPsx_12;
            txtPsx[13] = txtPsx_13;
            txtPsx[14] = txtPsx_14;
            txtPsx[15] = txtPsx_15;

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
            ReadSettings();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!SaveSettings()) return;
            if (robot.isConnected) robot.Disconnect();
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (robot.isConnected) robot.Disconnect();
            this.Close();
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < 16; i++)
            {
                txtPsx[i].Text = "";
            }
        }

        private bool ReadSettings()
        {
            byte[] cmd = new byte[10];
            cmd[0] = 0xA8;
            cmd[1] = 0x8A;
            cmd[2] = 0x06;
            cmd[3] = devType;
            cmd[4] = devId;
            cmd[5] = cmdRead;
            cmd[6] = cmdStartPos;
            cmd[7] = cmdDataLen;
            cmd[8] = 0x00;
            cmd[9] = 0xED;
            if (!robot.SendCommand(cmd, 170, 9, 1000))
            {
                return false;
            }
            byte[] result = robot.ReadAll();
            if (result.Length != 170)
            {
                return false;
            }
            for (int i = 0; i < 15; i++)
            {
                txtPsx[i].Text = Encoding.ASCII.GetString(result, 8 + i * 10, 10);
            }
            return true;
        }

        private bool SaveSettings()
        {
            byte[] cmd = new byte[170];
            cmd[0] = 0xA8;
            cmd[1] = 0x8A;
            cmd[2] = 0x06;
            cmd[3] = devType;
            cmd[4] = devId;
            cmd[5] = cmdWrite;
            cmd[6] = cmdStartPos;
            cmd[7] = cmdDataLen;
            for (int i = 0; i < 16; i++)
            {
                byte[] data = Encoding.ASCII.GetBytes(txtPsx[i].Text);
                int BasePos = 8 + 10 * i;
                for (int j = 0; i < 10; j++)
                {
                    cmd[BasePos + j] = (byte) (j < data.Length ? data[j] : 0);
                }
            }
            cmd[168] = 0x00;
            cmd[169] = 0xED;
            if (!robot.SendCommand(cmd, 170, 9, 1000))
            {
                UpdateInfo("Fail to save setting", UTIL.InfoType.error);
                return false;
            }
            return true;
        }

    }
}

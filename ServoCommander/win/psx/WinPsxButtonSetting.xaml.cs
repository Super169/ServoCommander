using MyUtil;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
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
            gridConnection.Background = new SolidColorBrush(connected ? Colors.LightBlue : Colors.LightGray);
            gridPSX.IsEnabled = connected;
            btnConnect.IsEnabled = (cboPorts.Items.Count > 0);
            cboPorts.IsEnabled = !connected;
            btnRefresh.IsEnabled = !connected;
            btnRefresh.Visibility = (connected ? Visibility.Hidden : Visibility.Visible);
            cboSpeed.IsEnabled = !connected;
            btnClear.IsEnabled = connected;
            btnReset.IsEnabled = connected;
            btnSave.IsEnabled = connected;
        }

        private void SetButtonLabel()
        {
            bool connected = robot.isConnected;
            // btnConnect.Content = LocUtil.FindResource(connected ? "btnConnectOff" : "btnConnect");
        }

        public WinPsxButtonSetting()
        {
            InitializeComponent();
            InitObjects();
            this.Closing += WinPsxButtonSetting_OnClosing;
        }

        private void WinPsxButtonSetting_OnClosing(object sender, CancelEventArgs e)
        {
            if (robot.isConnected) robot.Disconnect();
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
            UpdateInfo();
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
                int speed = 0;
                string sSpeed = cboSpeed.Text;
                if (!int.TryParse(sSpeed, out speed) || (speed < 9600) || (speed > 921600))
                {
                    MessageBox.Show(LocUtil.FindResource("psx.msgInvalidSpeed"), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                robot.Connect((string)cboPorts.SelectedValue, speed, Parity.None, 8, StopBits.One);
            }
            if (robot.isConnected)
            {
                GoReadSettings();
            }
            SetStatus();
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            UpdateInfo();
            GoReadSettings();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            UpdateInfo();
            if (SaveSettings())
            {
                MessageBox.Show(LocUtil.FindResource("psx.msgSaveSuccess"));
            } else
            {
                MessageBox.Show(LocUtil.FindResource("psx.msgSaveFail"), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            UpdateInfo();
            if (robot.isConnected) robot.Disconnect();
            this.Close();
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            UpdateInfo();
            for (int i = 0; i < 16; i++)
            {
                txtPsx[i].Text = "";
            }
        }

        private bool GoReadSettings()
        {
            bool result = ReadSettings();
            if (!result)
            {
                MessageBox.Show(LocUtil.FindResource("psx.msgReadFail"), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return result;
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
            cmd[8] = UTIL.CalCBCheckSum(cmd);
            cmd[9] = 0xED;


            if (!robot.SendCommand(cmd, 10, 170, 1000))
            {
                return false;
            }
            byte[] result = robot.ReadAll();
            if (result.Length != 170)
            {
                return false;
            }
            for (int i = 0; i < 16; i++)
            {
                string s = UTIL.B7Array2Str(result, 8 + i * 10, 10);
                if (s == null) s = "";
                txtPsx[i].Text = s;
            }
            return true;
        }

        private bool SaveSettings()
        {
            for (int part = 0; part < 2; part++)
            {
                byte[] cmdData = new byte[80];
                for (int i = 0; i < 8; i++)
                {
                    string sData = txtPsx[8 * part + i].Text;
                    byte[] data = UTIL.Str2B7Array(sData);
                    if (data == null)
                    {
                        UpdateInfo(string.Format(LocUtil.FindResource("psx.fmtInvalidSettings"), sData), UTIL.InfoType.error);
                        return false;
                    }
                    int BasePos = 10 * i;
                    for (int j = 0; j < 10; j++)
                    {
                        cmdData[BasePos + j] = (byte)(j < data.Length ? data[j] : 0);
                    }
                }
                byte startPos = (byte) (70 + 80 * part);
                int tryCount = 0;
                bool success = false;
                while ((tryCount < 3) && !success)
                {
                    tryCount++;
                    success = SaveRecord(startPos, cmdData);
                    if (!success)
                    {
                        Thread.Sleep(100);
                    }
                }
                if (!success)
                {
                    return false;
                }
            }
            return true;
        }


        private bool SaveRecord(byte startPos, byte[] data)
        {
            byte[] cmd = new byte[data.Length + 10];
            cmd[0] = 0xA8;
            cmd[1] = 0x8A;
            cmd[2] = (byte) (data.Length + 6);   // len, devType, devId, cmdWrite, cmdStartPos, cmdDataLen - 6 bytes
            cmd[3] = devType;
            cmd[4] = devId;
            cmd[5] = cmdWrite;
            cmd[6] = startPos;
            cmd[7] = (byte) data.Length;

            for (int i = 0; i < data.Length; i++) cmd[8 + i] = data[i];
            cmd[data.Length + 8] = UTIL.CalCBCheckSum(cmd);
            cmd[data.Length + 9] = 0xED;

            if (!robot.SendCommand(cmd, data.Length + 10, 9, 1000)) return false;
            byte[] result = robot.ReadAll();
            if (result.Length != 9) return false;
            if (result[6] != 0) return false;
            return true;
        }

        private void tb_PreviewInteger(object sender, TextCompositionEventArgs e)
        {
            UTIL.INPUT.PreviewInteger(ref e);
        }

        private void tb_PreviewKeyDown_nospace(object sender, KeyEventArgs e)
        {
            UTIL.INPUT.PreviewKeyDown_nospace(ref e);
        }

    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ServoCommander
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int checkId;
        int minId;
        Timer checkTimer = new Timer();

        public MainWindow()
        {
            InitializeComponent();
            InitializeSerialPort();
            FindPorts((string)Util.ReadRegistry(Util.KEY.LAST_CONNECTION));
            txtMaxId.Text = CONST.MAX_SERVO.ToString();
            sliderAdjValue.Value = 0;
            txtAdjAngle.Text = "0000";
            InitTimer();
            SetStatus();
        }

        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            if (serialPort.IsOpen) serialPort.Close();
        }

        private void InitTimer()
        {
            checkTimer.Interval = 10;
            checkTimer.Tick += new EventHandler(checkTimer_TickHandler);
            checkTimer.Enabled = false;
            checkTimer.Stop();
        }

        private void findPortButton_Click(object sender, RoutedEventArgs e)
        {
            FindPorts((string)portsComboBox.SelectedValue);
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            UpdateInfo();
            if (serialPort.IsOpen)
            {
                SerialDisconnect();
            }
            else
            {
                SerialConnect((string)portsComboBox.SelectedValue);
            }
            SetStatus();
        }

        private void SetStatus()
        {
            bool connected = serialPort.IsOpen;
            portsComboBox.IsEnabled = !connected;
            findPortButton.IsEnabled = !connected;
            findPortButton.Visibility = (connected ? Visibility.Hidden : Visibility.Visible);
            btnConnect.Content = (connected ? "斷開" : "連線");
            gridConnection.Background = new SolidColorBrush(connected ? Colors.LightBlue : Colors.LightGray);
            gridInput.IsEnabled = true;  // allow to test command all the time
            gridInput.Background = new SolidColorBrush(connected ? Colors.LightGreen : Colors.LightSalmon);
            btnExecute.Content = (connected ? "發送指令 (_S)" : "生成指令 (_S)");
            btnCheckID.IsEnabled = connected;
        }

        private void tb_PreviewCommand(object sender, TextCompositionEventArgs e)
        {
            e.Handled = new Regex("[^0-9A-Fa-f.]+").IsMatch(e.Text);
        }

        private void tb_PreviewInteger(object sender, TextCompositionEventArgs e)
        {
            e.Handled = new Regex("[^0-9]+").IsMatch(e.Text);
        }

        private void tb_PreviewHex(object sender, TextCompositionEventArgs e)
        {
            e.Handled = new Regex("[^0-9A-Fa-f]+").IsMatch(e.Text);
        }

        private void tb_PreviewHexMix(object sender, TextCompositionEventArgs e)
        {
            e.Handled = new Regex("[^0-9A-Fa-f.]+").IsMatch(e.Text);
        }

        private void tb_PreviewKeyDown_nospace(object sender, System.Windows.Input.KeyEventArgs e)

        {

            e.Handled = (e.Key == Key.Space);
        }

        private bool GetCommand(out byte[] command)
        {
            string sCommand = txtCommand.Text.Trim();
            command = new byte[10];

            try
            {
                Regex rx = new Regex("\\s+");
                string input = rx.Replace(sCommand, " ");
                string[] sData = input.Split(' ');
                command[0] = 0xFA;
                command[1] = 0xAF;

                for (int i = 0; i < 6; i++)
                {
                    if (i < sData.Length)
                    {
                        command[2 + i] = Util.GetInputByte(sData[i]);
                    }
                    else
                    {
                        command[2 + i] = 0x00;
                    }
                }
                if (command[3] == 0x01)
                {
                    if (command[5] > command[7]) command[7] = command[5];
                }
                command[8] = Util.CalCheckSum(command);
                command[9] = 0xED;
                txtPreview.Text = Util.GetByteString(command);

            }
            catch (Exception)
            {
                txtPreview.Text = "";
                return false;
            }
            return true;
        }

        private void txtCommand_TextChanged(object sender, TextChangedEventArgs e)
        {
            rbFreeInput.IsChecked = true;
            byte[] command;
            GetCommand(out command);
        }

        private void btnExecute_Click(object sender, RoutedEventArgs e)
        {
            UpdateInfo();
            UpdateMaxId();

            if (rbFreeInput.IsChecked == true)
            {
                ExecuteFreeInput();
            }
            else
            {
                int id = GetId();
                if (id < 0) return;

                if (rbCheckVersion.IsChecked == true)
                {
                    GetVersion(id);
                }
                else if (rbChangeId.IsChecked == true)
                {
                    int newId = GetIdValue(txtNewId.Text.Trim());
                    if ((newId <= 0) || (newId > CONST.MAX_SERVO))
                    {
                        UpdateInfo("Invalid New ID", Util.InfoType.error);
                        return;
                    }
                    if (id == newId)
                    {
                        UpdateInfo("New Id cannot be the same as existing Id", Util.InfoType.error);
                        return;
                    }
                    ChangeId(id, newId);
                }
                else if (rbMove.IsChecked == true)
                {
                    GoMove(id);
                }
                else if (rbGetAngle.IsChecked == true)
                {
                    GetAngle(id);
                }
                else if (rbGetAdjAngle.IsChecked == true)
                {
                    GetAdjAngle(id);
                }
                else if (rbSetAdjAngle.IsChecked == true)
                {
                    SetAdjAngle(id);
                }
            }

        }

        int GetIdValue(string data)
        {
            int id = 0;
            if ((data == null) | (data == "")) return 0;
            try
            {
                id = int.Parse(data);
            }
            catch (Exception)
            {
                return -1;
            }
            return id;

        }

        int GetId(bool allowZero = false)
        {
            int id = GetIdValue(txtId.Text.Trim());
            if ((id < 0) || (id > CONST.MAX_SERVO) || ((id == 0) && !allowZero))
            {
                UpdateInfo("Invalid ID", Util.InfoType.error);
                return -1;
            }
            return id;
        }

        private void SendCmd(byte[] cmd, uint expectCnt)
        {
            bool connected = serialPort.IsOpen;
            cmd[8] = Util.CalCheckSum(cmd);
            cmd[9] = 0xED;

            AppendLog("\n" + (connected ? ">> " : "") + Util.GetByteString(cmd) + "\n");
            ClearSerialBuffer();
            if (connected)
            {
                SendCommand(cmd, 10, expectCnt);
                string msg = "<<";
                if (receiveBuffer.Count > 0)
                {
                    byte[] result = receiveBuffer.ToArray();
                    msg += Util.GetByteString(result);
                }
                AppendLog(msg);
            }
        }

        private void adjAngle_Changed(object sender, RoutedEventArgs e)
        {
            string adjAngle = txtAdjAngle.Text;
            try
            {
                int adjValue = Convert.ToInt32(adjAngle, 16);
                bool validInput = (((adjValue >= 0x0000) && (adjValue <= 0x0130)) ||
                                   ((adjValue >= 0xFED0) && (adjValue <= 0xFFFF)));
                txtAdjAngle.Background = new SolidColorBrush((validInput ? Colors.White : Colors.LightPink));
                if (validInput)
                {
                    sliderAdjValue.Value = adjValue;
                }

            }
            catch (Exception)
            {
                txtAdjAngle.Background = new SolidColorBrush(Colors.LightPink);
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int n = (int)((Slider)sender).Value;

            if (n == 0)
            {
                txtAdjMsg.Text = String.Format("沒有偏移");
            }
            else if (n > 0)
            {
                txtAdjMsg.Text = String.Format("正向偏移 {0}", n);
            }
            else
            {
                txtAdjMsg.Text = String.Format("反向偏移 {0}", -n);
            }
            if (n < 0) n += 65536;
            txtAdjAngle.Text = n.ToString("X4");
        }

        private void UpdateMaxId()
        {
            if (int.TryParse(txtMaxId.Text.Trim(), out int maxId))
            {
                CONST.MAX_SERVO = maxId;
            }
        }

        private void btnCheckID_Click(object sender, RoutedEventArgs e)
        {
            UpdateMaxId();
            checkId = 1;
            minId = 0;
            gridConnection.IsEnabled = false;
            gridInput.IsEnabled = false;
            UpdateInfo("Checking ID, please wait......", Util.InfoType.alert);
            checkTimer.Enabled = true;
            checkTimer.Start();
        }

        private void checkTimer_TickHandler(object sender, EventArgs e)
        {
            checkTimer.Stop();

            byte[] cmd = { 0xFC, 0xCF, (byte)checkId, 1, 0, 0, 0, 0, 0, 0xED };
            SendCmd(cmd, 10);
            if (receiveBuffer.Count == 10)
            {
                AppendLog(String.Format("Servo {0} detected", checkId));
                if (minId == 0)
                {
                    minId = checkId;
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(
                            System.Windows.Threading.DispatcherPriority.Normal,
                            (Action)(() => txtId.Text = minId.ToString()));
                }
            }

            checkId++;
            if (checkId > CONST.MAX_SERVO)
            {
                checkTimer.Enabled = false;
                gridConnection.IsEnabled = true;
                gridInput.IsEnabled = true;
            }
            else
            {
                checkTimer.Start();
            }
        }


    }
}

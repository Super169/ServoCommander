using System;
using System.Collections.Generic;
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
using MyUtil;

namespace ServoCommander.uc
{
    /// <summary>
    /// Interaction logic for UcCommand_UBTech.xaml
    /// </summary>
    public partial class UcCommand_UBTech : UcCommand__base
    {
        // Timer checkTimer = new Timer();


        public UcCommand_UBTech()
        {
            InitializeComponent();
            txtMaxId.Text = CONST.MAX_SERVO.ToString();
            sliderAdjValue.Value = 0;
            txtAdjAngle.Text = "0000";

        }

        #region Check Servo

        private void btnCheckID_Click(object sender, RoutedEventArgs e)
        {
            StartCheckServo(txtMaxId.Text);
        }
        protected override bool DetectServo(int id)
        {
            return GetVersion(id);
        }

        protected override void UpdateMinId(int id)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                (Action)(() => txtId.Text = minId.ToString()));
        }

        #endregion Check Servo

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

        public override void ExecuteCommand()
        {
            if (!AllowExecution()) return;

            if (rbFreeInput.IsChecked == true)
            {
                ExecuteFreeInput();
            }
            else
            {
                int id = GetId(true);
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
                        UpdateInfo("Invalid New ID", UTIL.InfoType.error);
                        return;
                    }
                    if (id == newId)
                    {
                        UpdateInfo("New Id cannot be the same as existing Id", UTIL.InfoType.error);
                        return;
                    }
                    ChangeId(id, newId);
                }
                else if (rbGetAngle.IsChecked == true)
                {
                    GetAngle(id);
                }
                else if (rbMove.IsChecked == true)
                {
                    GoMove(id);
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

        int GetId(bool allowZero = false)
        {
            int id = GetIdValue(txtId.Text.Trim());
            if ((id < 0) || (id > CONST.MAX_SERVO) || ((id == 0) && !allowZero))
            {
                UpdateInfo("Invalid ID", UTIL.InfoType.error);
                return -1;
            }
            return id;
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

        private void SendCommand(byte[] cmd, uint expectCnt)
        {
            bool connected = robot.isConnected;
            cmd[8] = UTIL.CalUBTCheckSum(cmd);
            cmd[9] = 0xED;

            AppendLog("\n" + (connected ? ">> " : "") + UTIL.GetByteString(cmd) + "\n");
            robot.ClearRxBuffer();
            if (connected)
            {
                robot.SendCommand(cmd, 10, expectCnt);
                string msg = "<< ";
                if (robot.Available > 0)
                {
                    byte[] result = robot.PeekAll();
                    msg += UTIL.GetByteString(result);
                }
                AppendLog(msg);
            }
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
                        command[2 + i] = UTIL.GetInputByte(sData[i]);
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
                command[8] = UTIL.CalUBTCheckSum(command);
                command[9] = 0xED;
                txtPreview.Text = UTIL.GetByteString(command);

            }
            catch (Exception)
            {
                txtPreview.Text = "";
                return false;
            }
            return true;
        }

        // FC CF {id} 01 00 00 00 00 {sum} ED
        private bool GetVersion(int id)
        {
            // byte[] cmd = { 0xFC, 0xCF, (byte)id, 0x01, 0, 0, 0, 0, 0, 0xED };
            byte[] cmd = { 0xFC, 0xCF, (byte)id, 0x01, 0, 0, 0, 0, 0, 0xED };
            SendCommand(cmd, 10);
            if (robot.Available == 10)
            {
                byte[] buffer = robot.ReadAll();
                string result = String.Format("版本號為: {0:X2} {1:X2} {2:X2} {3:X2}\n",
                                              buffer[4], buffer[5], buffer[6], buffer[7]);
                AppendLog(result);
                return true;
            }
            return false;
        }

        // FA AF {id} CD 00 {newId} 00 00 {sum} ED
        private void ChangeId(int id, int newId)
        {
            string msg = String.Format("把 舵機編號 {0} 修改為 舵機編號 {1} \n", id, newId);
            if (!MessageConfirm(msg)) return;

            byte[] cmd = { 0xFA, 0xAF, (byte)id, 0xCD, 0, (byte)newId, 0, 0, 0, 0xED };
            SendCommand(cmd, 10);
            if (robot.Available == 10)
            {
                byte[] buffer = robot.ReadAll();
                if (buffer[3] == 0xAA)
                {
                    string result = String.Format("舵機編號 {0} 已成功修改為 舵機編號 {1} \n", id, newId);
                    AppendLog(result);
                    txtId.Text = newId.ToString();
                    UpdateInfo(result, UTIL.InfoType.alert);
                }
            }
        }

        private void GetAngle(int id)
        {
            byte[] cmd = { 0xFA, 0xAF, (byte)id, 2, 0, 0, 0, 0, 0, 0xED };
            SendCommand(cmd, 10);
            if (robot.Available == 10)
            {
                byte[] buffer = robot.ReadAll();
                string result = String.Format("舵機角度:  目前為: {0:X2} {1:X2} ({1}度), 實際為: {2:X2} {3:X2} ({3}度)\n",
                                              buffer[4], buffer[5], buffer[6], buffer[7]);
                string hexAngle = String.Format("{0:X2}", buffer[7]);
                txtAdjPreview.Text = hexAngle;
                AppendLog(result);
            }
        }


        private void GoMove(int id)
        {
            byte[] cmd = { 0xFA, 0xAF, (byte)id, 1, 0, 0, 0, 0, 0, 0xED };
            if (((txtMoveAngle.Text == null) || (txtMoveAngle.Text.Trim() == "")) ||
                ((txtMoveTime.Text == null) || (txtMoveTime.Text.Trim() == "")))
            {
                AppendLog("\n請先設定要移動的目標角度及移動時間");
                return;
            }
            try
            {
                byte angle = UTIL.GetInputByte(txtMoveAngle.Text);
                byte time = UTIL.GetInputByte(txtMoveTime.Text);
                cmd[4] = angle;
                cmd[5] = cmd[7] = time;
                SendCommand(cmd, 1);
                if (robot.Available == 1)
                {
                    byte[] buffer = robot.ReadAll();
                    if (buffer[0] == (0xAA + id))
                    {
                        txtAdjPreview.Text = String.Format("{0:X2}", angle);
                        AppendLog(String.Format("舵機 {0} 成功移動到 {1} 度位置", id, angle));
                    }
                    else
                    {
                        AppendLog(String.Format("舵機 {0} 移動失敗", id));
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog("\nERR: " + ex.Message);
            }
        }


        private void GetAdjAngle(int id)
        {
            byte[] cmd = { 0xFA, 0xAF, (byte)id, 0xD4, 0, 0, 0, 0, 0, 0xED };
            SendCommand(cmd, 10);
            if (robot.Available == 10)
            {
                byte[] buffer = robot.ReadAll();
                int adjValue = buffer[6] * 256 + buffer[7];
                string adjMsg = "";
                if (adjValue == 0)
                {
                    adjMsg = "沒有偏移";
                    sliderAdjValue.Value = 0;
                }
                else if ((adjValue >= 0x0000) && (adjValue <= 0x0130))
                {
                    adjMsg = "正向 " + adjValue.ToString();
                    sliderAdjValue.Value = adjValue;
                }
                else if ((adjValue >= 0xFED0) && (adjValue <= 0xFFFF))
                {
                    adjMsg = "反向 " + (65536 - adjValue).ToString();
                    sliderAdjValue.Value = (adjValue - 65536);
                }
                else
                {
                    adjMsg = "偏移量異常";
                }
                string result = String.Format("偏移校正:  {2:X2} {3:X2} 即 {4} \n",
                                              buffer[4], buffer[5], buffer[6], buffer[7], adjMsg);
                AppendLog(result);
            }

        }

        private void SetAdjAngle(int id)
        {
            int adjValue = (int)sliderAdjValue.Value;
            if (adjValue < 0) adjValue += 65536;

            byte adjHigh = (byte)(adjValue / 256);
            byte adjLow = (byte)(adjValue % 256);

            byte[] cmd = { 0xFA, 0xAF, (byte)id, 0xD2, 0, 0, adjHigh, adjLow, 0, 0xED };
            SendCommand(cmd, 10);
            if (robot.Available == 10)
            {
                byte[] buffer = robot.ReadAll();
                if (buffer[3] != 0xAA)
                {
                    AppendLog("偏移量設置失敗");
                }
            }
            else
            {
                return;
            }
            AppendLog("偏移量設置成功");

            if ((txtAdjPreview.Text == null) || (txtAdjPreview.Text.Trim() == "")) return;

            System.Threading.Thread.Sleep(500);
            txtMoveAngle.Text = txtAdjPreview.Text;
            txtMoveTime.Text = "28";
            GoMove(id);
        }

        private void ExecuteFreeInput()
        {
            byte[] command;
            if (GetCommand(out command))
            {
                SendCommand(command, 10);  // assume 10 byte returned
            }
        }

    }
}


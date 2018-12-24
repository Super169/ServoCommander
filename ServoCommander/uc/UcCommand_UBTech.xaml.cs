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
using System.Windows.Threading;
using MyUtil;

namespace ServoCommander.uc
{
    /// <summary>
    /// Interaction logic for UcCommand_UBTech.xaml
    /// </summary>
    public partial class UcCommand_UBTech : UcCommand__base
    {
        // Timer checkTimer = new Timer();
        DispatcherTimer sliderMoveTimer = new DispatcherTimer();
        private byte sliderMoveId = 0;

        public UcCommand_UBTech()
        {
            InitializeComponent();
            txtMaxId.Text = CONST.MAX_SERVO.ToString();
            sliderAdjValue.Value = 0;
            txtAdjAngle.Text = "0000";
            InitLocalTimer();

        }

        private void InitLocalTimer()
        {
            sliderMoveTimer.Tick += new EventHandler(sliderMoveTimer_Tick);
            sliderMoveTimer.Interval = TimeSpan.FromMilliseconds(20);
            sliderMoveTimer.Stop();

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

        private void sliderAdjValue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
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
                int id = GetId((cbxSupportBroadcast.IsChecked == true));
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
                else if (rbRotate.IsChecked == true)
                {
                    GoRotate(id);
                }
                else if (rbGetAdjAngle.IsChecked == true)
                {
                    GetAdjAngle(id);
                }
                else if (rbSetAdjAngle.IsChecked == true)
                {
                    SetAdjAngle(id);
                }
                else if (rbAutoAdjAngle.IsChecked == true)
                {
                    AutoAdjAngle(id);
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
            if ((id > 0) && (robot.Available == 10))
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
            string msg = String.Format("把 {0} 修改成 舵機編號 {1} \n", (id == 0 ? "所有舵機" : "舵機編號 " + id.ToString()), newId);
            if (!MessageConfirm(msg)) return;

            byte[] cmd = { 0xFA, 0xAF, (byte)id, 0xCD, 0, (byte)newId, 0, 0, 0, 0xED };
            SendCommand(cmd, 10);
            if ((id > 0) && (robot.Available == 10))
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
            byte angle;
            byte[] buffer;
            if (!UBTGetAngle(id, out angle, out buffer)) return;
            int iAngle = (buffer[4] << 8) | buffer[5];
            int iActual = (buffer[6] << 8) | buffer[7];
            string result = String.Format("舵機角度:  目前為: {0:X2} {1:X2} ({2}度), 實際為: {3:X2} {4:X2} ({5}度)\n",
                                          buffer[4], buffer[5], iAngle, buffer[6], buffer[7], iActual);
            // string hexAngle = String.Format("{0:X2}", buffer[7]);
            //txtAdjPreview.Text = buffer[7].ToString();
            //txtAutoAdjAngle.Text = buffer[7].ToString();
            SetCurrAngle(buffer[7]);
            AppendLog(result);
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
                byte angle = (byte)UTIL.GetInputInteger(txtMoveAngle.Text);
                int iTime = UTIL.GetInputInteger(txtMoveTime.Text);
                byte time = (byte)(iTime / 20);
                cmd[4] = angle;
                cmd[5] = cmd[7] = time;
                SendCommand(cmd, 1);
                if ((id != 0) && (robot.Available == 1))
                {
                    byte[] buffer = robot.ReadAll();
                    if (buffer[0] == (0xAA + id))
                    {
                        //txtAdjPreview.Text = angle.ToString();
                        //txtAutoAdjAngle.Text = angle.ToString();
                        SetCurrAngle(angle);
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


        private void GoRotate(int id)
        {
            byte[] cmd = { 0xFA, 0xAF, (byte)id, 1, 0, 0, 0, 0, 0, 0xED };
            if (string.IsNullOrWhiteSpace(txtRotateSpeed.Text))
            {
                AppendLog("\n請先設定要轉動的速度\n請輸入速度 0 停止轉動");
                return;
            }
            try
            {
                int iDirection = cboRotateDirection.SelectedIndex;
                int iSpeed = UTIL.GetInputInteger(txtRotateSpeed.Text);
                if (iSpeed > 2000)
                {
                    AppendLog("\n請轉入 0 - 2000 的速度");
                    return;
                }
                cmd[4] = (byte)(iDirection == 0 ? 0xFD : 0xFE);
                cmd[6] = (byte)(iSpeed >> 8 & 0xFF);
                cmd[7] = (byte)(iSpeed & 0xFF);
                SendCommand(cmd, 1);
                if ((id > 0) && (robot.Available == 1))
                {
                    byte[] buffer = robot.ReadAll();
                    if (buffer[0] == (0xAA + id))
                    {
                        AppendLog(String.Format("舵機 {0} 成功{1}轉動", id, (iSpeed == 0 ? "停止" : "開始")));
                    }
                    else
                    {
                        AppendLog(String.Format("舵機 {0} 轉動失敗", id));
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
            UInt16 adjValue;
            byte[] buffer;
            if (!UBTGetAdjAngle(id, out adjValue, out buffer)) return;
            if (id == 0) return;

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

        private void SetAdjAngle(int id)
        {
            int adjValue = (int)sliderAdjValue.Value;
            if (!UBTSetAdjAngle(id, adjValue))
            {
                if (robot.Available != 1) return;
                if (id == 0) return;
                AppendLog("偏移量設置失敗");
                return;
            }
            AppendLog("偏移量設置成功");

            if ((txtAdjPreview.Text == null) || (txtAdjPreview.Text.Trim() == "")) return;

            System.Threading.Thread.Sleep(500);
            txtMoveAngle.Text = txtAdjPreview.Text;
            txtMoveTime.Text = "1000";
            GoMove(id);
        }

        private void AutoAdjAngle(int id)
        {
            if (id == 0)
            {
                AppendLog("\n自動設定偏移量只能針對個別舵機, 不支援廣播, 請先選擇舵機");
                return;
            }
            if (!robot.isConnected)
            {
                AppendLog("\n自動設定偏移量必須要連上舵機才可以進行");
                return;
            }
            if ((txtAutoAdjAngle.Text == null) || (txtAutoAdjAngle.Text.Trim() == ""))
            {
                AppendLog("\n請先輸入要設定的角度");
                return;
            }
            try
            {
                int iFixAngle = (byte)UTIL.GetInputInteger(txtAutoAdjAngle.Text);
                if (iFixAngle > CONST.UBT.MAX_ANGLE)
                {
                    AppendLog(String.Format("輸入角度 {0} 過大了, 可設定角度為 0 - {1} 度", iFixAngle, CONST.UBT.MAX_ANGLE));
                    return;
                }

                // Get Crrent Adj Angle
                byte[] buffer;
                UInt16 adj;
                if (!UBTGetAdjAngle(id, out adj, out buffer))
                {
                    AppendLog("讀取當前偏移量失敗");
                    return;
                }

                int adjValue = 0;
                if ((adj >= 0x0000) && (adj <= 0x0130))
                {
                    adjValue = adj;
                }
                else if ((adj >= 0xFED0) && (adj <= 0xFFFF))
                {
                    adjValue = (adj - 65536);
                }
                else
                {
                    AppendLog(string.Format("Invalid adjustment {0:X4}", adj));
                    return;
                }

                byte currAngle;
                if (!UBTGetAngle(id, out currAngle, out buffer))
                {
                    AppendLog("讀取當前角度失敗");
                    return;
                }

                int delta = cboAutoAdjDelta.SelectedIndex;
                // adjValue = 3 * angle
                int actualValue = currAngle * 3 + adjValue;
                int actualAngle = actualValue / 3;
                int actualDelta = actualValue % 3;
                int newValue = iFixAngle * 3 + delta;
                int newAdjValue = actualValue - newValue;
                if (!UBTSetAdjAngle(id, newAdjValue))
                {
                    AppendLog("設置偏移量失敗");
                    return;
                }
                AppendLog(string.Format("舵機 {0} 當前機械角度為: {1}度[{2}], 現設定為: {3}度[{4}]", id, actualAngle, actualDelta, iFixAngle, delta));
                System.Threading.Thread.Sleep(100);
                GetAdjAngle(id);
            }
            catch (Exception ex)
            {
                AppendLog("\nERR: " + ex.Message);
            }
        }

        private void ExecuteFreeInput()
        {
            byte[] command;
            if (GetCommand(out command))
            {
                SendCommand(command, 10);  // assume 10 byte returned
            }
        }

        private void sliderAngle_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider sAngle = (Slider)sender;
            lblAngle.Content = Math.Round(sAngle.Value);
        }

        private void SetCurrAngle(byte angle)
        {
            sliderAngle.Value = angle;
            lblAngle.Content = angle.ToString();
            txtAdjPreview.Text = angle.ToString();
            txtAutoAdjAngle.Text = angle.ToString();
        }

        private int captureMode = 0;

        private void sliderAngle_GotMouseCapture(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Slider sAngle = (Slider)sender;
            string sId = txtId.Text.Trim();
            if (sId == "") return;
            int id;
            if (!int.TryParse(sId, out id)) return;
            if ((id < 0) || (id > CONST.MAX_SERVO)) return;
            sliderMoveId = (byte) id;
            UpdateInfo(string.Format("Slider got mouse capture for id {0}", sliderMoveId));
            captureMode = 2;
            sliderMoveTimer.Start();
        }

        private void sliderMoveTimer_Tick(object sender, EventArgs e)
        {
            sliderMoveTimer.Stop();

            double dblAngle = Math.Round(sliderAngle.Value);
            byte angle = (byte)dblAngle;
            byte timeUBT = 0;
            byte[] cmd = { 0xFA, 0xAF, (byte) sliderMoveId, 1, angle, timeUBT, 0, timeUBT, 0, 0xED };
            SendCommand(cmd, 0, false);
            if (captureMode > 0)
            {
                // extra one time after stop
                if (captureMode == 1) captureMode = 0;
                sliderMoveTimer.Start();
            }
        }

        private void sliderAngle_LostMouseCapture(object sender, System.Windows.Input.MouseEventArgs e)
        {
            captureMode = 1;
        }
    }
}


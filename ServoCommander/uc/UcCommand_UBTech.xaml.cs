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
                txtAdjMsg.Text = String.Format(LocUtil.FindResource("ubt.msgNoAdjust"));
            }
            else if (n > 0)
            {
                txtAdjMsg.Text = String.Format(LocUtil.FindResource("ubt.msgPosAdjust"), n);
            }
            else
            {
                txtAdjMsg.Text = String.Format(LocUtil.FindResource("ubt.msgNegAdjust"), -n);
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
                        UpdateInfo(LocUtil.FindResource("ubt.msgInvalidNewId"), UTIL.InfoType.error);
                        return;
                    }
                    if (id == newId)
                    {
                        UpdateInfo(LocUtil.FindResource("ubt.msgNewIdSame"), UTIL.InfoType.error);
                        return;
                    }
                    ChangeId(id, newId);
                }
                else if (rbLedOn.IsChecked == true)
                {
                    SetLed(id, true);
                }
                else if (rbLedOff.IsChecked == true)
                {
                    SetLed(id, false);
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
                UpdateInfo(LocUtil.FindResource("ubt.msgInvalidId"), UTIL.InfoType.error);
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
                string format = LocUtil.FindResource("ubt.msgShowVersion");
                string result = String.Format(format,
                                              buffer[4], buffer[5], buffer[6], buffer[7]);
                AppendLog(result);
                return true;
            }
            return false;
        }

        // FA AF {id} CD 00 {newId} 00 00 {sum} ED
        private void ChangeId(int id, int newId)
        {
            string msg = String.Format(LocUtil.FindResource("ubt.msgConfirmChangeId"), 
                                      (id == 0 ? LocUtil.FindResource("ubt.msgAllServo") : LocUtil.FindResource("ubt.msgServoId") + id.ToString()), newId);
            if (!MessageConfirm(msg)) return;

            byte[] cmd = { 0xFA, 0xAF, (byte)id, 0xCD, 0, (byte)newId, 0, 0, 0, 0xED };
            SendCommand(cmd, 10);
            if ((id > 0) && (robot.Available == 10))
            {
                byte[] buffer = robot.ReadAll();
                if (buffer[3] == 0xAA)
                {
                    string result = String.Format(LocUtil.FindResource("ubt.msgChangeIdSuccess"), id, newId);
                    AppendLog(result);
                    txtId.Text = newId.ToString();
                    UpdateInfo(result, UTIL.InfoType.alert);
                }
            }
        }

        private void SetLed(int id, bool mode)
        {
            byte[] cmd = { 0xFA, 0xAF, (byte)id, 0x04, (byte) (mode ? 0x00 : 0x01), 0, 0, 0, 0, 0xED };
            SendCommand(cmd, 10);
            if ((id != 0) && (robot.Available == 1))
            {
                byte[] buffer = robot.ReadAll();
                string action = LocUtil.FindResource(mode ? "ubt.msgLedOn" : "ubt.msgLedOff");
                if (buffer[0] == (0xAA + id))
                {
                    AppendLog(String.Format(LocUtil.FindResource("ubt.msgSetLedSuccess"), id, action));
                }
                else
                {
                    AppendLog(String.Format(LocUtil.FindResource("ubt.msgSetLedFail"), id, action));
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
            string result = String.Format(LocUtil.FindResource("ubt.msgShowAngle"),
                                          buffer[4], buffer[5], iAngle, buffer[6], buffer[7], iActual);
            SetCurrAngle(buffer[7]);
            AppendLog(result);
        }

        private void GoMove(int id)
        {
            byte[] cmd = { 0xFA, 0xAF, (byte)id, 1, 0, 0, 0, 0, 0, 0xED };
            if (((txtMoveAngle.Text == null) || (txtMoveAngle.Text.Trim() == "")) ||
                ((txtMoveTime.Text == null) || (txtMoveTime.Text.Trim() == "")))
            {
                AppendLog(LocUtil.FindResource("ubt.msgGoMoveParameter"));
                return;
            }
            try
            {
                byte angle = (byte)UTIL.GetInputInteger(txtMoveAngle.Text);
                int timeMs = UTIL.GetInputInteger(txtMoveTime.Text);
                /*
                byte time = (byte)(timeMs / 20);
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
                        AppendLog(String.Format(LocUtil.FindResource("ubt.msgGoMoveSuccess"), id, angle));
                    }
                    else
                    {
                        AppendLog(String.Format(LocUtil.FindResource("ubt.msgGoMoveFail"), id));
                    }
                }
                */
                int result = UBTGoMove(id, angle, timeMs);
                switch (result)
                {
                    case 0:
                        SetCurrAngle(angle);
                        AppendLog(String.Format(LocUtil.FindResource("ubt.msgGoMoveSuccess"), id, angle));
                        break;
                    case 1:
                        AppendLog(String.Format(LocUtil.FindResource("ubt.msgGoMoveFail"), id));
                        break;

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
                AppendLog(LocUtil.FindResource("ubt.msgGoRotateParameter"));
                return;
            }
            try
            {
                int iDirection = cboRotateDirection.SelectedIndex;
                int iSpeed = UTIL.GetInputInteger(txtRotateSpeed.Text);
                if (iSpeed > 2000)
                {
                    AppendLog(LocUtil.FindResource("ubt.msgGoRotateSpeed"));
                    return;
                }
                cmd[4] = (byte)(iDirection == 0 ? 0xFD : 0xFE);
                cmd[6] = (byte)(iSpeed >> 8 & 0xFF);
                cmd[7] = (byte)(iSpeed & 0xFF);
                SendCommand(cmd, 1);
                if ((id > 0) && (robot.Available == 1))
                {
                    byte[] buffer = robot.ReadAll();
                    string action = (iSpeed == 0 ? LocUtil.FindResource("msgStop") : LocUtil.FindResource("ubt.msgStart"));
                    if (buffer[0] == (0xAA + id))
                    {
                        AppendLog(String.Format(LocUtil.FindResource("ubt.msgGoRotateSuccess"), id, action));
                    }
                    else
                    {
                        AppendLog(String.Format(LocUtil.FindResource("ubt.msgGoRotateFail"), id, action));
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
                adjMsg = LocUtil.FindResource("ubt.msgNoAdjust");
                sliderAdjValue.Value = 0;
            }
            else if ((adjValue >= 0x0000) && (adjValue <= 0x0130))
            {
                adjMsg = string.Format(LocUtil.FindResource("ubt.msgPosAdjust"), adjValue);
                sliderAdjValue.Value = adjValue;
            }
            else if ((adjValue >= 0xFED0) && (adjValue <= 0xFFFF))
            {
                adjMsg = string.Format(LocUtil.FindResource("ubt.msgNegAdjust"), (65536 - adjValue));
                sliderAdjValue.Value = (adjValue - 65536);
            }
            else
            {
                adjMsg = LocUtil.FindResource("ubt.msgInvalidAdjust");
            }
            string result = String.Format(LocUtil.FindResource("ubt.msgShowAdjust"),
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
                AppendLog(LocUtil.FindResource("ubt.msgSetAdjustFail"));
                return;
            }
            AppendLog(LocUtil.FindResource("ubt.msgSetAdjustSuccess"));

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
                AppendLog(LocUtil.FindResource("ubt.msgAutoAdjustNoBroadcast"));
                return;
            }
            if (!robot.isConnected)
            {
                AppendLog(LocUtil.FindResource("ubt.msgAutoAdjustMustConnect"));
                return;
            }
            if ((txtAutoAdjAngle.Text == null) || (txtAutoAdjAngle.Text.Trim() == ""))
            {
                AppendLog(LocUtil.FindResource("ubt.msgAutoAdjustRequireAngle"));
                return;
            }
            try
            {
                int iFixAngle = (byte)UTIL.GetInputInteger(txtAutoAdjAngle.Text);
                if (iFixAngle > CONST.UBT.MAX_ANGLE)
                {
                    AppendLog(String.Format(LocUtil.FindResource("ubt.msgAutoInvalidAngle"), iFixAngle, CONST.UBT.MAX_ANGLE));
                    return;
                }

                // Get Crrent Adj Angle
                byte[] buffer;
                UInt16 adj;
                if (!UBTGetAdjAngle(id, out adj, out buffer))
                {
                    AppendLog(LocUtil.FindResource("ubt.msgGetAdjustFail"));
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
                    AppendLog(string.Format(LocUtil.FindResource("ubt.msgCurrentAdjustInvalid"), adj));
                    return;
                }

                byte currAngle;
                if (!UBTGetAngle(id, out currAngle, out buffer))
                {
                    AppendLog(LocUtil.FindResource("ubt.msgGetAngleFail"));
                    return;
                }

                int delta = cboAutoAdjDelta.SelectedIndex;
                int actualValue = currAngle * 3 + adjValue;
                int actualAngle = actualValue / 3;
                int actualDelta = actualValue % 3;
                int newValue = iFixAngle * 3 + delta;
                int newAdjValue = actualValue - newValue;
                if (!UBTSetAdjAngle(id, newAdjValue))
                {
                    AppendLog(LocUtil.FindResource("ubt.msgSetAdjustFail"));
                    return;
                }
                AppendLog(string.Format(LocUtil.FindResource("ubt.msgAutoAdjustComplete"), id, actualAngle, actualDelta, iFixAngle, delta));
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


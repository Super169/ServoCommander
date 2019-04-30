using MyUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ServoCommander.uc
{
    /// <summary>
    /// Interaction logic for UcCommand_HaiLzd.xaml
    /// </summary>
    public partial class UcCommand_HaiLzd : UcCommand__base
    {
        public UcCommand_HaiLzd()
        {
            InitializeComponent();
            txtMaxId.Text = CONST.MAX_SERVO.ToString();
        }

        #region Check Servo

        private void txtMaxId_TextChanged(object sender, TextChangedEventArgs e)
        {
            SetMaxId(txtMaxId.Text);
        }

        private void btnCheckID_Click(object sender, RoutedEventArgs e)
        {
            StartCheckServo(txtMaxId.Text);
        }

        protected override bool DetectServo(int id)
        {
            return GoGetVersion(id);
        }

        protected override void UpdateMinId(int id)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Normal,
                (Action)(() => txtId.Text = minId.ToString()));
        }

        #endregion Check Servo

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
                    GoGetVersion(id);
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
                    GoChangeId(id, newId);
                }
                else if (rbUnlock.IsChecked == true)
                {
                    GoUnlock(id);
                }
                else if (rbLock.IsChecked == true)
                {
                    GoLock(id);
                }
                else if (rbGetAngle.IsChecked == true)
                {
                    GetAngle(id);
                }
                else if (rbMovePWM.IsChecked == true)
                {
                    GoMovePWM(id);
                }
                else if (rbMoveAngle.IsChecked == true)
                {
                    GoMoveAngle(id);
                }
                else if (rbSetPWM.IsChecked == true)
                {
                    GoSetPWM(id);
                }
                else if (rbSetAngle.IsChecked == true)
                {
                    GoSetAngle(id);
                }
                else if (rbSetAdj.IsChecked == true)
                {
                    GoSetAdj(id);
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

        // #{id}PVER\r\n
        private bool GoGetVersion(int id)
        {
            string version = GetVersion(id);
            if (version == "") return false;
            AppendLog(String.Format("舵機 {0} 的固件版本為 {1}", id, version));
            return true;
        }

        // @{id}P{newId:000}\r\n
        private void GoChangeId(int id, int newId)
        {
            if (RejectMissingID(id)) return;

            string msg = String.Format("把 舵機 {0} 修改為 舵機 {1} \n", id, newId);
            if (!MessageConfirm(msg)) return;

            if (ChangeId(id, newId))
            {
                AppendLog(String.Format("舵機 {0} 已經版修改成 舵機 {1}, 請使用新的舵機編號操作  \n", id, newId));
                txtId.Text = newId.ToString();
            }
        }


        // #{id}PULK\r\n
        private bool GoUnlock(int id)
        {
            if (RejectMissingID(id)) return false;
            if (UnlockServo(id))
            {
                AppendLog(String.Format("Servo {0} unlocked.", id));
                return true;
            }
            return false;
        }

        // #{id}PULT\r\n
        private bool GoLock(int id)
        {
            if (RejectMissingID(id)) return false;
            if (LockServo(id))
            {
                AppendLog(String.Format("Servo {0} locked.", id));
                return true;
            }
            return false;
        }

        // #{id}PRAD\r\n
        private void GetAngle(int id)
        {
            if (RejectMissingID(id)) return;
            int mode = GetServoMode(id);
            int pos = GetServoPos(id);

            if (!robot.isConnected) return;

            String modeName = GetServoModeName(mode);
            AppendLog("\n舵機模式: " + modeName);

            if (pos == -1)
            {
                appendLog("位置回傳不正常");
                return;
            }
            String msg = String.Format("當前位置值為 {0}", pos);
            if (mode > 4)
            {
                msg += "; 當前模式, 無法換算成角度";
            }
            else
            {
                int angle = ConvertPWMToAngle(mode, pos);
                if (angle == -1)
                {
                    msg += "; 不能換算成角度";
                }
                else
                {
                    msg += String.Format("; 即 {0} 度", angle);
                }

            }
            appendLog(msg);
        }


        // #{id}P{PWM}T{Time}\r\n
        private void GoMovePWM(int id)
        {
            if (((txtMovePWM.Text == null) || (txtMovePWM.Text.Trim() == "")) ||
                ((txtMovePWMTime.Text == null) || (txtMovePWMTime.Text.Trim() == "")))
            {
                AppendLog("\n請先設定要移動的目標位置及移動時間");
                return;
            }
            try
            {
                int pwm = Convert.ToInt32(txtMovePWM.Text.Trim(), 10);
                int time = Convert.ToInt32(txtMovePWMTime.Text.Trim(), 10);

                if ((pwm < 500) || (pwm > 2500))
                {
                    UpdateInfo("請輸入 500 - 2500 之間 的 PWM 值.", UTIL.InfoType.error);
                    return;
                }

                MovePWM(id, pwm, time);
                AppendLog(String.Format("舵機移動到 {0} 的位置", pwm));
            }
            catch (Exception ex)
            {
                AppendLog("\nERR: " + ex.Message);
            }
        }

        // #{id}P{PWM}T{Time}\r\n
        private void GoMoveAngle(int id)
        {
            if (((txtMoveAngle.Text == null) || (txtMoveAngle.Text.Trim() == "")) ||
                ((txtMoveAngleTime.Text == null) || (txtMoveAngleTime.Text.Trim() == "")))
            {
                AppendLog("\n請先設定要移動的目標角度及移動時間");
                return;
            }
            try
            {
                int angle = Convert.ToInt32(txtMoveAngle.Text.Trim(), 10);
                int time = Convert.ToInt32(txtMoveAngleTime.Text.Trim(), 10);

                if ((angle < 0) || (angle > 270))
                {
                    UpdateInfo("請輸入 0 - 270 之間 的 角度.", UTIL.InfoType.error);
                    return;
                }

                movePosReturn mar = MoveAngle(id, angle, time);
                if (mar == movePosReturn.success)
                {
                    AppendLog(String.Format("舵機移動到 {0}度 的位置", angle));
                } else
                {
                    AppendLog(GetErrorMsg(mar));
                }
            }
            catch (Exception ex)
            {
                AppendLog("\nERR: " + ex.Message);
            }
        }

        private void GoSetPWM(int id)
        {
            if (((txtNewPWM.Text == null) || (txtNewPWM.Text.Trim() == "")))
            {
                AppendLog("\n請先設定要設定的位置值");
                return;
            }
            try
            {
                int pwm = Convert.ToInt32(txtNewPWM.Text.Trim(), 10);
                if ((pwm < 500) || (pwm > 2500))
                {
                    UpdateInfo("請輸入 500 - 2500 之間 的 位置值.", UTIL.InfoType.error);
                    return;
                }


                setPosReturn spr = (SetPWM(id, pwm));
                if (spr == setPosReturn.success)
                {
                    AppendLog(String.Format("當前位置設定成 {0} 的位置", pwm));
                }
                else
                {
                    AppendLog(GetErrorMsg(spr));
                }
            }
            catch (Exception ex)
            {
                AppendLog("\nERR: " + ex.Message);
            }
        }

        private void GoSetAngle(int id)
        {
            if (((txtNewAngle.Text == null) || (txtNewAngle.Text.Trim() == "")))
            {
                AppendLog("\n請先設定要設定的角度");
                return;
            }
            try
            {
                int angle = Convert.ToInt32(txtNewAngle.Text.Trim(), 10);
                if ((angle < 0) || (angle > 270))
                {
                    UpdateInfo("請輸入 0 - 270 之間 的 位置值.", UTIL.InfoType.error);
                    return;
                }


                setPosReturn spr = (SetAngle(id, angle));
                if (spr == setPosReturn.success)
                {
                    AppendLog(String.Format("當前位置設定成 {0} 度", angle));
                }
                else
                {
                    AppendLog(GetErrorMsg(spr));
                }
            }
            catch (Exception ex)
            {
                AppendLog("\nERR: " + ex.Message);
            }
        }


        private void GoSetAdj(int id)
        {
            if (((txtNewAdj.Text == null) || (txtNewAdj.Text.Trim() == "")))
            {
                AppendLog("\n請先設定要設定修正量");
                return;
            }
            try
            {
                int adj = Convert.ToInt32(txtNewAdj.Text.Trim(), 10);
                if ((adj < -240) || (adj > 240))
                {
                    UpdateInfo("請輸入 -240 - 240 之間 的 修正量.", UTIL.InfoType.error);
                    return;
                }

                if (setAdj(id, adj))
                {
                    AppendLog(String.Format("舵機 {0} 的修正量設定成 {1}", id, adj));
                }
            }
            catch (Exception ex)
            {
                AppendLog("\nERR: " + ex.Message);
            }
        }


        private void ExecuteFreeInput()
        {
            String cmd = txtCommand.Text.Trim();
            if (cmd.Length == 0) return;
            if (cmd.ElementAt(0) != '#') return;
            SendCommand(cmd, 5);
            // #OK\r\n
            if (robot.Available > 0)
            {
                byte[] buffer = robot.ReadAll();
                string result = Encoding.UTF8.GetString(buffer, 0, buffer.Length - 2);
                AppendLog(result);
            }
        }

        private bool RejectMissingID(int id)
        {
            if (id < 1)
            {
                AppendLog("\n此功能必須指定舵機 ID");
                return true;
            }
            return false;
        }

    }
}

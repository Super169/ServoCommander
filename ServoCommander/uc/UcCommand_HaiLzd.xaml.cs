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

        public override void ExecuteCommand()
        {

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

        private void SendCommand(string cmd, uint expectCnt)
        {
            bool connected = robot.isConnected;
            List<byte> list = new List<byte>();
            list.AddRange(Encoding.UTF8.GetBytes(cmd));
            list.Add(0x0D);
            list.Add(0x0A);
            byte[] data = list.ToArray();

            AppendLog("\n" + (connected ? ">> " : "") + cmd + "\n");
            robot.ClearRxBuffer();
            if (connected)
            {
                robot.SendCommand(data, data.Length, expectCnt);
                string msg = "<< ";
                if (robot.Available > 0)
                {
                    byte[] result = robot.PeekAll();
                    msg += Encoding.UTF8.GetString(result);
                }
                AppendLog(msg);
            }
        }

        // #{id}PVER\r\n
        private bool GetVersion(int id)
        {
            string cmd = String.Format("#{0}PVER", id);
            SendCommand(cmd, 12);
            // #???PVx.xx\r\n
            if (robot.Available > 0)
            {
                byte[] buffer = robot.ReadAll();
                string result = Encoding.UTF8.GetString(buffer, 0, buffer.Length - 2);
                AppendLog(result);
                return true;
            }
            return false;
        }

        // @{id}P{newId:000}\r\n
        private void ChangeId(int id, int newId)
        {
            string cmd = String.Format("#{0}PID{1,3:000}", id, newId);
            SendCommand(cmd, 7);
            // #???PVx.xx\r\n
            if (robot.Available > 0)
            {
                byte[] buffer = robot.ReadAll();
                string result = Encoding.UTF8.GetString(buffer, 0, buffer.Length - 2);
                AppendLog(result);
            }
        }

        // #{id}PRAD\r\n
        private void GetAngle(int id)
        {
            string cmd = String.Format("#{0}PRAD", id);
            SendCommand(cmd, 8);
            // #???PVx.xx\r\n
            if (robot.Available > 0)
            {
                byte[] buffer = robot.ReadAll();
                string result = Encoding.UTF8.GetString(buffer, 0, buffer.Length - 2);
                AppendLog(result);
            }
        }


        // #{id}P{PWM}T{Time}\r\n
        private void GoMove(int id)
        {
            if (((txtMovePWM.Text == null) || (txtMovePWM.Text.Trim() == "")) ||
                ((txtMoveTime.Text == null) || (txtMoveTime.Text.Trim() == "")))
            {
                AppendLog("\n請先設定要移動的目標角度及移動時間");
                return;
            }
            try
            {
                int pwm = Convert.ToInt32(txtMovePWM.Text.Trim(), 10);  
                int time = Convert.ToInt32(txtMoveTime.Text.Trim(), 10);

                if ((pwm < 500) || (pwm > 2500))
                {
                    UpdateInfo("請輸入 500 - 2500 之間 的 PWM 值.", UTIL.InfoType.error);
                    return;
                }

                string cmd = String.Format("#{0}P{1}T{2}", id, pwm, time);
                SendCommand(cmd, 0);
                if (robot.Available > 0)
                {
                    // Should have no return.
                    byte[] buffer = robot.ReadAll();
                    string result = Encoding.UTF8.GetString(buffer, 0, buffer.Length - 2);
                    AppendLog(result);
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

    }
}

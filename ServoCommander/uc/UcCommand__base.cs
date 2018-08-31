using MyUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ServoCommander.uc
{
    public abstract class UcCommand__base : UserControl
    {
        public UcCommand__base()
        {
            InitTimer();
        }


        protected RobotConnection robot;

        public delegate void DelegateAppendLog(string msg = "", bool async = false);
        protected DelegateAppendLog appendLog;
        protected void AppendLog(string msg = "", bool async = false)
        {
            appendLog?.Invoke(msg, async);
        }


        protected UTIL.DelegateUpdateInfo updateInfo;
        protected void UpdateInfo(string msg = "", UTIL.InfoType iType = UTIL.InfoType.message, bool async = false)
        {
            updateInfo?.Invoke(msg, iType, async);
        }

        public void InitObject(UTIL.DelegateUpdateInfo fxUpdateInfo, DelegateAppendLog fxAppendLog, RobotConnection robot)
        {
            this.updateInfo = fxUpdateInfo;
            this.appendLog = fxAppendLog;
            this.robot = robot;
        }

        static protected bool MessageConfirm(String msg)
        {
            MessageBoxResult result = MessageBox.Show(msg, "請確定", MessageBoxButton.YesNo, MessageBoxImage.Question);
            return (result == MessageBoxResult.Yes);
        }

        protected void tb_PreviewCommand(object sender, TextCompositionEventArgs e)
        {
            e.Handled = new Regex("[^0-9A-Fa-f.]+").IsMatch(e.Text);
        }

        protected void tb_PreviewInteger(object sender, TextCompositionEventArgs e)
        {
            UTIL.INPUT.PreviewInteger(ref e);
        }

        protected void tb_PreviewHex(object sender, TextCompositionEventArgs e)
        {
            UTIL.INPUT.PreviewHex(ref e);
        }

        protected void tb_PreviewHexMix(object sender, TextCompositionEventArgs e)
        {
            UTIL.INPUT.PreviewHexMix(ref e);
        }

        protected void tb_PreviewKeyDown_nospace(object sender, System.Windows.Input.KeyEventArgs e)
        {
            UTIL.INPUT.PreviewKeyDown_nospace(ref e);
        }

        static protected bool ValidInteger(TextBox tb, int min, int max, string fieldName)
        {
            int value;
            bool valid = int.TryParse(tb.Text, out value);
            if ((value >= min) && (value <= max)) return true;
            string msg = String.Format("{0} 的数值 '{3}' 不正确\n\n请输入 {1} 至 {2} 之间的数值.", fieldName, min, max, tb.Text);
            MessageBox.Show(msg, "输入错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        static protected bool ValidString(TextBox tb, int min, int max, string fieldName)
        {
            int len = System.Text.Encoding.ASCII.GetByteCount(tb.Text);
            if ((len >= min) && (len <= max)) return true;
            string msg = String.Format("{0} 的长度为 '{3}' 不正确\n\n请输入长度在 {1} 至 {2} 之间的字串.", fieldName, min, max, tb.Text);
            MessageBox.Show(msg, "输入错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        protected void txtCommand_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        public abstract void ExecuteCommand();

        private System.Windows.Forms.Timer checkTimer = new System.Windows.Forms.Timer();
        protected int checkId;
        protected int minId;
        protected int servoCnt;

        protected void InitTimer()
        {
            checkTimer.Interval = 10;
            checkTimer.Tick += new EventHandler(checkTimer_TickHandler);
            checkTimer.Enabled = false;
            checkTimer.Stop();
        }

        protected void StartCheckServo(string sMaxId)
        {
            if (int.TryParse(sMaxId.Trim(), out int maxId))
            {
                CONST.MAX_SERVO = maxId;
            }
            checkId = 1;
            minId = 0;
            servoCnt = 0;
            OnCheckServoStart();
            UpdateInfo("Checking ID, please wait......", UTIL.InfoType.alert);
            checkTimer.Enabled = true;
            checkTimer.Start();
        }

        protected virtual void OnCheckServoStart()
        {
            return;
        }

        protected virtual bool DetectServo(int id)
        {
            return false;
        }

        protected virtual void UpdateMinId(int id)
        {
            return;
        }

        protected virtual void OnCheckServoCompleted()
        {
            return;
        }

        private void checkTimer_TickHandler(object sender, EventArgs e)
        {
            checkTimer.Stop();
            if (DetectServo(checkId))
            {
                AppendLog(String.Format("Servo {0} detected", checkId));
                servoCnt++;
                if (minId == 0)
                {
                    minId = checkId;
                    UpdateMinId(minId);
                }
            }
            checkId++;
            if (checkId > CONST.MAX_SERVO)
            {
                checkTimer.Enabled = false;
                UpdateInfo(String.Format("{0} servo detected.", servoCnt));
                OnCheckServoCompleted();
            }
            else
            {
                checkTimer.Start();
            }
        }
    }


}

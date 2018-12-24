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
    /// Interaction logic for UcCommand_ControlBoard.xaml
    /// </summary>
    public partial class UcCommand_ControlBoard : UcCommand__base
    {
        public UcCommand_ControlBoard()
        {
            InitializeComponent();
        }

        
        public override void InitObject(UTIL.DelegateUpdateInfo fxUpdateInfo, DelegateAppendLog fxAppendLog, RobotConnection robot)
        {
            base.InitObject(fxUpdateInfo, fxAppendLog, robot);
            SetVersion();
        }

        public override void ConnectionChanged()
        {
            SetVersion();
        }

        private void SetVersion()
        {
            if (robot.isConnected)
            {
                string version = GetVersion();
                if (version == "")
                {
                    lblVersion.Content = LocUtil.FindResource("cb.msgControllerNotFound");
                    lblVersion.Foreground = new SolidColorBrush(Colors.Red);
                }
                else
                {
                    lblVersion.Content = version;
                    lblVersion.Foreground = new SolidColorBrush(Colors.Blue);
                }
            }
            else
            {
                lblVersion.Content = LocUtil.FindResource("cb.msgPleaseConnectController");
                lblVersion.Foreground = new SolidColorBrush(Colors.LightGray);
            }

        }

        public override void ExecuteCommand()
        {
            if (rbServoMode.IsChecked == true)
            {
                CheckServoMode();
            } else if (rbCommandMode.IsChecked == true)
            {
                CheckCommandMode();
            } else if (rbEnterUSBTTL.IsChecked == true)
            {
                EnterUSBTTL();
            } else if (rbExitUSBTTL.IsChecked == true)
            {
                ExitUSBTTL();
            }
        }

        private bool SendCBCommand(byte[] cmd, int expectCnt)
        {
            if (robot == null) return false;
            bool connected = robot.isConnected;
            if (cmd.Length < 6) return false;
            cmd[0] = 0xA9;
            cmd[1] = 0x9A;
            cmd[2] = (byte) (cmd.Length - 4);
            cmd[cmd.Length - 1] = 0xED;
            cmd[cmd.Length - 2] = UTIL.CalCBCheckSum(cmd);

            AppendLog("\n" + (connected ? ">> " : "") + UTIL.GetByteString(cmd) + "\n");
            robot.ClearRxBuffer();
            if (connected)
            {
                robot.SendCommand(cmd, cmd.Length, expectCnt);
                string msg = "<< ";
                if (robot.Available > 0)
                {
                    byte[] result = robot.PeekAll();
                    msg += UTIL.GetByteString(result);
                }
                AppendLog(msg);
                return true;
            }
            return false;
        }

        // A9 9A 02 10 12 ED
        private void CheckServoMode()
        {
            byte[] cmd = { 0xA9, 0x9A, 0x02, 0x10, 0x12, 0xED };
            SendCBCommand(cmd, 7);
            if (robot.Available == 7)
            {
                byte[] result = robot.ReadAll();
                string servo;
                switch (result[4])
                {
                    case 1:
                        servo = LocUtil.FindResource("cb.msgServoUBTech");
                        break;
                    case 2:
                        servo = LocUtil.FindResource("cb.msgServoHaiLzd");
                        break;
                    case 3:
                        servo = LocUtil.FindResource("cb.msgServoFeeTech");
                        break;
                    default:
                        servo = LocUtil.FindResource("cb.msgServoError");
                        break;
                }
                AppendLog(String.Format(LocUtil.FindResource("cb.msgServoProtocol"), servo));
            }
            robot.ClearRxBuffer();
        }

        // A9 9A 02 0A 0C ED
        private void CheckCommandMode()
        {
            byte[] cmd = { 0xA9, 0x9A, 0x02, 0x0A, 0x0C, 0xED };
            SendCBCommand(cmd, 12);
            if (robot.Available == 12)
            {
                byte[] result = robot.ReadAll();
                String msg = "";
                if (result[4] != 0) msg += " V1";
                if (result[5] != 0) msg += " V2";
                if (result[6] != 0) msg += " BT";
                if (result[7] != 0) msg += " CB";
                if (result[8] != 0) msg += " SV";
                if (result[9] != 0) msg += " HaiLzd";
                if (msg == "")
                {
                    msg = LocUtil.FindResource("cb.msgNoCommandSupported");
                } else
                {
                    msg = LocUtil.FindResource("cb.msgSupportedCommand") + msg;
                }

                AppendLog(msg);
            }
            robot.ClearRxBuffer();
        }


        private string GetVersion()
        {
            byte[] cmd = { 0xA9, 0x9A, 0x02, 0xFF, 0x01, 0xED };
            SendCBCommand(cmd, 10);
            String version = "";
            if (robot.Available == 10)
            {
                byte[] result = robot.ReadAll();
                version = string.Format("{0}.{1}.{2}", result[4], result[5], result[6]);
                if (result[7] > 0) version += string.Format("  [Fix {0}]", result[7]);
            }
            else
            {
                version = "";
            }
            return version;
        }

        private void EnterUSBTTL()
        {
            if (robot.currMode == RobotConnection.connMode.Network)
            {
                if (!MessageConfirm(LocUtil.FindResource("cb.msgConfirmEnterUSBTTL"))) return;
            }

            byte[] cmd = { 0xA9, 0x9A, 0x04, 0x07, 0x00, 0x00, 0x0B, 0xED };
            SendCBCommand(cmd, 7);
            if (robot.isConnected)
            {
                string action = LocUtil.FindResource("cb.msgEnterUSBTTL");
                if (robot.Available == 7)
                {
                    byte[] result = robot.ReadAll();
                    if (result[4] == 0)
                    {
                        AppendLog(LocUtil.FindResource("msgSuccess") + action);
                    }
                    else
                    {
                        AppendLog(action + LocUtil.FindResource("msgFail"));
                    }
                }
                else
                {
                    AppendLog(string.Format(LocUtil.FindResource("cb.msgUSBTTLNoReply"), action));
                }
            }
        }

        private void ExitUSBTTL()
        {

            if (robot.currMode == RobotConnection.connMode.Network)
            {
                if (!MessageConfirm(LocUtil.FindResource("cb.msgConfirmQuitUSBTTL"))) return;
            }
           
            byte[] cmd = { 0xA9, 0x9A, 0x01, 0x06, 0x09 };
            AppendLog("\n" + (robot.isConnected ? ">> " : "") + UTIL.GetByteString(cmd) + "\n");
            robot.ClearRxBuffer();
            if (robot.isConnected)
            {
                robot.SendCommand(cmd, cmd.Length, 0);
                AppendLog(LocUtil.FindResource("cb.msgQuitUSBTTLSent"));
            }
        }

    }
}

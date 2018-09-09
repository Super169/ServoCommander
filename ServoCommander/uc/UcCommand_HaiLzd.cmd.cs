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

        private bool IsOK()
        {
            string result;
            return IsOK(out result);
        }

        private bool IsOK(out string result)
        {
            result = "";
            if (robot.Available > 0)
            {
                byte[] buffer = robot.ReadAll();
                result = Encoding.UTF8.GetString(buffer, 0, buffer.Length - 2);
                if (result.Equals("#OK")) return true;
            }
            return false;
        }

        private bool IsCommandOK(string cmd)
        {
            string result;
            return IsCommandOK(cmd, out result);
        }


        private bool IsCommandOK(string cmd, out string result)
        {
            SendCommand(cmd, 5);
            if (IsOK(out result))
            {
                return true;
            }
            return false;
        }


        // #{id}PVER\r\n
        private string GetVersion(int id)
        {
            string cmd = String.Format("#{0}PVER", id);
            SendCommand(cmd, 12);
            // #???PVx.xx\r\n
            if (robot.Available > 0)
            {
                byte[] buffer = robot.ReadAll();
                string result = Encoding.UTF8.GetString(buffer, 0, buffer.Length - 2);
                cmd = String.Format("#{0,3:000}PV", id);
                if (result.StartsWith(cmd))
                {
                    return result.Replace(cmd, "");
                }
            }
            return "";
        }

        //#{id}PID{newId}\r\n
        private bool ChangeId(int id, int newId)
        {
            string cmd = String.Format("#{0}PID{1,3:000}", id, newId);
            SendCommand(cmd, 7);
            // #???P\r\n
            if (robot.Available > 0)
            {
                byte[] buffer = robot.ReadAll();
                string result = Encoding.UTF8.GetString(buffer, 0, buffer.Length - 2);
                cmd = String.Format("#{0,3:000}P", newId);
                if (result.StartsWith(cmd)) return true;
            }
            return false;
        }

        // #{id}PULK\r\n
        private bool UnlockServo(int id)
        {
            string cmd = String.Format("#{0}PULK", id);
            return IsCommandOK(cmd);
        }

        // #{id}PULT\r\n
        private bool LockServo(int id)
        {
            string cmd = String.Format("#{0}PULR", id);
            return IsCommandOK(cmd);
        }

        private int GetServoMode(int id)
        {
            string cmd;
            cmd = String.Format("#{0,3:000}PMOD", id);
            SendCommand(cmd, 11);
            // #???PMOD?\r\n
            int mode = -1;
            if (robot.Available == 11)
            {
                byte[] buffer = robot.ReadAll();
                string result = Encoding.UTF8.GetString(buffer, 0, buffer.Length - 2);
                if (result.StartsWith(cmd))
                {
                    string sMod = result.Replace(cmd, "");
                    if (!int.TryParse(sMod, out mode)) mode = -1;
                }
            }
            return mode;
        }

        private string GetServoModeName(int mode)
        {
            String modeName = "";
            switch (mode)
            {
                case 1:
                    modeName = "逆向 270度";
                    break;
                case 2:
                    modeName = "順向 270度";
                    break;
                case 3:
                    modeName = "逆向 180度";
                    break;
                case 4:
                    modeName = "順向 180度";
                    break;
                case 5:
                    modeName = "逆向 360度 (定圈)";
                    break;
                case 6:
                    modeName = "順向 360度 (定圈)";
                    break;
                case 7:
                    modeName = "逆向 360度 (定時)";
                    break;
                case 8:
                    modeName = "順向 360度 (定時)";
                    break;
                default:
                    modeName = "不明模式:";
                    break;
            }
            return modeName;
        }

        private int GetServoPos(int id)
        {
            string cmd = String.Format("#{0,3:000}PRAD", id);
            SendCommand(cmd, 8);
            // #???P????\r\n
            int pos = -1;
            if (robot.Available >= 8)
            {
                byte[] buffer = robot.ReadAll();
                string result = Encoding.UTF8.GetString(buffer, 0, buffer.Length - 2);
                cmd = String.Format("#{0,3:000}P", id);
                if (result.StartsWith(cmd))
                {
                    string sPos = result.Replace(cmd, "");
                    if (!int.TryParse(sPos, out pos)) pos = -1;
                }
            }
            return pos;
        }

        private int ConvertPWMToAngle(int mode, int pwm)
        {
            if ((mode < 1) || (mode > 4) || (pwm < 500) || (pwm > 2500)) return -1;
            int angle = -1;
            switch (mode)
            {
                case 1:
                case 2:
                    angle = (pwm - 500) * 270 / 2000;
                    break;
                case 3:
                case 4:
                    angle = (pwm - 500) * 180 / 2000;
                    break;
            }
            return angle;
        }

        private int ConvertAngleToPWM(int mode, int angle)
        {
            if ((mode < 1) || (mode > 4) || (angle < 0) || (angle > 270)) return -1;
            if ((mode > 2) && (angle > 180)) return -1;

            int pwm = -1;
            switch (mode)
            {
                case 1:
                case 2:
                    pwm = 500 + 2000 * angle / 270;
                    break;
                case 3:
                case 4:
                    pwm = 500 + 2000 * angle / 180;
                    break;
                default:
                    return -1;
            }
            return pwm;
        }



        private movePosReturn MovePWM(int id, int pwm, int time)
        {
            if ((pwm < 0) || (pwm > 2500)) return movePosReturn.invalidPWM;
            string cmd = String.Format("#{0}P{1}T{2}", id, pwm, time);
            SendCommand(cmd, 0);
            if (robot.Available > 0)
            {
                // Just clear the buffer, there should have no return from move command
                byte[] buffer = robot.ReadAll();
            }
            return movePosReturn.success;
        }

        private enum movePosReturn
        {
            success, unknownMode, unsupportedMode, invalidAngle, invalidPWM
        }

        private movePosReturn MoveAngle(int id, int angle, int time)
        {
            int mode = GetServoMode(id);
            if (mode == -1) return movePosReturn.unknownMode;
            if ((mode < 1) || (mode > 4)) return movePosReturn.unsupportedMode;

            int pwm = ConvertAngleToPWM(mode, angle);
            if (pwm == -1) return movePosReturn.invalidAngle;

            return MovePWM(id, pwm, time);
        }

        private enum setPosReturn
        {
            success, invalidPos, invalidAngle, getModeFail, invalidMode, resetFail, getPosFail, outOfRange, fail
        }

        private string GetErrorMsg(movePosReturn code)
        {
            string msg = "";
            switch (code)
            {
                case movePosReturn.success:
                    break;
                case movePosReturn.invalidPWM:
                    msg = "位置不正確";
                    break;
                case movePosReturn.invalidAngle:
                    msg = "輸入角度不正確";
                    break;
                case movePosReturn.unknownMode:
                    msg = "舵機模式不明";
                    break;
                case movePosReturn.unsupportedMode:
                    msg = "舵機模式不支援";
                    break;
                default:
                    msg = "不明錯誤: " + code.ToString();
                    break;
            }
            return msg;
        }

        private String GetErrorMsg(setPosReturn code)
        {
            string msg = "";
            switch (code)
            {
                case setPosReturn.success:
                    break;
                case setPosReturn.invalidPos:
                    msg = "位置值不正確";
                    break;
                case setPosReturn.invalidAngle:
                    msg = "角度不正確";
                    break;
                case setPosReturn.getModeFail:
                    msg = "讀取當前模式失敗";
                    break;
                case setPosReturn.invalidMode:
                    msg = "當前模式不支援";
                    break;
                case setPosReturn.resetFail:
                    msg = "重設修正失敗";
                    break;
                case setPosReturn.getPosFail:
                    msg = "讀取當前位置失敗";
                    break;
                case setPosReturn.outOfRange:
                    msg = "修正置超出可設定範圍";
                    break;
                case setPosReturn.fail:
                    msg = "設置修正失敗";
                    break;
                default:
                    msg = "不明錯誤: " + code.ToString();
                    break;
            }
            return msg;
        }

        private bool setAdj(int id, int adj)
        {
            if ((adj < -240) || (adj > 240)) return false;
            string cmd = String.Format("#{0,3:000}PSCK", id) + (adj >= 0 ? "+" : "") + adj.ToString();
            return (IsCommandOK(cmd));
        }

        private setPosReturn SetPWM(int id, int pwm, int mode = -1)
        {
            if ((pwm < 500) || (pwm > 2500)) return setPosReturn.invalidPos;

            if (mode == -1)
            {
                mode = GetServoMode(id);
            }
            if (mode == -1) return setPosReturn.getModeFail;
            if ((mode < 1) || (mode > 4)) return setPosReturn.invalidMode;

            // Reset adjustment
            string cmd = String.Format("#{0,3:000}PSCK+0", id);
            if (!IsCommandOK(cmd)) return setPosReturn.resetFail;

            int currPwm = GetServoPos(id);
            if (currPwm == -1) return setPosReturn.getPosFail;

            int adj = pwm - currPwm;
            if (Math.Abs(adj) > 240) return setPosReturn.outOfRange;

            if (adj == 0) return setPosReturn.success;

            if (setAdj(id, adj)) return setPosReturn.success;
            return setPosReturn.fail;
        }

        private setPosReturn SetAngle(int id, int angle, int mode = -1)
        {
            if ((angle < 0) || (angle > 270)) return setPosReturn.invalidAngle;

            if (mode == -1)
            {
                mode = GetServoMode(id);
            }
            if (mode == -1) return setPosReturn.getModeFail;
            if ((mode < 1) || (mode > 4)) return setPosReturn.invalidMode;

            int pwm = ConvertAngleToPWM(mode, angle);
            if (pwm == -1) return setPosReturn.invalidAngle;

            return SetPWM(id, pwm, mode);
        }

    }
}

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

        private void SendCommand(byte[] cmd, uint expectCnt, bool writeLog = true)
        {
            bool connected = robot.isConnected;
            cmd[8] = UTIL.CalUBTCheckSum(cmd);
            cmd[9] = 0xED;

            if (writeLog) AppendLog("\n" + (connected ? ">> " : "") + UTIL.GetByteString(cmd) + "\n");
            robot.ClearRxBuffer();
            if (connected)
            {
                robot.SendCommand(cmd, 10, expectCnt);
                if (writeLog)
                {
                    string msg = "<< ";
                    if (robot.Available > 0)
                    {
                        byte[] result = robot.PeekAll();
                        msg += UTIL.GetByteString(result);
                    }
                    AppendLog(msg);
                }
            }
        }


        private bool UBTGetAdjAngle(int id, out UInt16 adjValue, out byte[] buffer)
        {
            adjValue = 0;
            buffer = null;
            byte[] cmd = { 0xFA, 0xAF, (byte)id, 0xD4, 0, 0, 0, 0, 0, 0xED };
            SendCommand(cmd, 10);
            if ((id > 0) && (robot.Available == 10))
            {
                buffer = robot.ReadAll();
                adjValue = (UInt16) ((buffer[6] << 8) | buffer[7]);
                return true;
            }
            return false;
        }

        private bool UBTGetAngle(int id, out byte angle, out byte[] buffer)
        {
            angle = 0;
            buffer = null;
            byte[] cmd = { 0xFA, 0xAF, (byte)id, 2, 0, 0, 0, 0, 0, 0xED };
            SendCommand(cmd, 10);
            if ((id > 0) && (robot.Available == 10))
            {
                buffer = robot.ReadAll();
                angle = buffer[7];
                return true;
            }
            return false;
        }


        private bool UBTSetAdjAngle(int id, int adjValue)
        {
            if (adjValue < 0) adjValue += 65536;

            byte adjHigh = (byte)(adjValue / 256);
            byte adjLow = (byte)(adjValue % 256);

            byte[] cmd = { 0xFA, 0xAF, (byte)id, 0xD2, 0, 0, adjHigh, adjLow, 0, 0xED };
            SendCommand(cmd, 10);
            if ((id > 0) && (robot.Available == 10))
            {
                byte[] buffer = robot.ReadAll();
                if (buffer[3] == 0xAA) return true;
            }
            return false;
        }

        // -1 : cannot detect
        //  0 : success
        //  1 : failed
        private int UBTGoMove(int id, byte angle, int timeMs)
        {
            byte[] cmd = { 0xFA, 0xAF, (byte)id, 1, 0, 0, 0, 0, 0, 0xED };
            byte time = (byte)(timeMs / 20);
            cmd[4] = angle;
            cmd[5] = cmd[7] = time;
            SendCommand(cmd, 1);
            if ((id != 0) && (robot.Available == 1))
            {
                byte[] buffer = robot.ReadAll();
                if (buffer[0] == (0xAA + id))
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            }
            return -1;
        }


    }
}

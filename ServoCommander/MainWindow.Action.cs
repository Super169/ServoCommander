using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace ServoCommander
{
    public partial class MainWindow : Window
    {
        private void GetVersion(int id)
        {
            byte[] cmd = { 0xFC, 0xCF, (byte)id, 0x01, 0, 0, 0, 0, 0, 0xED };
            SendCmd(cmd, 10);
            if (receiveBuffer.Count == 10)
            {
                string result = String.Format("版本號為: {0:X2} {1:X2} {2:X2} {3:X2}\n",
                                              receiveBuffer[4], receiveBuffer[5], receiveBuffer[6], receiveBuffer[7]);
                AppendLog(result);
            }

        }

        private void ChangeId(int id, int newId)
        {
            string msg = String.Format("把 舵機編號 {0} 修改為 舵機編號 {1} \n", id, newId);
            if (!MessageConfirm(msg)) return;
            byte[] cmd = { 0xFA, 0xAF, (byte)id, 0xCD, 0, (byte) newId, 0, 0, 0, 0xED };
            SendCmd(cmd, 10);
            if (receiveBuffer.Count == 10)
            {
                if (receiveBuffer[3] == 0xAA)
                {
                    string result = String.Format("舵機編號 {0} 已成功修改為 舵機編號 {1} \n", id, newId);
                    AppendLog(result);
                    txtId.Text = newId.ToString();
                    UpdateInfo(result, Util.InfoType.alert);
                }
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
                byte angle = Util.GetInputByte(txtMoveAngle.Text);
                byte time = Util.GetInputByte(txtMoveTime.Text);
                cmd[4] = angle;
                cmd[5] = cmd[7] = time;
                SendCmd(cmd, 1);
                if (receiveBuffer.Count == 1)
                {
                    if (receiveBuffer[0] == (0xAA + id))
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

        private void GetAngle(int id)
        {
            byte[] cmd = { 0xFA, 0xAF, (byte)id, 2, 0, 0, 0, 0, 0, 0xED };
            SendCmd(cmd, 10);
            if (receiveBuffer.Count == 10)
            {

                string result = String.Format("舵機角度:  目前為: {0:X2} {1:X2} ({1}度), 實際為: {2:X2} {3:X2} ({3}度)\n",
                                              receiveBuffer[4], receiveBuffer[5], receiveBuffer[6], receiveBuffer[7]);
                string hexAngle = String.Format("{0:X2}", receiveBuffer[7]);
                txtAdjPreview.Text = hexAngle;
                AppendLog(result);
            }

        }

        private void GetAdjAngle(int id)
        {
            byte[] cmd = { 0xFA, 0xAF, (byte)id, 0xD4, 0, 0, 0, 0, 0, 0xED };
            SendCmd(cmd, 10);
            if (receiveBuffer.Count == 10)
            {

                int adjValue = receiveBuffer[6] * 256 + receiveBuffer[7];
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
                                              receiveBuffer[4], receiveBuffer[5], receiveBuffer[6], receiveBuffer[7], adjMsg);
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
            SendCmd(cmd, 10);
            if (receiveBuffer.Count == 10)
            {
                if (receiveBuffer[3] != 0xAA)
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

            Thread.Sleep(500);
            txtMoveAngle.Text = txtAdjPreview.Text;
            txtMoveTime.Text = "28";
            GoMove(id);
        }


        private void ExecuteFreeInput()
        {
            byte[] command;
            if (GetCommand(out command))
            {
                SendCmd(command, 10);  // assume 10 byte returned
            }
        }

        private void btnClearLog_Click(object sender, RoutedEventArgs e)
        {
            UpdateMaxId();
            txtLog.Text = "";
        }

    }
}

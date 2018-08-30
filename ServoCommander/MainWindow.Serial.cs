using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace ServoCommander
{
    public partial class MainWindow : Window
    {
        private const long DEFAULT_COMMAND_TIMEOUT = 10;    // Default 10ms, according to spec, servo return at 400us, add some overhead for ESP8266 handler
        private const long MAX_WAIT_MS = 1000;             // Default 1s, for any command, it should not wait more than 1s

        private SerialPort serialPort = new SerialPort();
        private List<byte> receiveBuffer = new List<byte>();
        // maximum wait for a complete command

        private void InitializeSerialPort()
        {
            serialPort.DataReceived += SerialPort_DataReceived;
        }

        private void ClearSerialBuffer()
        {
            receiveBuffer.Clear();
        }

        private void FindPorts(string defaultPort)
        {
            portsComboBox.ItemsSource = SerialPort.GetPortNames();
            if (portsComboBox.Items.Count > 0)
            {
                if (defaultPort == null)
                {
                    portsComboBox.SelectedIndex = 0;
                }
                else
                {
                    portsComboBox.SelectedIndex = portsComboBox.Items.IndexOf(defaultPort);
                    if (portsComboBox.SelectedIndex < 0) portsComboBox.SelectedIndex = 0;
                }
                portsComboBox.IsEnabled = true;
            }
            else
            {
                portsComboBox.IsEnabled = false;
            }
        }

        private bool SerialConnect(String portName)
        {
            bool flag = false;

            UpdateInfo();

            if (serialPort.IsOpen)
            {
                UpdateInfo(string.Format("Port {0} already connected - 115200, N, 8, 1", serialPort.PortName), UTIL.InfoType.alert);
                return true;
            }

            serialPort.PortName = portName;
            serialPort.BaudRate = 115200;
            serialPort.Parity = Parity.None;
            serialPort.DataBits = 8;
            serialPort.StopBits = StopBits.One;

            try
            {
                serialPort.Open();
                if (serialPort.IsOpen)
                {
                    serialPort.DiscardInBuffer();
                    serialPort.DiscardOutBuffer();
                    UpdateInfo(string.Format("Port {0} connected - 115200, N, 8, 1", serialPort.PortName));
                    UTIL.WriteRegistry(UTIL.KEY.LAST_CONNECTION, portName);
                    flag = true;
                }
                else
                {
                    UpdateInfo(string.Format("Fail connecting to Port {0} - 115200, N, 8, 1", serialPort.PortName), UTIL.InfoType.error);
                }
            }
            catch (Exception ex)
            {
                UpdateInfo("Error: " + ex.Message, UTIL.InfoType.error);
            }
            return flag;
        }

        private bool SerialDisconnect()
        {

            if (!serialPort.IsOpen)
            {
                UpdateInfo(string.Format("Port {0} not yet connected", serialPort.PortName), UTIL.InfoType.alert);
                return true;
            }

            UpdateInfo();

            serialPort.Close();

            if (serialPort.IsOpen)
            {
                UpdateInfo(string.Format("Fail to disconnect Port {0}", serialPort.PortName), UTIL.InfoType.error);
                return false;
            }

            UpdateInfo(string.Format("Port {0} disconnected", serialPort.PortName));
            return true;
        }

        private void SerialPort_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            System.IO.Ports.SerialPort sp = sender as System.IO.Ports.SerialPort;

            if (sp == null) return;

            int bytesToRead = sp.BytesToRead;
            byte[] tempBuffer = new byte[bytesToRead];

            sp.Read(tempBuffer, 0, bytesToRead);

            //TODO: May need to lock receiveBuffer first
            receiveBuffer.AddRange(tempBuffer);
        }

        private long WaitForSerialData(long minBytes, long maxMs)
        {
            // Wait for at least 1 bytes
            if (minBytes < 1) minBytes = 1;
            // at least wait for 1 ms, but not more than 10s
            if (maxMs < 1) maxMs = 1;
            if (maxMs > MAX_WAIT_MS) maxMs = MAX_WAIT_MS;

            long startTicks = DateTime.Now.Ticks;
            long endTicks = DateTime.Now.Ticks + maxMs * TimeSpan.TicksPerMillisecond;
            while (DateTime.Now.Ticks < endTicks)
            {
                if (receiveBuffer.Count >= minBytes) break;
                System.Threading.Thread.Sleep(1);
            }
            return receiveBuffer.Count;
        }


        private bool SerialPortWrite(string textData)
        {
            bool flag = false;

            if (serialPort == null) return false;
            if (!serialPort.IsOpen) return false;

            try
            {
                serialPort.Write(textData);
                flag = true;
            }
            catch (Exception ex)
            {
                UpdateInfo("Error: " + ex.Message, UTIL.InfoType.error);
            }

            return flag;
        }

        public bool SendCommand(char ch, long expBytes, long maxMs = DEFAULT_COMMAND_TIMEOUT)
        {
            return SendCommand(ch.ToString(), expBytes, maxMs);
        }

        public bool SendCommand(string command, long expBytes, long maxMs = DEFAULT_COMMAND_TIMEOUT)
        {
            if (!serialPort.IsOpen) return false;
            byte[] data = Encoding.Default.GetBytes(command);
            return SendCommand(data, data.Length, expBytes, maxMs);
        }

        public bool SendCommand(byte[] command, int count, long expBytes, long maxMs = DEFAULT_COMMAND_TIMEOUT)
        {
            if (!serialPort.IsOpen) return false;
            ClearSerialBuffer();
            serialPort.Write(command, 0, count);
            WaitForSerialData(expBytes, maxMs);
            return (receiveBuffer.Count == expBytes);
        }

    }

}

using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using MyUtil;

namespace SimpleCOM
{
    public class SerialConnection
    {
        private const long DEFAULT_COMMAND_TIMEOUT = 10;    // Default 10ms, according to spec, servo return at 400us, add some overhead for ESP8266 handler
        private const long MAX_WAIT_MS = 1000;             // Default 1s, for any command, it should not wait more than 1s

        private SerialPort serialPort = new SerialPort();
        public List<byte> rxBuffer = new List<byte>();

        protected UTIL.DelegateUpdateInfo updateInfo;
        protected void UpdateInfo(string msg = "", UTIL.InfoType iType = UTIL.InfoType.message, bool async = false)
        {
            updateInfo?.Invoke(msg, iType, async);
        }

        public SerialConnection()
        {
            serialPort.DataReceived += SerialPort_DataReceived;
        }

        public void InitialObject(UTIL.DelegateUpdateInfo fxUpdateInfo)
        {
            this.updateInfo = fxUpdateInfo;
        }

        public bool isConnected
        {
            get
            {
                return serialPort.IsOpen;
            }
        }

        public bool Connect(string portName, int baudRate = 115200, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One)
        {
            bool flag = false;

            if (isConnected)
            {
                UpdateInfo(string.Format("Port {0} already connected - 115200, N, 8, 1", serialPort.PortName), UTIL.InfoType.alert);
                return true;
            }

            serialPort.PortName = portName;
            serialPort.BaudRate = baudRate;
            serialPort.Parity = parity;
            serialPort.DataBits = dataBits;
            serialPort.StopBits = stopBits;

            try
            {
                serialPort.Open();
                if (isConnected)
                {
                    serialPort.DiscardInBuffer();
                    serialPort.DiscardOutBuffer();
                    UpdateInfo(string.Format("Port {0} connected - 115200, N, 8, 1", serialPort.PortName));
                    UTIL.WriteRegistry(UTIL.KEY.LAST_CONNECTION_SERIAL, portName);
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

        public bool Disconnect()
        {
            if (!isConnected)
            {
                UpdateInfo(string.Format("Port {0} not yet connected", serialPort.PortName), UTIL.InfoType.alert);
                return true;
            }

            UpdateInfo();

            serialPort.Close();

            // Still connecting, seems some error here
            if (isConnected)
            {
                UpdateInfo(string.Format("Fail to disconnect Port {0}", serialPort.PortName), UTIL.InfoType.error);
                return false;
            }

            UpdateInfo(string.Format("Port {0} disconnected", serialPort.PortName));
            return true;
        }

        // Force close without any message
        public void Close() {
            if (isConnected) serialPort.Close();
        }


        public void ClearSerialBuffer()
        {
            rxBuffer.Clear();
        }

        private void SerialPort_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            System.IO.Ports.SerialPort sp = sender as System.IO.Ports.SerialPort;

            if (sp == null) return;

            int bytesToRead = sp.BytesToRead;
            byte[] tempBuffer = new byte[bytesToRead];

            sp.Read(tempBuffer, 0, bytesToRead);

            //TODO: May need to lock receiveBuffer first
            rxBuffer.AddRange(tempBuffer);
        }


        public string LastConnection
        {
            get
            {
                return (string)UTIL.ReadRegistry(UTIL.KEY.LAST_CONNECTION_SERIAL);
            }
        }

        public string[] GetPortNames()
        {
            return SerialPort.GetPortNames();
        }

        public void SetAvailablePorts(ComboBox comboPorts, string defaultPort = null)
        {
            if (defaultPort == null) defaultPort = LastConnection;
            comboPorts.ItemsSource = SerialPort.GetPortNames();
            if (comboPorts.Items.Count > 0)
            {
                defaultPort = defaultPort.Trim();
                if ((defaultPort == null) || (defaultPort == ""))
                {
                    comboPorts.SelectedIndex = 0;
                }
                else
                {
                    comboPorts.SelectedIndex = comboPorts.Items.IndexOf(defaultPort);
                    if (comboPorts.SelectedIndex < 0) comboPorts.SelectedIndex = 0;
                }
                comboPorts.IsEnabled = true;
            }
            else
            {
                comboPorts.IsEnabled = false;
            }
        }

        public long WaitForSerialData(long minBytes, long maxMs)
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
                if (rxBuffer.Count >= minBytes) break;
                System.Threading.Thread.Sleep(1);
            }
            return rxBuffer.Count;
        }

        #region UBTech specific command, should be moved to other library later

        public bool SendUBTCommand(char ch, long expBytes, long maxMs = DEFAULT_COMMAND_TIMEOUT)
        {
            return SendUBTCommand(ch.ToString(), expBytes, maxMs);
        }

        public bool SendUBTCommand(string command, long expBytes, long maxMs = DEFAULT_COMMAND_TIMEOUT)
        {
            if (!serialPort.IsOpen) return false;
            byte[] data = Encoding.Default.GetBytes(command);
            return SendUBTCommand(data, data.Length, expBytes, maxMs);
        }

        public bool SendUBTCommand(byte[] command, int count, long expBytes, long maxMs = DEFAULT_COMMAND_TIMEOUT)
        {
            if (!serialPort.IsOpen) return false;
            ClearSerialBuffer();
            serialPort.Write(command, 0, count);
            WaitForSerialData(expBytes, maxMs);
            return (rxBuffer.Count == expBytes);
        }

        #endregion

    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MyUtil
{
    public class Robot_Net : Robot_base
    {
        private NetClient client = new NetClient(NetConnection.ClientMode.bufferMode);

        public Robot_Net()
        {
            client.OnDataReceived += NetClient_OnDataReceived;
            // For network connection, especially WiFi, maximum 5s is still reasonable
            DEFAULT_COMMAND_TIMEOUT = 5000;
            MAX_WAIT_MS = 5000;
        }

        private void NetClient_OnDataReceived(object sender, NetConnection connection, byte[] e)
        {
            AddRxData(e);
            CallOnDataReceived(this, e);
        }

        public override bool isConnected
        {
            get { return client.Connected;  }
        }

        public override void Close()
        {
            client.Disconnect();
            connTarget = null;
        }

        public bool Connect(IPAddress address, int port)
        {
            RobotConnParm parm = new RobotConnParm()
            {
                mode = RobotConnMode.bufferMode,
                address = address,
                port = port
            };
            return Connect(parm);
        }

        public bool Connect(String hostName, int port)
        {
            RobotConnParm parm = new RobotConnParm()
            {
                mode = RobotConnMode.bufferMode,
                hostName = hostName,
                port = port
            };
            return Connect(parm);
        }

        public override bool Connect(RobotConnParm parm)
        {
            if (isConnected)
            {
                UpdateInfo(string.Format("Already connected to {0}", connTarget), UTIL.InfoType.alert);
                return false;
            }
            if (parm.address == null)
            {
                if ((parm.hostName == null) || (parm.hostName == ""))
                {
                    parm.hostName = "localhost";
                }
                connTarget = string.Format("{0}:{1}", parm.hostName, parm.port);
                client.Connect(parm.hostName, parm.port);
            } else
            {
                connTarget = string.Format("{0}:{1}", parm.address, parm.port);
                client.Connect(parm.address, parm.port);
            }
            if (!client.Connected) connTarget = "{" + connTarget + "} - failed";
            return client.Connected;
        }

        public override bool Disconnect()
        {
            client.Disconnect();
            if (client.Connected) return false;
            connTarget = null;
            return true;
        }

        public override void Send(byte[] data, int offset, int count)
        {
            byte[] sendData = new byte[count];
            Array.Copy(data, offset, sendData, 0, count);
            client.ClearRxBuffer();
            client.Send(sendData);
        }

        // async mode not work, use buffer mode
        public override long WaitForData(long minBytes, long maxMs)
        {
            // Wait for at least 1 bytes
            if (minBytes < 1) minBytes = 1;
            // at least wait for 1 ms, but not more than 10s
            if (maxMs < 1) maxMs = 1;
            if (maxMs > MAX_WAIT_MS) maxMs = MAX_WAIT_MS;

            long startTicks = DateTime.Now.Ticks;
            long endTicks = DateTime.Now.Ticks + maxMs * TimeSpan.TicksPerMillisecond;
            List<byte> tempBuffer = new List<byte>();
            while (DateTime.Now.Ticks < endTicks)
            {
                if (client.Available > 0)
                {
                    byte[] data = client.ReadAll();
                    tempBuffer.AddRange(data);
                }
                if (tempBuffer.Count >= minBytes) break;
                // Special handling for UBT return with missing 1st byte
                if (tempBuffer.Count == 9)
                {
                    byte head = (byte)tempBuffer.ElementAt(0);
                    if (((head == 0xAF) || (head == 0xCF)) && 
                        (tempBuffer.ElementAt(8) == 0xED))
                    {
                        tempBuffer.Insert(0, (byte) (head == 0xAF ? 0xFA : 0xFC));
                        break;
                    }
                }
                System.Threading.Thread.Sleep(1);
            }
            AddRxData(tempBuffer.ToArray());
            // return rxBuffer.Count;
            return Available;
        }
    }
}

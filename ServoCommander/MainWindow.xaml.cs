﻿using MyUtil;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace ServoCommander
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private RobotConnection robot = new RobotConnection();

        public MainWindow()
        {
            InitializeComponent();
            robot.InitObject(UpdateInfo);
            robot.SetSerialPorts(portsComboBox);
            SetCommandPanel();
            SetStatus();
            
        }

        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            if (robot.isConnected) robot.Close();
        }

        private void findPortButton_Click(object sender, RoutedEventArgs e)
        {
            robot.SetSerialPorts(portsComboBox, (string)portsComboBox.SelectedValue);
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            UpdateInfo();
            if (robot.isConnected)
            {
                robot.Disconnect();
            }
            else
            {
                robot.Connect((string)portsComboBox.SelectedValue);
            }
            SetStatus();
        }

        private void btnNetConnect_Click(object sender, RoutedEventArgs e)
        {
            UpdateInfo();
            if (robot.isConnected)
            {
                robot.Disconnect();
            }
            else
            {
                try
                {
                    int port = int.Parse(txtPort.Text);
                    robot.Connect(txtIP.Text, port);
                } catch
                {

                }
            }
            SetStatus();
        }

        private void SetStatus()
        {
            bool connected = robot.isConnected;
            portsComboBox.IsEnabled = !connected;
            findPortButton.IsEnabled = !connected;
            findPortButton.Visibility = (connected ? Visibility.Hidden : Visibility.Visible);
            txtIP.IsEnabled = !connected;
            txtPort.IsEnabled = !connected;
            
            if (connected)
            {
                btnConnect.Content = "斷開串口";
                btnNetConnect.Content = "斷開網路";
                btnConnect.IsEnabled = (robot.currMode == RobotConnection.connMode.Serial);
                btnNetConnect.IsEnabled = (robot.currMode == RobotConnection.connMode.Network);
            } else
            {
                btnConnect.Content = "串口連接";
                btnConnect.IsEnabled = true;
                btnNetConnect.Content = "網路連接";
                btnNetConnect.IsEnabled = true;
            }

            gridConnection.Background = new SolidColorBrush(connected ? Colors.LightBlue : Colors.LightGray);
            gridCommand.IsEnabled = true;  // allow to test command all the time
            gridCommand.Background = new SolidColorBrush(connected ? Colors.LightGreen : Colors.LightSalmon);
            btnExecute.Content = (connected ? "發送指令 (_S)" : "生成指令 (_S)");
        }
    

        private enum CT
        {
            ControlBoard, UBTech, HaiLzd
        }

        private CT commandType;
        uc.UcCommand__base ucCommand = null;

        private CT getCommandType()
        {
            if (rbUBTech.IsChecked == true) return CT.UBTech;
            if (rbHaiLzd.IsChecked == true) return CT.HaiLzd;
            return CT.ControlBoard;

        }

        private void rbCommand_Checked(object sender, RoutedEventArgs e)
        {
            SetCommandPanel();
        }

        private void SetCommandPanel()
        {
            if (this.gridCommand == null) return;
            this.gridCommand.Children.Clear();
            commandType = getCommandType();
            switch (commandType)
            {
                case CT.UBTech:
                    ucCommand = new uc.UcCommand_UBTech();
                    break;
                case CT.HaiLzd:
                    ucCommand = new uc.UcCommand_HaiLzd();
                    break;
                default:
                    ucCommand = new uc.UcCommand_ControlBoard();
                    break;
            }
            ucCommand.InitObject(UpdateInfo, AppendLog, robot);
            this.gridCommand.Children.Add(ucCommand);
        }

        private void tb_PreviewCommand(object sender, TextCompositionEventArgs e)
        {
            UTIL.INPUT.PreviewCommand(ref e);
        }

        private void tb_PreviewInteger(object sender, TextCompositionEventArgs e)
        {
            UTIL.INPUT.PreviewInteger(ref e);
        }

        private void tb_PreviewIP(object sender, TextCompositionEventArgs e)
        {
            UTIL.INPUT.PreviewIP(ref e);
        }

        private void tb_PreviewHex(object sender, TextCompositionEventArgs e)
        {
            UTIL.INPUT.PreviewHex(ref e);
        }

        private void tb_PreviewHexMix(object sender, TextCompositionEventArgs e)
        {
            UTIL.INPUT.PreviewHexMix(ref e);
        }

        private void tb_PreviewKeyDown_nospace(object sender, System.Windows.Input.KeyEventArgs e)
        {
            UTIL.INPUT.PreviewKeyDown_nospace(ref e);
        }



        private void btnExecute_Click(object sender, RoutedEventArgs e)
        {
            if (ucCommand == null) return;
            UpdateInfo();
            ucCommand.ExecuteCommand();
        }

        private void btnClearLog_Click(object sender, RoutedEventArgs e)
        {
            txtLog.Text = "";
        }
    }
}

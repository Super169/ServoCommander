using MyUtil;
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

            LocUtil.SetDefaultLanguage(this);

            foreach (System.Windows.Controls.MenuItem item in miLanguages.Items)
            {
                if (item.Tag.ToString().Equals(LocUtil.GetCurrentCultureName(this))) item.IsChecked = true;
            }

            robot.InitObject(UpdateInfo);
            robot.SetSerialPorts(portsComboBox);
            robot.SetNetConnection(txtIP, txtPort);
            SetDefaultCommandType();
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
            int baudRate;
            bool baudOK = false;
            string sBaud = cboBaud.Text.Trim();
            if (int.TryParse(sBaud, out baudRate))
            {
                baudOK = (baudRate >= 9600);
            }
            if (!baudOK)
            {
                UpdateInfo(String.Format((string) LocUtil.FindResource("base.msgInavidBaudRate"), sBaud));
                return;
            }

            if (robot.isConnected)
            {
                robot.Disconnect();
            }
            else
            {
                robot.Connect((string)portsComboBox.SelectedValue, baudRate, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
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
                }
                catch
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
            cboBaud.IsEnabled = !connected;
            findPortButton.Visibility = (connected ? Visibility.Hidden : Visibility.Visible);
            txtIP.IsEnabled = !connected;
            txtPort.IsEnabled = !connected;

            if (connected)
            {
                btnConnect.IsEnabled = (robot.currMode == RobotConnection.connMode.Serial);
                btnNetConnect.IsEnabled = (robot.currMode == RobotConnection.connMode.Network);
            }
            else
            {
                btnConnect.IsEnabled = true;
                btnNetConnect.IsEnabled = true;
            }

            gridConnection.Background = new SolidColorBrush(connected ? Colors.LightBlue : Colors.LightGray);
            gridCommand.IsEnabled = true;  // allow to test command all the time
            gridCommand.Background = new SolidColorBrush(connected ? Colors.LightGreen : Colors.LightSalmon);
            ucCommand.ConnectionChanged();
            SetButtonLabel();
        }

        private enum CT
        {
            ControlBoard, UBTech, HaiLzd
        }

        private CT commandType;
        uc.UcCommand__base ucCommand = null;

        private CT getCommandType()
        {
            if (miUBTech.IsChecked == true) return CT.UBTech;
            if (miHaiLzd.IsChecked == true) return CT.HaiLzd;
            return CT.ControlBoard;
            /*
            if (rbUBTech.IsChecked == true) return CT.UBTech;
            if (rbHaiLzd.IsChecked == true) return CT.HaiLzd;
            return CT.ControlBoard;
            */

        }

        private const string LAST_COMMAND_TYPE = "Last Command Type";

        private void SetDefaultCommandType()
        {
            string lastType = (string)UTIL.ReadRegistry(LAST_COMMAND_TYPE);
            switch (lastType)
            {
                case "UBTech":
                    miUBTech.IsChecked = true;
                    // rbUBTech.IsChecked = true;
                    break;
                case "HaiLzd":
                    miHaiLzd.IsChecked = true;
                    // rbHaiLzd.IsChecked = true;
                    break;
                default:
                    miControlBoard.IsChecked = true;
                    // rbControlBoard.IsChecked = true;
                    break;
            }
            SetCommandPanel();
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
                    UTIL.WriteRegistry(LAST_COMMAND_TYPE, "UBTech");
                    ucCommand = new uc.UcCommand_UBTech();
                    break;
                case CT.HaiLzd:
                    UTIL.WriteRegistry(LAST_COMMAND_TYPE, "HaiLzd");
                    ucCommand = new uc.UcCommand_HaiLzd();
                    break;
                default:
                    UTIL.WriteRegistry(LAST_COMMAND_TYPE, "ControlBoard");
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

        private void miLanguages_Click(object sender, RoutedEventArgs e)
        {
            foreach (System.Windows.Controls.MenuItem item in miLanguages.Items)
            {
                item.IsChecked = false;
            }

            System.Windows.Controls.MenuItem mi = sender as System.Windows.Controls.MenuItem;
            mi.IsChecked = true;
            LocUtil.SwitchLanguage(this, mi.Tag.ToString());
            SetButtonLabel();
        }

        private void miFunctionMenu_Click(object sender, RoutedEventArgs e)
        {
            foreach (System.Windows.Controls.MenuItem item in miFunctionMenu.Items)
            {
                item.IsChecked = false;
            }

            System.Windows.Controls.MenuItem mi = sender as System.Windows.Controls.MenuItem;
            mi.IsChecked = true;
            SetCommandPanel();

        }

        private void miPsxButton_Click(object sender, RoutedEventArgs e)
        {
            WinPsxButtonSetting win = new WinPsxButtonSetting();
            win.Owner = this;
            win.ShowDialog();
            win = null;
        }

        private void SetButtonLabel()
        {
            bool connected = robot.isConnected;
            btnConnect.Content = LocUtil.FindResource(connected ? "btnConnectOff" : "btnConnect");
            btnNetConnect.Content = LocUtil.FindResource(connected ? "btnNetConnectOff" : "btnNetConnect");
            btnExecute.Content = LocUtil.FindResource(connected ? "btnExecute" : "btnShowCommand");
        }

    }
}

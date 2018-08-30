using MyUtil;
using SimpleCOM;
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

        private SerialConnection serial = new SerialConnection();

        public MainWindow()
        {
            InitializeComponent();
            serial.InitialObject(UpdateInfo);
            serial.SetAvailablePorts(portsComboBox, serial.LastConnection);
            SetCommandPanel();
            // InitTimer();
            SetStatus();
            
        }

        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            if (serial.isConnected) serial.Close();
        }

        private void findPortButton_Click(object sender, RoutedEventArgs e)
        {
            serial.SetAvailablePorts(portsComboBox, (string)portsComboBox.SelectedValue);
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            UpdateInfo();
            if (serial.isConnected)
            {
                serial.Disconnect();
            }
            else
            {
                serial.Connect((string)portsComboBox.SelectedValue);
            }
            SetStatus();
        }

        private void SetStatus()
        {
            bool connected = serial.isConnected;
            portsComboBox.IsEnabled = !connected;
            findPortButton.IsEnabled = !connected;
            findPortButton.Visibility = (connected ? Visibility.Hidden : Visibility.Visible);
            btnConnect.Content = (connected ? "斷開" : "連線");
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
            ucCommand.InitObject(UpdateInfo, AppendLog, serial);
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

        private void btnNetConnect_Click(object sender, RoutedEventArgs e)
        {

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

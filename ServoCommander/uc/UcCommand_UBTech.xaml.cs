using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace ServoCommander.uc
{
    /// <summary>
    /// Interaction logic for UcCommand_UBTech.xaml
    /// </summary>
    public partial class UcCommand_UBTech : UcCommand__base
    {
        int checkId;
        int minId;
        Timer checkTimer = new Timer();

        public UcCommand_UBTech()
        {
            InitializeComponent();
        }

        private void btnCheckID_Click(object sender, RoutedEventArgs e)
        {
            UpdateMaxId();
            checkId = 1;
            minId = 0;
            //gridConnection.IsEnabled = false;
            // gridCommand.IsEnabled = false;
            UpdateInfo("Checking ID, please wait......", UTIL.InfoType.alert);
            checkTimer.Enabled = true;
            checkTimer.Start();
        }

        private void btnClearLog_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btnExecute_Click(object sender, RoutedEventArgs e)
        {
        }

        private void UpdateMaxId()
        {
            if (int.TryParse(txtMaxId.Text.Trim(), out int maxId))
            {
                CONST.MAX_SERVO = maxId;
            }
        }

        private void adjAngle_Changed(object sender, RoutedEventArgs e)
        {
            string adjAngle = txtAdjAngle.Text;
            try
            {
                int adjValue = Convert.ToInt32(adjAngle, 16);
                bool validInput = (((adjValue >= 0x0000) && (adjValue <= 0x0130)) ||
                                   ((adjValue >= 0xFED0) && (adjValue <= 0xFFFF)));
                txtAdjAngle.Background = new SolidColorBrush((validInput ? Colors.White : Colors.LightPink));
                if (validInput)
                {
                    sliderAdjValue.Value = adjValue;
                }

            }
            catch (Exception)
            {
                txtAdjAngle.Background = new SolidColorBrush(Colors.LightPink);
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int n = (int)((Slider)sender).Value;

            if (n == 0)
            {
                txtAdjMsg.Text = String.Format("沒有偏移");
            }
            else if (n > 0)
            {
                txtAdjMsg.Text = String.Format("正向偏移 {0}", n);
            }
            else
            {
                txtAdjMsg.Text = String.Format("反向偏移 {0}", -n);
            }
            if (n < 0) n += 65536;
            txtAdjAngle.Text = n.ToString("X4");
        }


    }
}

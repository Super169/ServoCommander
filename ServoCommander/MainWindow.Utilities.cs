using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Threading;

namespace ServoCommander
{
    public partial class MainWindow : Window
    {
        private void UpdateInfo(string msg = "", UTIL.InfoType iType = UTIL.InfoType.message, bool async = false)
        {
            if (Dispatcher.FromThread(Thread.CurrentThread) == null)
            {
                if (async)
                {
                    Application.Current.Dispatcher.BeginInvoke(
                      System.Windows.Threading.DispatcherPriority.Normal,
                      (Action)(() => UpdateInfo(msg, iType, async)));
                    return;
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(
                      System.Windows.Threading.DispatcherPriority.Normal,
                      (Action)(() => UpdateInfo(msg, iType, async)));
                    return;
                }
            }
            // Update UI is allowed here
            switch (iType)
            {
                case UTIL.InfoType.message:
                    statusBar.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x7A, 0xCC));
                    break;
                case UTIL.InfoType.alert:
                    statusBar.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xCA, 0x51, 0x00));
                    break;
                case UTIL.InfoType.error:
                    statusBar.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x00, 0x00));
                    break;

            }
            statusInfoTextBlock.Text = msg;
        }

        private void AppendLog(string msg = "", bool async = false)
        {
            if (Dispatcher.FromThread(Thread.CurrentThread) == null)
            {
                if (async)
                {
                    Application.Current.Dispatcher.BeginInvoke(
                      System.Windows.Threading.DispatcherPriority.Normal,
                      (Action)(() => AppendLog(msg, async)));
                    return;
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(
                      System.Windows.Threading.DispatcherPriority.Normal,
                      (Action)(() => AppendLog(msg, async)));
                    return;
                }
            }
            // Update UI is allowed here
            if (!msg.EndsWith("\n")) msg += "\n";
            txtLog.AppendText(msg);
            txtLog.ScrollToEnd();

        }

        private bool MessageConfirm(String msg)
        {
            MessageBoxResult result = MessageBox.Show(msg, "請確定", MessageBoxButton.YesNo, MessageBoxImage.Question);
            return (result == MessageBoxResult.Yes);
        }

    }
}

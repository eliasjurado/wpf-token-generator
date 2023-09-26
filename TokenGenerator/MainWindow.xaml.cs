using Newtonsoft.Json.Linq;
using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using TokenGenerator.Infrastructure.Constants;

namespace TokenGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            cbHasBody.IsChecked = false;
            txBody.IsEnabled = false;
            Title += $" v.{Assembly.GetExecutingAssembly().GetName().Version}";
            txUri.Focus();
        }

        private static string ComputeHash(string message, string secret)
        {
            secret = secret ?? string.Empty;

            Encoding utf8 = Encoding.UTF8;

            byte[] keyByte = utf8.GetBytes(secret);
            byte[] messageBytes = utf8.GetBytes(message);

            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
                return ByteArrayToString(hashmessage);
            }
        }

        public static string ByteArrayToString(byte[] ba)
        {
            var hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
            {
                hex.AppendFormat("{0:x2}", b);
            }
            return hex.ToString();
        }

        private void cbHasBody_CheckedChanged(object sender, EventArgs e)
        {
            txBody.IsEnabled = cbHasBody.IsChecked ?? false;
        }

        private void SetOutput(string message)
        {
            txOutput.Text = string.Empty;
            txOutput.Foreground = Brushes.Red;
            txCopied.IsEnabled = false;
            txOutput.IsEnabled = false;
            txOutput.Text = $"Output: {message}\n";
            ResetOutput(3000);

        }

        public void ResetOutput(int timeout)
        {
            var timer = new Timer();
            timer.Interval = timeout;
            timer.Elapsed += delegate (object source, ElapsedEventArgs e)
            {
                timer.Stop();
            };
            timer.Start();
        }

        private void rbtAuto_CheckedChanged(object sender, EventArgs e)
        {
            txTime.IsEnabled = true;
            dtCustom.IsEnabled = false;
            txTimestamp.IsEnabled = false;
        }

        private void rbtDate_CheckedChanged(object sender, EventArgs e)
        {
            dtCustom.IsEnabled = true;
            txTimestamp.IsEnabled = false;
            txTime.Text = string.Empty;
        }

        private void rbtString_CheckedChanged(object sender, EventArgs e)
        {
            dtCustom.IsEnabled = false;
            txTimestamp.IsEnabled = true;
            txTime.Text = string.Empty;
        }

        private void cboTime_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (cboTime.SelectedItem.ToString())
            {
                case TimeInputType.AutoCurrentTime:
                    dtCustom.IsEnabled = false;
                    txTimestamp.IsEnabled = false;
                    break;
                case TimeInputType.ManualDate:
                    dtCustom.IsEnabled = true;
                    txTimestamp.IsEnabled = false;
                    break;
                case TimeInputType.ManualText:
                    dtCustom.Text = string.Empty;
                    dtCustom.IsEnabled = false;
                    txTimestamp.IsEnabled = true;
                    break;
            }
        }

        private void chkManual_CheckedChanged(object sender, EventArgs e)
        {
            txKey.IsEnabled = !txKey.IsEnabled;
        }

        private void txTime_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.SetText(txTime.Text);
                ResetOutput(2000);
                txOutput.Text = string.Empty;
                txOutput.Foreground = Brushes.Red;
                txCopied.IsEnabled = false;
                txOutput.IsEnabled = false;
            }
            catch (Exception)
            {
                SetOutput($"Error. Empty timestamp result");
                cleanResults();
            }
        }

        private void txHash_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.SetText(txHash.Text);
                ResetOutput(2000);
                txOutput.Text = string.Empty;
                txOutput.Foreground = Brushes.Red;
                txCopied.IsEnabled = false;
                txOutput.IsEnabled = false;
            }
            catch (Exception)
            {
                SetOutput($"Error. Empty hash result");
                cleanResults();
            }
        }

        private void cleanResults()
        {
            txMessage.Text = string.Empty;
            txTime.Text = string.Empty;
            txHash.Text = string.Empty;
        }

        private void Click_Generate(object sender, RoutedEventArgs e)
        {
            var key = string.Empty;
            switch (cboClient.SelectedItem.ToString())
            {
                case InternalSystem.Sales:
                    key = MockToken.Sales;
                    break;
                case InternalSystem.Labor:
                    key = MockToken.Labor;
                    break;
            }

            if (txKey.IsEnabled)
            {
                key = txKey.Text;
            }

            var date = string.Empty;
            var dtNow = DateTime.UtcNow;
            var strNow = dtNow.ToString(Helper.DateFormat);

            if (txTime.IsEnabled)
            {
                date = strNow;
            }

            if (dtCustom.IsEnabled && dtCustom.SelectedDate.HasValue)
            {
                date = dtCustom.SelectedDate.Value.ToString(Helper.DateFormat);
            }

            if (txTimestamp.IsEnabled)
            {
                date = txTimestamp.Text.Trim();
            }

            string jsonText = string.Empty;
            if (txBody.IsEnabled == true)
            {
                if (txBody.Text != string.Empty)
                {
                    try
                    {
                        jsonText = JToken.Parse(txBody.Text.Replace("\r\n", string.Empty)).ToString((Newtonsoft.Json.Formatting)Formatting.None);
                        txOutput.Text = string.Empty;
                    }
                    catch (Exception ex)
                    {
                        SetOutput($"Error. Invalid json body. {ex.Message}");
                        cleanResults();
                        return;
                    }
                }
                else
                {
                    SetOutput($"Info. Json body not found");
                    cleanResults();
                    return;
                }
            }
            string uri = txUri.Text.Trim();
            if (uri == string.Empty)
            {
                SetOutput($"Info. You're missing a request URI");
                cleanResults();
                return;
            }
            if (!uri.StartsWith("/"))
            {
                SetOutput($"Info. You're missing an '/' at the begining of your request URI");
                cleanResults();
                return;
            }
            if (date == string.Empty)
            {
                SetOutput($"Info. You're missing a timestamp");
                cleanResults();
                return;
            }
            if (!DateTime.TryParse(date, out DateTime outdate) && date != string.Empty)
            {
                SetOutput($"Info. Manual text time can't be parsed to timestamp. Format '{Helper.DateFormat}'");
                cleanResults();
                return;
            }
            txTime.Text = date;
            string message = string.Format("{0}|{1}|{2}", date, txUri.Text.Trim(), jsonText);
            txMessage.Text = message;
            txHash.Text = ComputeHash(message, key);
            if (cboTime.SelectedIndex == 0)
            {
                SetOutput($"Info. Valid until {dtNow.ToLocalTime().AddMinutes(5).ToString("HH:mm:ss")} of your local time");
            };
        }
    }
}

using System;
using System.Configuration;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TwitchStreamInfo.Models;

namespace TwitchStreamInfo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TwitchStreamInfoMain main;

        public MainWindow()
        {
            InitializeComponent();
            if (Initialize())
            {
                var apiClientId = ConfigurationManager.AppSettings["ApiClientId"].ToString();
                main = new TwitchStreamInfoMain(apiClientId);
            }
        }

        private bool Initialize()
        {
            if (ConfigurationManager.AppSettings["ApiClientId"] == null || ConfigurationManager.AppSettings["ApiClientId"].ToString().Length == 0)
            {
                ErrorTextBlock.Text = "ApiClientId is not configured";
                DisableAll();
                return false;
            }

            if (ConfigurationManager.AppSettings["DefaultUserName"] != null)
            {
                StreamerTextBox.Text = ConfigurationManager.AppSettings["DefaultUserName"].ToString();
            }

            return true;
        }

        protected virtual async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            DisableAll();
            StopButton.IsEnabled = true;
            UpdateIntervalComboBox.IsEnabled = false;
            InfoTextBlock.Text = ViewersTextBlock.Text = string.Empty;

            var interval = Convert.ToInt32(((ComboBoxItem)UpdateIntervalComboBox.Items[UpdateIntervalComboBox.SelectedIndex]).Tag);
            var result = await main.StartPeriodicTask(StreamerTextBox.Text, interval, OnTick);

            if (!result)
            {
                ErrorTextBlock.Text = "Error when fetching channel";
                DisableAll();
                StartButton.IsEnabled = StreamerTextBox.IsEnabled = true;
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            EnableAll();
            StopButton.IsEnabled = false;
            ViewerCountValue.Text = InfoTextBlock.Text = "stopped";
            LastUpdated.Text = "";
            main.CancelCancellationToken();
        }

        private async Task OnTick()
        {
            try
            {
                StreamInformation info = await main.GetStreamInformation();

                ViewerCountValue.Text = info.viewers.ToString();
                InfoTextBlock.Text = @"Name: " + info.channel.display_name + Environment.NewLine;
                InfoTextBlock.Text += @"Game: " + info.game + Environment.NewLine;
                InfoTextBlock.Text += @"Status: " + info.channel.status + Environment.NewLine;
                InfoTextBlock.Text += @"Language: " + info.channel.language + Environment.NewLine;
                InfoTextBlock.Text += @"Video height: " + info.video_height + Environment.NewLine;
                InfoTextBlock.Text += @"Average fps: " + info.average_fps + Environment.NewLine;
                InfoTextBlock.Text += @"Started at: " + info.created_at + Environment.NewLine;
                InfoTextBlock.Text += @"Views: " + info.channel.views + Environment.NewLine;
                InfoTextBlock.Text += @"Followers: " + info.channel.followers + Environment.NewLine;
                InfoTextBlock.Text += @"URL: " + info.channel.url + Environment.NewLine;

                LastUpdated.Text = DateTime.Now.ToString(CultureInfo.CurrentCulture.DateTimeFormat);

                var chatters = await main.GetStreamChatters(StreamerTextBox.Text.ToLower());
                ViewersTextBlock.Text = chatters;
            }
            catch
            {
                ErrorTextBlock.Text = "Couldn't fetch stream info";
            }
        }

        private void EnableAll()
        {
            StartButton.IsEnabled = StopButton.IsEnabled = UpdateIntervalComboBox.IsEnabled = StreamerTextBox.IsEnabled = true;
        }

        private void DisableAll()
        {
            StartButton.IsEnabled = StopButton.IsEnabled = UpdateIntervalComboBox.IsEnabled = StreamerTextBox.IsEnabled = false;
        }

    }
}

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TwitchStreamInfo.Models;

namespace TwitchStreamInfo
{
    public class TwitchStreamService
    {
        private readonly string _apiClientId;
        private readonly string _getStreamInformationUrl = "https://api.twitch.tv/kraken/streams/{0}";
        private readonly string _getStreamUserUrl = "https://api.twitch.tv/kraken/users?login={0}";
        private readonly string _getViewersUrl = "http://tmi.twitch.tv/group/user/{0}/chatters";
        private readonly string _acceptHeader = "application/vnd.twitchtv.v5+json";
        private string _channelId;
        private CancellationTokenSource _ts;
        HttpClient client;

        public TwitchStreamService(string apiClientId)
        {
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            client = new HttpClient(new HttpClientHandler() { UseProxy = false });
            _apiClientId = apiClientId;
        }

        public async Task<StreamInformation> GetStreamInformation()
        {
            var result = await DownloadString(new Uri(string.Format(_getStreamInformationUrl, _channelId))).ConfigureAwait(false);
            var streamInformationWrapper = JsonConvert.DeserializeObject<StreamInformationWrapper>(result);
            return streamInformationWrapper.stream;
        }

        public async Task<string> GetStreamChatters(string streamerName)
        {
            return await DownloadString(new Uri(string.Format(_getViewersUrl, streamerName))).ConfigureAwait(false);
        }

        public async Task<bool> StartPeriodicTask(string streamerName, int selectedInterval, Func<Task> OnTick)
        {
            _channelId = await GetChannelIdByUserName(streamerName);

            if (_channelId != default(string) && _channelId.Length > 0)
            {
                _ts = new CancellationTokenSource();
                CancellationToken ct = _ts.Token;
                var dueTime = TimeSpan.FromSeconds(0);
                var interval = TimeSpan.FromSeconds(selectedInterval);
                await RunPeriodicAsync(OnTick, dueTime, interval, ct);
            }
            else
            {
                return false;
            }

            return true;
        }

        public void CancelCancellationToken()
        {
            if (_ts != null)
            {
                _ts.Cancel();
            }
        }

        private static async Task RunPeriodicAsync(Func<Task> onTick,
                                                   TimeSpan dueTime,
                                                   TimeSpan interval,
                                                   CancellationToken token)
        {
            // Initial wait time before we begin the periodic loop.
            if (dueTime > TimeSpan.Zero)
                await Task.Delay(dueTime, token);

            // Repeat this loop until cancelled.
            while (!token.IsCancellationRequested)
            {
                // Call our onTick function.
                try
                {
                    await onTick?.Invoke();

                    // Wait to repeat again.
                    if (interval > TimeSpan.Zero)
                        await Task.Delay(interval, token);
                }
                catch (TaskCanceledException)
                {
                }
            }
        }

        private async Task<string> DownloadString(Uri uri)
        {
            string result = null;

            var request = new HttpRequestMessage()
            {
                RequestUri = uri,
                Method = HttpMethod.Get,
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(_acceptHeader));
            request.Headers.Add("Client-ID", new string[] { _apiClientId });

            var httpResponse = await client.SendAsync(request).ConfigureAwait(false);
            result = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            return result;
        }

        private async Task<string> GetChannelIdByUserName(string userName)
        {
            string name = null;

            try
            {
                var result = await DownloadString(new Uri(string.Format(_getStreamUserUrl, userName))).ConfigureAwait(false);

                var streamInformationWrapper = JsonConvert.DeserializeObject<StreamUserWrapper>(result);
                var streamUser = streamInformationWrapper.users[0];
                name = streamUser._id;
            }
            catch (Exception ex)
            {

            }

            return name;
        }
    }
}

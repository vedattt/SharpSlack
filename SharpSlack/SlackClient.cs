using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SharpSlack.JsonObjects;

/*
 * Need to:
 * - Add caching
*/

namespace SharpSlack
{
    public class SlackClient // Make this IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _token;
        private ClientWebSocket _socket;

        private ConcurrentQueue<string> _outgoingQueue;
        private ConcurrentQueue<string> _acknowledgementQueue;

        public SlackClient(string oauthToken)
        {
            _token = oauthToken;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        }

        public async Task ConnectSocketMode(string appToken)
        {
            if (_socket != null)
                throw new InvalidOperationException("There is already an active socket");

            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://slack.com/api/apps.connections.open"))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", appToken);

                var response = await _httpClient.SendAsync(requestMessage);
                var responseJson = JsonConvert.DeserializeObject<OpenConnectionResponse>(await response.Content.ReadAsStringAsync());

                if (responseJson.Ok != true)
                    throw new Exception("Response not OK");

                _socket = new ClientWebSocket();
                await _socket.ConnectAsync(new Uri(responseJson.Url), CancellationToken.None);
                StartReceiveLoop();
                StartSendLoop();
            }
        }

        private void StartReceiveLoop()
        {
            Task.Factory.StartNew(async () =>
            {
                var buffer = new ArraySegment<byte>(new byte[1024]);
                while (_socket.State == WebSocketState.Open)
                {
                    var stream = new MemoryStream();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await _socket.ReceiveAsync(buffer, CancellationToken.None);
                        stream.Write(buffer.Array, buffer.Offset, result.Count);
                    } while (!result.EndOfMessage);
                    stream.Seek(0, SeekOrigin.Begin);

                    var byteData = stream.ToArray();
                    var stringData = Encoding.UTF8.GetString(byteData);
                    var messageJson = JsonConvert.DeserializeObject<SocketMessage>(stringData);
                    _acknowledgementQueue.Enqueue(JsonConvert.SerializeObject(new SocketMessageAcknowledgement { EnvelopeId = messageJson.EnvelopeId }));

                    Console.WriteLine(Encoding.UTF8.GetString(byteData));
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void StartSendLoop()
        {
            _outgoingQueue = new ConcurrentQueue<string>();
            _acknowledgementQueue = new ConcurrentQueue<string>();

            Task.Factory.StartNew(async () => 
            {
                while (_socket.State == WebSocketState.Open)
                {
                    while (!_acknowledgementQueue.IsEmpty || !_outgoingQueue.IsEmpty)
                    {
                        var queue = _acknowledgementQueue.IsEmpty ? _outgoingQueue : _acknowledgementQueue;
                        queue.TryDequeue(out var message);

                        var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
                        await _socket.SendAsync(buffer, WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);
                    }
                }
            });
        }

        public async Task CloseSocketMode()
        {
            if (_socket is null)
                return;

            await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed normally", CancellationToken.None);

            _socket.Dispose();
            _socket = null;
        }

        public async Task<string> PostMessage(string channelId, string text)
        {
            var content = new StringContent(JsonConvert.SerializeObject(
                new { channel = channelId, text }
            ), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("https://slack.com/api/chat.postMessage", content);
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<IList<SlackChannel>> GetConversationList()
        {
            var response = await _httpClient.GetAsync("https://slack.com/api/conversations.list");
            var responseJson = JsonConvert.DeserializeObject<ConversationListResponse>(await response.Content.ReadAsStringAsync());

            if (responseJson.Ok != true)
                throw new Exception("Response not OK");

            return responseJson.Channels;
        }

        public async Task<SlackChannel> FindChannelByName(string channelName)
        {
            var list = await GetConversationList();
            return list.Where(c => c.Name == channelName).Single();
        }

        public async Task<IList<SlackUser>> GetUserList()
        {
            var response = await _httpClient.GetAsync("https://slack.com/api/users.list");
            var responseJson = JsonConvert.DeserializeObject<UserListResponse>(await response.Content.ReadAsStringAsync());

            if (responseJson.Ok != true)
                throw new Exception("Response not OK");

            return responseJson.Members;
        }

        public async Task<SlackUser> FindUserByEmail(string email)
        {
            var response = await _httpClient.GetAsync($"https://slack.com/api/users.lookupByEmail?email={email}");
            var responseJson = JsonConvert.DeserializeObject<EmailLookupResponse>(await response.Content.ReadAsStringAsync());

            if (responseJson.Ok != true)
                throw new Exception("Response not OK");

            return responseJson.User;
        }

        public async Task<SlackDirectMessageConversation> OpenDirectMessageConversation(List<string> userIds)
        {
            var content = new StringContent(JsonConvert.SerializeObject(
                new { users = string.Join(",", userIds) }
            ), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("https://slack.com/api/conversations.open", content);

            var responseJson = JsonConvert.DeserializeObject<OpenDirectMessageConversationResponse>(await response.Content.ReadAsStringAsync());

            if (responseJson.Ok != true)
                throw new Exception("Response not OK");

            return responseJson.Channel;
        }

        public async Task<string> UploadFile(List<string> channelIds, byte[] fileData, string filename, string comment)
        {
            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(fileData), "file", filename);

            if (channelIds != null)
                content.Add(new StringContent(string.Join(",", channelIds)), "channels");
            if (filename != null)
                content.Add(new StringContent(filename), "filename");
            if (comment != null)
                content.Add(new StringContent(comment), "initial_comment");

            var response = await _httpClient.PostAsync("https://slack.com/api/files.upload", content);
            return await response.Content.ReadAsStringAsync();
        }
    }
}


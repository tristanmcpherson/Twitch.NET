using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Chat;
using Grpc.Core;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Events;

namespace TwitchMoq
{
    public class TwitchMock : Chat.TwitchChat.TwitchChatBase, ITwitchClient
    {
        public bool AutoReListenOnException { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public MessageEmoteCollection ChannelEmotes => throw new NotImplementedException();

        public ConnectionCredentials ConnectionCredentials => throw new NotImplementedException();

        public bool DisableAutoPong { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool IsConnected => throw new NotImplementedException();

        public bool IsInitialized => throw new NotImplementedException();

        public IReadOnlyList<JoinedChannel> JoinedChannels => throw new NotImplementedException();

        public bool OverrideBeingHostedCheck { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public WhisperMessage PreviousWhisper => throw new NotImplementedException();

        public string TwitchUsername => throw new NotImplementedException();

        public bool WillReplaceEmotes { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public event EventHandler<OnBeingHostedArgs> OnBeingHosted;
        public event EventHandler<OnChannelStateChangedArgs> OnChannelStateChanged;
        public event EventHandler<OnChatClearedArgs> OnChatCleared;
        public event EventHandler<OnChatColorChangedArgs> OnChatColorChanged;
        public event EventHandler<OnChatCommandReceivedArgs> OnChatCommandReceived;
        public event EventHandler<OnConnectedArgs> OnConnected;
        public event EventHandler<OnConnectionErrorArgs> OnConnectionError;
        public event EventHandler<OnDisconnectedEventArgs> OnDisconnected;
        public event EventHandler<OnExistingUsersDetectedArgs> OnExistingUsersDetected;
        public event EventHandler<OnGiftedSubscriptionArgs> OnGiftedSubscription;
        public event EventHandler<OnHostingStartedArgs> OnHostingStarted;
        public event EventHandler<OnHostingStoppedArgs> OnHostingStopped;
        public event EventHandler OnHostLeft;
        public event EventHandler<OnIncorrectLoginArgs> OnIncorrectLogin;
        public event EventHandler<OnJoinedChannelArgs> OnJoinedChannel;
        public event EventHandler<OnLeftChannelArgs> OnLeftChannel;
        public event EventHandler<OnLogArgs> OnLog;
        public event EventHandler<OnMessageReceivedArgs> OnMessageReceived;
        public event EventHandler<OnMessageSentArgs> OnMessageSent;
        public event EventHandler<OnModeratorJoinedArgs> OnModeratorJoined;
        public event EventHandler<OnModeratorLeftArgs> OnModeratorLeft;
        public event EventHandler<OnModeratorsReceivedArgs> OnModeratorsReceived;
        public event EventHandler<OnNewSubscriberArgs> OnNewSubscriber;
        public event EventHandler<OnNowHostingArgs> OnNowHosting;
        public event EventHandler<OnRaidNotificationArgs> OnRaidNotification;
        public event EventHandler<OnReSubscriberArgs> OnReSubscriber;
        public event EventHandler<OnSendReceiveDataArgs> OnSendReceiveData;
        public event EventHandler<OnUserBannedArgs> OnUserBanned;
        public event EventHandler<OnUserJoinedArgs> OnUserJoined;
        public event EventHandler<OnUserLeftArgs> OnUserLeft;
        public event EventHandler<OnUserStateChangedArgs> OnUserStateChanged;
        public event EventHandler<OnUserTimedoutArgs> OnUserTimedout;
        public event EventHandler<OnWhisperCommandReceivedArgs> OnWhisperCommandReceived;
        public event EventHandler<OnWhisperReceivedArgs> OnWhisperReceived;
        public event EventHandler<OnWhisperSentArgs> OnWhisperSent;
        public event EventHandler<OnMessageThrottledEventArgs> OnMessageThrottled;
        public event EventHandler<OnWhisperThrottledEventArgs> OnWhisperThrottled;
        public event EventHandler<OnErrorEventArgs> OnError;
        public event EventHandler<OnReconnectedEventArgs> OnReconnected;

        public void AddChatCommandIdentifier(char identifier)
        {
            throw new NotImplementedException();
        }

        public void AddWhisperCommandIdentifier(char identifier)
        {
            throw new NotImplementedException();
        }

        public void Connect()
        {
        }

        public void Disconnect()
        {
        }

        public JoinedChannel GetJoinedChannel(string channel)
        {
            throw new NotImplementedException();
        }

        public void Initialize(ConnectionCredentials credentials, string channel = null, char chatCommandIdentifier = '!', char whisperCommandIdentifier = '!', bool autoReListenOnExceptions = true)
        {
        }

        private readonly ConcurrentBag<IServerStreamWriter<Chat.ChatMessage>> Clients = new ConcurrentBag<IServerStreamWriter<Chat.ChatMessage>>();

        public override async Task Chat(IAsyncStreamReader<Chat.ChatMessage> requestStream, IServerStreamWriter<Chat.ChatMessage> responseStream, ServerCallContext context)
        {
            Clients.Add(responseStream);

            while (await requestStream.MoveNext())
            {
                await BroadcastMessage(requestStream.Current);
            }
        }

        private async Task BroadcastMessage(Chat.ChatMessage chatMessage)
        {
            if (Clients.IsEmpty)
            {
                return;
            }

            foreach (var client in Clients)
            {
                await client.WriteAsync(chatMessage);
            }

            var twitchChatMessage = new TwitchLib.Client.Models.ChatMessage(chatMessage.Author, null, chatMessage.Author, chatMessage.Author, null, System.Drawing.Color.Black, null, chatMessage.Text, TwitchLib.Client.Enums.UserType.Viewer, chatMessage.Channel.Name, null, true, 10, null, true, false, false, false, TwitchLib.Client.Enums.Noisy.False, null, null, null, null, 0, 0);
            OnMessageReceived?.Invoke(null, new OnMessageReceivedArgs { ChatMessage = twitchChatMessage });
        }

        //public override async Task (Chat.Channel request, IServerStreamWriter<Chat.ChatMessage> responseStream, ServerCallContext context)
        //{
        //    var hasMessages = Messages.TryGetValue(request.Name, out var messages);
        //    if (hasMessages && messages != null)
        //    {
        //        foreach (var message in messages)
        //        {
        //            await responseStream.WriteAsync(message);
        //        }
        //    }            
        //}

        //public override Task<Empty> SendMessage(Chat.ChatMessage request, ServerCallContext context)
        //{
        //    var chatMessage = new TwitchLib.Client.Models.ChatMessage(request.Author, null, request.Author, request.Author, null, System.Drawing.Color.Black, null, request.Text, TwitchLib.Client.Enums.UserType.Viewer, request.Channel.Name, null, true, 10, null, true, false, false, false, TwitchLib.Client.Enums.Noisy.False, null, null, null, null, 0, 0);
        //    var bus = Messages.GetOrAdd(request.Channel.Name, new List<Chat.ChatMessage>());
        //    bus.Add(request);
        //    OnMessageReceived(this, new OnMessageReceivedArgs { ChatMessage = chatMessage });
        //    return Task.FromResult(new Chat.Empty());
        //}

        public void JoinChannel(string channel, bool overrideCheck = false)
        {
            throw new NotImplementedException();
        }

        public void JoinRoom(string channelId, string roomId, bool overrideCheck = false)
        {
            throw new NotImplementedException();
        }

        public void LeaveChannel(JoinedChannel channel)
        {
            throw new NotImplementedException();
        }

        public void LeaveChannel(string channel)
        {
            throw new NotImplementedException();
        }

        public void LeaveRoom(string channelId, string roomId)
        {
            throw new NotImplementedException();
        }

        public void OnReadLineTest(string rawIrc)
        {
            throw new NotImplementedException();
        }

        public void Reconnect()
        {
            throw new NotImplementedException();
        }

        public void RemoveChatCommandIdentifier(char identifier)
        {
            throw new NotImplementedException();
        }

        public void RemoveWhisperCommandIdentifier(char identifier)
        {
            throw new NotImplementedException();
        }

        public void SendMessage(JoinedChannel channel, string message, bool dryRun = false)
        {
            var chatMessage = new Chat.ChatMessage { Author = "BOT", Channel = new Chat.Channel { Name = channel.Channel }, Text = message, TimeStamp = DateTime.UtcNow.Ticks };
            BroadcastMessage(chatMessage).Wait();
        }

        public void SendMessage(string channel, string message, bool dryRun = false)
        {
            var chatMessage = new Chat.ChatMessage { Author = "BOT", Channel = new Chat.Channel { Name = channel }, Text = message, TimeStamp = DateTime.UtcNow.Ticks };
            BroadcastMessage(chatMessage).Wait();
        }

        public void SendQueuedItem(string message)
        {
            throw new NotImplementedException();
        }

        public void SendRaw(string message)
        {
            throw new NotImplementedException();
        }

        public void SendWhisper(string receiver, string message, bool dryRun = false)
        {
            throw new NotImplementedException();
        }

        public void SetConnectionCredentials(ConnectionCredentials credentials)
        {
            throw new NotImplementedException();
        }
    }
}

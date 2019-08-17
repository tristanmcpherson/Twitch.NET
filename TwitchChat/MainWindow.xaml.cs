using Grpc.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace TwitchChat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private Chat.TwitchChat.TwitchChatClient Client;
        private AsyncDuplexStreamingCall<Chat.ChatMessage, Chat.ChatMessage> Duplex;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
   => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler PropertyChanged;


        private string text;
        public string Text
        {
            get => text;
            set { text = value; OnPropertyChanged(); }
        }

        private string textToSend;
        public string TextToSend
        {
            get => textToSend;
            set { textToSend = value; OnPropertyChanged(); }
        }

        public MainWindow()
        {
            var channel = new Channel("127.0.0.1:50052", ChannelCredentials.Insecure);
            
            Client = new Chat.TwitchChat.TwitchChatClient(channel);

            InitializeComponent();
            DataContext = this;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Duplex = Client.Chat();

            var reader = Duplex.ResponseStream;

            try
            {
                while (await reader.MoveNext())
                {
                    var current = reader.Current;
                    Text += $"{current.Author}: {current.Text}\n";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var chatMessage = new Chat.ChatMessage
            {
                Channel = new Chat.Channel { Name = "shredder89100" },
                Author = "shredder89100",
                Text = TextToSend,
                TimeStamp = DateTime.UtcNow.Ticks
            };

            await Duplex.RequestStream.WriteAsync(chatMessage);

            TextToSend = "";
        }
    }
}

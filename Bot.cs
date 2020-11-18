using System;
using System.Collections.Generic;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace PyRZyBot
{
    internal class Bot
    {

        ConnectionCredentials credentials = new ConnectionCredentials(TwitchInfo.BotName, TwitchInfo.BotToken);
        TwitchClient client;
        List<string> Banned_Users = new List<string>();
        internal void Connect(bool isLogging)
        {
            Banned_Users.Add("nightbot");
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(5)
            };
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            client = new TwitchClient(customClient);
            client.Initialize(credentials, TwitchInfo.ChannelName);

            if (isLogging)
                client.OnLog += Client_OnLog;

            client.OnMessageReceived += Client_OnMessageReceived;
            client.OnChatCommandReceived += Client_OnChatCommandReceived;
            client.Connect();
        }

        private void Client_OnChatCommandReceived(object sender, OnChatCommandReceivedArgs e)
        {
            if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster)
            {
                var Username = e.Command.ArgumentsAsList[0].Replace("@", "").ToLower();
                if (e.Command.CommandText == "ban" && Banned_Users.Contains(Username) == false)
                {
                    Banned_Users.Add(Username);
                }
            }
        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            if (Banned_Users.Contains(e.ChatMessage.Username) == true) { return; }

            if (e.ChatMessage.Message.Contains("xD") || e.ChatMessage.Message.Contains("XD"))
            {
                client.SendMessage(TwitchInfo.ChannelName, "xD");
            }



        }

        private void Client_OnLog(object sender, OnLogArgs e)
        {
            Console.WriteLine(e.Data);
        }

        internal void Disconnect()
        {
            client.Disconnect();
        }
    }
}
using System;
using System.Linq;
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
        Random Random = new Random();
        List<string> Banned_Users = new List<string>();
        List<string> Responces = new List<string> { "Tak", "Nie", "xD", "Oj nie wiem nie wiem", "Paaaaanie, bota o to pytasz? litości... :roll_eyes:" };
        List<int> Weights = new List<int> { 8, 8, 1, 3, 1 };

        internal void Connect(bool isLogging)
        {
            Banned_Users.Add("nightbot");
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 5,
                ThrottlingPeriod = TimeSpan.FromSeconds(3)
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
                var Username = e.Command.ArgumentsAsList[0].Replace("@", "");
                if (e.Command.CommandText == "ban" && Banned_Users.Contains(Username.ToLower()) == false)
                {
                    Banned_Users.Add(Username.ToLower());
                    client.SendMessage(TwitchInfo.ChannelName, $"Banned {Username} :partying:");
                }
            }
        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            if (Banned_Users.Contains(e.ChatMessage.Username) == true) { return; }

            if (e.ChatMessage.Message.ToLower().StartsWith("czy"))
            {
                int x = 0;
                var RandomValue = Random.NextDouble() * Weights.Sum();
                for (int i = 0; i < Weights.Count; i++)
                {
                    x += Weights[i];
                    if (x >= RandomValue)
                    {
                        client.SendMessage(TwitchInfo.ChannelName, Responces[i]);
                        return;
                    }
                }
            }
            else
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
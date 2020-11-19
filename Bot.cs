using System;
using System.Linq;
using System.Collections.Generic;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using System.Text.RegularExpressions;
using System.Text;

namespace PyRZyBot
{
    internal class Bot
    {
        TwitchClient client;
        List<string> Banned_Users = new List<string> { "nightbot" };
        List<string> Mods = new List<string> { "kyrzy", "ananieana", "medial802", "pyrzybot" };
        List<string> Responces = new List<string> { "Tak", "Nie", "xD", "Oj nie wiem nie wiem", "Paaaaanie, bota o to pytasz? litości... :roll_eyes:" };
        List<int> Weights = new List<int> { 8, 8, 1, 3, 1 };
        Dictionary<string, int> tldrCounter = new Dictionary<string, int>();
        Dictionary<string, string> simpleCommands = new Dictionary<string, string>();
        string _dotDotDotPattern = @"^(\.)+$";

        internal void Connect(bool isLogging)
        {
            ConnectionCredentials credentials = new ConnectionCredentials(TwitchInfo.BotName, TwitchInfo.BotToken);
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
                var Username = string.Empty;
                if (e.Command.ArgumentsAsList.Count > 0)
                {
                    Username = e.Command.ArgumentsAsList[0].Replace("@", "");
                }
                if (e.Command.CommandText == "ban")
                    if (Banned_Users.Contains(Username.ToLower()) == false)
                    {
                        if (Mods.Contains(Username.ToLower()) == true)
                        {
                            client.SendMessage(TwitchInfo.ChannelName, "Nie można zbanować moderatora!");
                            return;
                        }

                        Banned_Users.Add(Username.ToLower());
                        client.SendMessage(TwitchInfo.ChannelName, $"Zbanowano {Username} :partying:");
                    }
                    else { client.SendMessage(TwitchInfo.ChannelName, $"{Username} już jest zbanowany!"); }

                if (e.Command.CommandText == "unban")
                    if (Banned_Users.Contains(Username.ToLower()) == true)
                    {
                        Banned_Users.Remove(Username.ToLower());
                        client.SendMessage(TwitchInfo.ChannelName, $"Odbanowano {Username} :frowning:");
                    }
                    else { client.SendMessage(TwitchInfo.ChannelName, $"{Username} nie jest zbanowany!"); }

                if (e.Command.CommandText == "command")
                {
                    if (e.Command.ArgumentsAsList[0] == "add"
                        && e.Command.ArgumentsAsList.Count > 2
                        && !simpleCommands.ContainsKey(e.Command.ArgumentsAsList[1].ToLower()))
                    {
                        simpleCommands.Add(e.Command.ArgumentsAsList[1].ToLower(), e.Command.ArgumentsAsString.Substring(4 + e.Command.ArgumentsAsList[1].Length));
                        client.SendMessage(TwitchInfo.ChannelName, $"Komenda {e.Command.ArgumentsAsList[1]} została dodana.");
                    }
                    else
                    if (e.Command.ArgumentsAsList[0] == "delete"
                         && e.Command.ArgumentsAsList.Count > 1
                         && simpleCommands.ContainsKey(e.Command.ArgumentsAsList[1].ToLower()))
                    {
                        simpleCommands.Remove(e.Command.ArgumentsAsList[1].ToLower());
                        client.SendMessage(TwitchInfo.ChannelName, $"Komenda {e.Command.ArgumentsAsList[1]} została usunięta.");
                    }
                }
                if (simpleCommands.ContainsKey(e.Command.CommandText.ToLower()))
                {
                    client.SendMessage(TwitchInfo.ChannelName, simpleCommands[e.Command.CommandText.ToLower()]);
                }
            }
        }


        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            if (Banned_Users.Contains(e.ChatMessage.Username) == true) { return; }

            if (e.ChatMessage.Message.Length > 200)
            {
                if (!tldrCounter.ContainsKey(e.ChatMessage.Username))
                {
                    tldrCounter.Add(e.ChatMessage.Username, 1);
                }
                else
                {
                    tldrCounter[e.ChatMessage.Username]++;
                }
                var output = "Duuuuuud, Don't ściana tekstu me :rage:";
                if (tldrCounter[e.ChatMessage.Username] > 2)
                {
                    output += $" to już {tldrCounter[e.ChatMessage.Username]} raz!";
                }
                client.SendMessage(TwitchInfo.ChannelName, output);
            }
            else if (e.ChatMessage.Message.ToLower().StartsWith("czy"))
            {
                Random Random = new Random();
                var RandomValue = Random.NextDouble() * Weights.Sum();
                for (int i = 0; i < Weights.Count; i++)
                {
                    RandomValue -= Weights[i];
                    if (RandomValue <= 0)
                    {
                        client.SendMessage(TwitchInfo.ChannelName, Responces[i]);
                        return;
                    }
                }
            }
            else if (Regex.Match(e.ChatMessage.Message, _dotDotDotPattern).Success)
            {
                var length = e.ChatMessage.Message.Length <= 6 ? e.ChatMessage.Message.Length : 6;
                var sb = new StringBuilder().Insert(0, "kropka ", length);
                sb.Append($"do Ciebie też {e.ChatMessage.Username} :rage:");

                client.SendMessage(TwitchInfo.ChannelName, sb.ToString());
            }
            else if (e.ChatMessage.Message.Contains("xD") || e.ChatMessage.Message.Contains("XD"))
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
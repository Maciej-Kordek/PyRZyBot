using System;
using System.Timers;
using System.Linq;
using System.Collections.Generic;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using System.Text.RegularExpressions;
using System.Text;
using Newtonsoft.Json;
using System.IO;

namespace PyRZyBot
{
    internal class Bot
    {
        TwitchClient client;
        List<string> Banned_Users;
        List<string> Mods;
        List<string> Responces;
        List<int> Weights;
        Dictionary<string, int> tldrCounter;
        Dictionary<string, int> czySpam = new Dictionary<string, int>();
        Dictionary<string, string> simpleCommands;
        string _dotDotDotPattern = @"^(\.)+$";
        DateTime LSM = DateTime.Now;
        private System.Timers.Timer DSCTimer;
        private System.Timers.Timer czyTimer;

        internal void Connect(bool isLogging)
        {
            var json = string.Empty;
            using (var sr = new StreamReader(Environment.CurrentDirectory + @"\path.txt"))
            {
                json = sr.ReadToEnd();
            }
            if (!string.IsNullOrEmpty(json))
            {
                var savefile = JsonConvert.DeserializeObject<SaveFileTemplate>(json);
                Banned_Users = savefile.Banned_Users;
                Mods = savefile.Mods;
                Responces = savefile.Responces;
                Weights = savefile.Weights;
                tldrCounter = savefile.tldrCounter;
                simpleCommands = savefile.simpleCommands;
            }

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
            SetTimer();
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
                    if (String.IsNullOrEmpty(Username) == true)
                    {
                        client.SendMessage(TwitchInfo.ChannelName, "Musisz podać nazwę użytkownika!");
                        return;
                    }
                    else
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
                    if (String.IsNullOrEmpty(Username) == true)
                    {
                        client.SendMessage(TwitchInfo.ChannelName, "Musisz podać nazwę użytkownika!");
                        return;
                    }
                    else
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
                        client.SendMessage(TwitchInfo.ChannelName, $"Komenda !{e.Command.ArgumentsAsList[1]} została dodana.");
                    }
                    else
                    if (e.Command.ArgumentsAsList[0] == "delete"
                         && e.Command.ArgumentsAsList.Count > 1
                         && simpleCommands.ContainsKey(e.Command.ArgumentsAsList[1].ToLower()))
                    {
                        simpleCommands.Remove(e.Command.ArgumentsAsList[1].ToLower());
                        client.SendMessage(TwitchInfo.ChannelName, $"Komenda !{e.Command.ArgumentsAsList[1]} została usunięta.");
                    }
                }
            }
            if (simpleCommands.ContainsKey(e.Command.CommandText.ToLower()))
            {
                client.SendMessage(TwitchInfo.ChannelName, simpleCommands[e.Command.CommandText.ToLower()]);
            }
        }


        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            if (Banned_Users.Contains(e.ChatMessage.Username) == true) { return; }
            if (LSM.AddSeconds(5) > DateTime.Now) { return; }
            LSM = DateTime.Now;

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
                var output = "Duuuuuud, don't ściana tekstu me :rage:";
                if (tldrCounter[e.ChatMessage.Username] > 2)
                {
                    output += $" to już {tldrCounter[e.ChatMessage.Username]} raz!";
                }
                client.SendMessage(TwitchInfo.ChannelName, output);
            }
            else if (e.ChatMessage.Message.ToLower().Contains("siema pyrzy") || e.ChatMessage.Message.ToLower().Contains("cześć pyrzy") || e.ChatMessage.Message.ToLower().Contains("czesc pyrzy"))
            {
                client.SendMessage(TwitchInfo.ChannelName, $"Cześć {e.ChatMessage.Username}");
                return;
            }
            else if (e.ChatMessage.Message.ToLower().StartsWith("czy"))
            {
                if (!czySpam.ContainsKey(e.ChatMessage.Username))
                {
                    czySpam.Add(e.ChatMessage.Username, 1);
                }
                else
                {
                    czySpam[e.ChatMessage.Username]++;
                }
                if (czySpam[e.ChatMessage.Username] > 7)
                {
                    return;
                }
                if (czySpam[e.ChatMessage.Username] > 5)
                {
                    var digits = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
                    var Username = e.ChatMessage.Username;

                    client.SendMessage(TwitchInfo.ChannelName, $"Przestań zadawać tyle pytań {Username.TrimEnd(digits)} :rage:");
                    czySpam[e.ChatMessage.Username]++;
                    return;
                }
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
                var length = e.ChatMessage.Message.Length <= 10 ? e.ChatMessage.Message.Length : 10;
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

        private void SetTimer()
        {
            czyTimer = new System.Timers.Timer(60000);
            czyTimer.Elapsed += czyEvent;
            czyTimer.AutoReset = true;
            czyTimer.Enabled = true;
            DSCTimer = new System.Timers.Timer(1200000);
            DSCTimer.Elapsed += DSCEvent;
            DSCTimer.AutoReset = true;
            DSCTimer.Enabled = true;
        }

        private void czyEvent(object source, ElapsedEventArgs e)
        {
            List<string> KeyList = new List<string>(czySpam.Keys);
            foreach (var key in KeyList)
            {
                czySpam[key] = czySpam[key] > 0 ? czySpam[key] - 1 : 0;
            }
        }

        private void DSCEvent(Object source, ElapsedEventArgs e)
        {
            client.SendMessage(TwitchInfo.ChannelName, " Chcesz wiedzieć, kiedy będzie kolejny stream? zapraszam na Discorda: https://discord.gg/jtGZQFa");
        }

        internal void Disconnect()
        {
            czyTimer.Stop();
            czyTimer.Dispose();
            DSCTimer.Stop();
            DSCTimer.Dispose();
            var saveFileTemplate = new SaveFileTemplate
            {
                Banned_Users = Banned_Users,
                Mods = Mods,
                Responces = Responces,
                Weights = Weights,
                tldrCounter = tldrCounter,
                simpleCommands = simpleCommands
            };

            string json = JsonConvert.SerializeObject(saveFileTemplate);

            System.IO.File.WriteAllText(Environment.CurrentDirectory + @"\path.txt", json);
            client.Disconnect();
        }
    }
}
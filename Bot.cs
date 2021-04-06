using PyRZyBot_2._0.Entities;
using System;
using System.Linq;
using System.Collections.Generic;
using TwitchLib.Api;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events.LiveStreamMonitor;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.PubSub;
using System.Text.RegularExpressions;

namespace PyRZyBot_2._0
{
    internal class Bot
    {
        public static bool IsConnected = false;

        static TwitchClient client;
        static TwitchPubSub PubSub;
        static LiveStreamMonitorService Monitor;
        public static List<string> Channels = new List<string> { "kyrzy", "ananieana" };
        public static Dictionary<string, TwitchAPI> APIs = new Dictionary<string, TwitchAPI>();
        internal void Connect()
        {
            Program.StatusMessage("Włączanie - 0% ");
            using (var context = new Database())
            {
                ConnectionCredentials credentials = new ConnectionCredentials(
                    context.ChannelInfo.FirstOrDefault(x => x.Channel == "pyrzybot" && x.Info == "BotName").Value,
                    context.ChannelInfo.FirstOrDefault(x => x.Channel == "pyrzybot" && x.Info == "BotToken").Value);

                var clientOptions = new ClientOptions
                {
                    MessagesAllowedInPeriod = 10,
                    ThrottlingPeriod = TimeSpan.FromSeconds(10)
                };
                WebSocketClient customClient = new WebSocketClient(clientOptions);
                client = new TwitchClient(customClient);
                client.Initialize(credentials, Channels);

                int i = 0;
                foreach (var Channel in Channels)
                {
                    i += 25;
                    Program.StatusMessage($"Włączanie - {i}%");
                    var API = new TwitchAPI();
                    API.Settings.ClientId = context.ChannelInfo.FirstOrDefault(x => x.Channel == Channel && x.Info == "ClientId").Value;
                    API.Settings.AccessToken = context.ChannelInfo.FirstOrDefault(x => x.Channel == Channel && x.Info == "AccessToken").Value;
                    APIs.Add(Channel, API);

                    PubSub = new TwitchPubSub();
                    PubSub.ListenToRewards(context.ChannelInfo.FirstOrDefault(x => x.Channel == Channel && x.Info == "ChannelId").Value);
                    PubSub.Connect();

                    PubSub.OnRewardRedeemed += OnRewardRedeemed;
                    PubSub.OnPubSubServiceConnected += OnPubSubServiceConnected;
                }
            }
            Monitor = new LiveStreamMonitorService(APIs["kyrzy"]);
            Monitor.SetChannelsByName(Channels);
            Program.StatusMessage("Włączanie - 75%");

            client.OnMessageReceived += OnMessageReceived;
            client.OnUserJoined += OnUserJoined;
            client.OnUserLeft += OnUserLeft;

            Monitor.OnStreamOnline += OnStreamOnline;
            Monitor.OnStreamOffline += OnStreamOffline;
            client.Connect();
            Monitor.Start();

            Program.StartFailSafe();
            Program.StatusMessage("Włączono - czekam na połączenie z czatem");
        }
        internal void Disconnect()
        {
            Program.FailSafeDispose();
            Program.StatusMessage("Wyłączanie");
            using (var context = new Database())
            {
                var ChannelsOnline = context.ChannelInfo.Where(x => x.Info == "IsStreaming" && x.Value == "1").ToList();
                if (ChannelsOnline.Count() > 0)
                {
                    ChannelsOnline.ForEach(x =>
                    {
                        x.Value = "0";
                        string Channel = x.Channel;
                        var UsersOnline = context.ChatUsers.Where(x => x.IsOnline && x.Channel == Channel).ToList();
                        if (UsersOnline.Count > 0)
                        {
                            UsersOnline.ForEach(x =>
                            {
                                TimeSpan Watchtime = DateTime.Now - x.GotOnline;
                                x.Watchtime += (int)Watchtime.TotalMinutes;
                            });
                            context.UpdateRange(UsersOnline);
                        }
                        context.UpdateRange(ChannelsOnline);
                    });
                }

                var Users = context.ChatUsers.Where(x => x.IsOnline).ToList();
                if (Users.Count() > 0)
                {
                    Users.ForEach(x => x.IsOnline = false);
                    context.UpdateRange(Users);
                }

                var InDuel = context.ChatUsers_S.Where(x => x.DuelId != 0).ToList();
                if (InDuel.Count() > 0)
                {
                    InDuel.ForEach(x => x.DuelId = 0);
                    context.UpdateRange(InDuel);
                }
                context.SaveChanges();
            }
            APIs.Clear();
            client.Disconnect();
            PubSub.Disconnect();
            Monitor.Stop();
            IsConnected = false;
            Program.StatusMessage("Wyłączono\n");
        }

        private void OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            int TwitchId = int.Parse(e.ChatMessage.UserId);
            string Name = e.ChatMessage.DisplayName;
            string Message = e.ChatMessage.Message;
            string Channel = e.ChatMessage.Channel;
            bool IsBroadcaster = e.ChatMessage.IsBroadcaster;
            bool IsMod = e.ChatMessage.IsModerator;
            bool IsVip = e.ChatMessage.IsVip;

            if (Name.ToLower() == "nightbot") { return; }

            Database.CheckUser(TwitchId, Name, Channel, IsBroadcaster, IsMod, IsVip);

            LogMessage(true, Channel, Name, Message);

            if(Message.ToLower() == "!on")
                CustomCommands.StartTimers(Channel);

            if (Message.ToLower() == "!off")
                CustomCommands.StopTimers(Channel);

            CheckMessage(Channel, Name, Message);
        }
        public static void CheckMessage(string Channel, string Name, string Message)
        {
            List<string> Arguments = Regex.Split(Message, @"\s+").ToList();
            //if (Message.StartsWith("!"))
            {
                if (BotCommands(Channel, Name, Arguments))
                {
                    return;
                }
                if (CustomCommands.ChannelCommands(Channel, Name, Arguments))
                {
                    return;
                }
                return;
            }
            //ChatResponses(Channel, Name, Arguments);            
        }
        static bool BotCommands(string Channel, string Name, List<string> Arguments)
        {
            switch (Arguments[0].ToLower())
            {
                case "!set":
                    InternalCommands_Set.Set(Channel, Name, Arguments);
                    break;

                case "!command":
                    CustomCommands.Command(Channel, Name, Arguments);
                    break;

                case "!title":
                    InternalCommands_Channel.Title(Channel, Name, Arguments);
                    break;

                case "!game":
                    InternalCommands_Channel.Game(Channel, Name, Arguments);
                    break;

                case "!next":
                    InternalCommands_Channel.Next(Channel, Name, Arguments);
                    break;

                case "!ban":
                    InternalCommands_Moderation.Ban(Channel, Name, Arguments);
                    break;

                case "!softban":
                    InternalCommands_Moderation.SoftBan(Channel, Name, Arguments);
                    break;

                case "!unban":
                    InternalCommands_Moderation.Unban(Channel, Name, Arguments);
                    break;

                case "!sudo":
                    InternalCommands_Moderation.Sudo(Channel, Name, Arguments);
                    break;

                default:
                    return false;
            }
            return true;
        }
        internal void ConsoleCommands(string Channel, string Name, string Message)
        {
            List<string> Arguments = Message.Split(" ").ToList();
            switch (Arguments[0])
            {
                case "say":
                    SendMessage(Channel, 2, false, Message.Substring(4));
                    return;
            }
        }

        private void OnPubSubServiceConnected(object sender, EventArgs e) { ((TwitchPubSub)sender).SendTopics(); }
        private void OnRewardRedeemed(object sender, TwitchLib.PubSub.Events.OnRewardRedeemedArgs e)
        {
            var Name = e.DisplayName;
            var Reward = e.RewardTitle;
            var Cost = e.RewardCost;
            string Channel;
            string RedeemOn;
            using (var context = new Database())
            {
                Channel = context.ChannelInfo.FirstOrDefault(x => x.Value == e.ChannelId).Channel;
                RedeemOn = context.ChannelInfo.FirstOrDefault(x => x.Channel == Channel && x.Info == "RedeemOn").Value;
            }

            LogEvent(Channel, 0, $"Użytkownik {Name} odebrał nagrodę: {Reward}");

            if (Reward.ToLower().Contains(RedeemOn))
            {
                Points.OnPointsRedeemed(Name, Cost, Channel);
            }
        }

        private void OnStreamOnline(object sender, OnStreamOnlineArgs e)
        {
            string Channel = e.Channel;

            LogEvent(Channel, 2, "Stream się rozpoczął");

            CustomCommands.StartTimers(Channel);

            using (var context = new Database())
            {
                var IsStreaming = context.ChannelInfo.FirstOrDefault(x => x.Channel == Channel && x.Info == "IsStreaming");
                IsStreaming.Value = "1";
                context.Update(IsStreaming);
                var Users = context.ChatUsers.Where(x => x.IsOnline && x.Channel == Channel).ToList();
                if (Users.Count != 0)
                {
                    Users.ForEach(x => x.GotOnline = DateTime.Now);
                    context.UpdateRange(Users);
                }
                context.SaveChanges();
            }
        }
        private void OnStreamOffline(object sender, OnStreamOfflineArgs e)
        {
            string Channel = e.Channel;

            LogEvent(Channel, 2, "Stream się zakończył");

            CustomCommands.StopTimers(Channel);

            using (var context = new Database())
            {
                var IsStreaming = context.ChannelInfo.FirstOrDefault(x => x.Channel == Channel && x.Info == "IsStreaming");
                IsStreaming.Value = "0";
                context.Update(IsStreaming);
                var Users = context.ChatUsers.Where(x => x.IsOnline && x.Channel == Channel).ToList();
                if (Users.Count != 0)
                {
                    Users.ForEach(x =>
                    {
                        TimeSpan Watchtime = DateTime.Now - x.GotOnline;
                        x.Watchtime += (int)Watchtime.TotalMinutes;
                    });
                    context.UpdateRange(Users);
                }
                context.SaveChanges();
            }
        }
        private void OnUserJoined(object sender, OnUserJoinedArgs e)
        {
            string Channel = e.Channel;
            string Name = e.Username;

            if (IsConnected == false)
            {
                IsConnected = true;
                Program.FailSafeDispose();
                Program.StatusMessage("U mnie działa!");
            }

            LogEvent(Channel, 1, $"{Name} Jest Online");

            using (var context = new Database())
            {
                var User = context.ChatUsers.FirstOrDefault(x => x.Channel == Channel && x.Name == Name);
                if (User == null) { return; }
                User.IsOnline = true;
                User.GotOnline = DateTime.Now;
                context.Update(User);
                context.SaveChanges();
            }
        }
        private void OnUserLeft(object sender, OnUserLeftArgs e)
        {
            string Channel = e.Channel;
            string Name = e.Username;

            LogEvent(Channel, 1, $"{Name} Jest Offline");

            using (var context = new Database())
            {
                var User = context.ChatUsers.FirstOrDefault(x => x.Channel == Channel && x.Name == Name);
                if (User == null) { return; }
                User.IsOnline = false;

                var IsStreaming = context.ChannelInfo.FirstOrDefault(x => x.Channel == Channel && x.Info == "IsStreaming");
                if (IsStreaming.Value == "1")
                {
                    TimeSpan Watchtime = DateTime.Now - User.GotOnline;
                    User.Watchtime += (int)Watchtime.TotalMinutes;
                }
                context.Update(User);
                context.SaveChanges();
            }
        }

        internal static void SendMessage(string Channel, int Priority, bool LogMessage, string Message)
        {
            if (LogMessage)
                Bot.LogMessage(false, Channel, "PyRZyBot", Message);

            using (var context = new Database())
            {
                int CurrentFeedbackLevel = int.Parse(context.ChannelInfo.FirstOrDefault(x => x.Channel == Channel && x.Info == "ChatFeedback").Value);
                if (CurrentFeedbackLevel + Priority < 2)
                    return;
            }

            client.SendMessage(Channel, Message);
            Database.CountMessage("PyRZyBot", Channel);
        }
        internal static void LogMessage(bool CountMessage, string Channel, string Name, string Message)
        {
            ConsoleColor ChannelColor;
            Message = Regex.Replace(Message, "[^ a-zą-żóA-ZĄ-ŻÓ0-9!-~]", "?", RegexOptions.Compiled);

            using (var context = new Database())
            {
                var Info = context.ChannelInfo.FirstOrDefault(x => x.Channel == "pyrzybot" && x.Info == "Color");
                string BotColor = Info.Value;

                switch (Channel)
                {
                    case "kyrzy":
                        ChannelColor = ConsoleColor.Blue;
                        if (BotColor != "Blue")
                        {
                            client.SendMessage(Channel, "/color dodgerblue");
                            Info.Value = "Blue";
                            context.Update(Info);
                            context.SaveChanges();
                        }
                        break;
                    case "ananieana":
                        ChannelColor = ConsoleColor.Red;
                        if (BotColor != "Pink")
                        {
                            client.SendMessage(Channel, "/color hotpink");
                            Info.Value = "Pink";
                            context.Update(Info);
                            context.SaveChanges();
                        }
                        break;
                    default:
                        ChannelColor = ConsoleColor.White;
                        break;
                }
            }
            if (CountMessage)
                Database.CountMessage(Name, Channel);

            Console.Write(Program.TimeNow());
            Console.ForegroundColor = ChannelColor;
            Console.Write(" <");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(Name);
            Console.ForegroundColor = ChannelColor;
            Console.Write("> ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(Message);
        }
        internal static void LogEvent(string Channel, int Priority, string Event)
        {
            int CurrentFeedbackLevel;
            ConsoleColor ChannelColor;
            Event = Regex.Replace(Event, "[^ a-zą-żóA-ZĄ-ŻÓ!-~]", "?", RegexOptions.Compiled);

            using (var context = new Database())
                CurrentFeedbackLevel = int.Parse(context.ChannelInfo.FirstOrDefault(x => x.Channel == "pyrzybot" && x.Info == "ConsoleFeedback").Value);

            if (CurrentFeedbackLevel + Priority < 2)
                return;

            switch (Channel)
            {
                case "kyrzy":
                    ChannelColor = ConsoleColor.Blue;
                    break;
                case "ananieana":
                    ChannelColor = ConsoleColor.Red;
                    break;
                default:
                    ChannelColor = ConsoleColor.White;
                    break;
            }
            Console.Write(Program.TimeNow());
            Console.ForegroundColor = ChannelColor;
            Console.Write(" [");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(Event);
            Console.ForegroundColor = ChannelColor;
            Console.WriteLine("] ");
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
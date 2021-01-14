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
using TwitchLib.Api;
using System.Threading.Tasks;
using TwitchLib.PubSub;
using TwitchLib.Api.Services;

namespace PyRZyBot
{
    internal class Bot
    {
        TwitchClient client;
        TwitchPubSub client_pubsub;
        List<int> Weights;
        List<string> Responces;
        List<string> Grafik;
        Dictionary<string, string> Mistakes;
        Dictionary<string, string> simpleCommands;

        Dictionary<string, ChatUser> ChatUsers;

        char[] digits = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.' };
        private List<string> Dzien = new List<string>()
        {
            {"Poniedziałek"},
            {"Wtorek"},
            {"Środa"},
            {"Czwartek"},
            {"Piątek"},
            {"Sobota"},
            {"Niedziela"},
        };

        public static Timer RewardCd = new Timer(1000);
        private Timer DSCTimer;
        private Timer CzyTimer;
        DateTime LSM = DateTime.Now;

        private static TwitchAPI API;
        private LiveStreamMonitorService Monitor;

        int WyzywającyWin = 0;
        int WyzwanyWin = 0;
        string Game;
        string Title;
        string NextGame;
        string NextTitle;
        string LastGame = null;
        string LastTitle = null;

        internal void Connect(bool isLogging)
        {
            var json = string.Empty;
            using (var sr = new StreamReader(Environment.CurrentDirectory + @"\path.json"))
            {
                json = sr.ReadToEnd();
            }
            if (!string.IsNullOrEmpty(json))
            {
                var savefile = JsonConvert.DeserializeObject<SaveFileTemplate>(json);
                ChatUsers = savefile.ChatUsers;
                Responces = savefile.Responces;
                Weights = savefile.Weights;
                simpleCommands = savefile.simpleCommands;
                Grafik = savefile.Grafik;
                Mistakes = savefile.Mistakes;
                NextGame = savefile.NextGame;
                NextTitle = savefile.NextTitle;
            }

            ConnectionCredentials credentials = new ConnectionCredentials(TwitchInfo.BotName, TwitchInfo.BotToken);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 7,
                ThrottlingPeriod = TimeSpan.FromSeconds(3)
            };
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            client = new TwitchClient(customClient);
            client.Initialize(credentials, TwitchInfo.ChannelName);

            if (isLogging)
                client.OnLog += Client_OnLog;

            client.OnChatCommandReceived += Client_OnChatCommandReceived;
            client.OnMessageReceived += Client_OnMessageReceived;

            API = new TwitchAPI();
            API.Settings.ClientId = TwitchInfo.ClientID;
            API.Settings.AccessToken = TwitchInfo.AccessToken;
            var ChannelInfo = API.V5.Channels.GetChannelAsync(API.Settings.AccessToken);
            Game = ChannelInfo.Result.Game;
            Title = ChannelInfo.Result.Status;

            Monitor = new LiveStreamMonitorService(API, 60);
            List<string> Lista = new List<string> { TwitchInfo.ChannelID };
            Monitor.SetChannelsById(Lista);
            Monitor.OnStreamOnline += Monitor_OnStreamOnline;
            Monitor.OnStreamOffline += Monitor_OnStreamOffline;

            client_pubsub = new TwitchPubSub();
            client_pubsub.OnPubSubServiceConnected += Client_pubsub_OnPubSubServiceConnected;
            client_pubsub.ListenToRewards(TwitchInfo.ChannelID);
            client_pubsub.OnRewardRedeemed += Client_pubsub_OnRewardRedeemed;

            client_pubsub.Connect();
            client.Connect();
            Monitor.Start();
        }
        private void Monitor_OnStreamOnline(object sender, TwitchLib.Api.Services.Events.LiveStreamMonitor.OnStreamOnlineArgs e)
        {
            Console.WriteLine("------------Timers On------------");
            SetTimer();
        }
        private void Monitor_OnStreamOffline(object sender, TwitchLib.Api.Services.Events.LiveStreamMonitor.OnStreamOfflineArgs e)
        {
            Console.WriteLine("------------Timers Off------------");
            DSCTimer.Stop();
            DSCTimer.Dispose();
            Console.WriteLine($"Zwycięstwa Wyzywający/Wyzwani - {WyzywającyWin}/{WyzwanyWin}");
        }

        private void RewardMessage(Object source, ElapsedEventArgs e)
        {
            string Message = ChatUser.CheckPendingRewardMessages(ChatUsers);
            if (!string.IsNullOrEmpty(Message))
            {
                client.SendMessage(TwitchInfo.ChannelName, Message);
            }
        }

        private void Client_pubsub_OnRewardRedeemed(object sender, TwitchLib.PubSub.Events.OnRewardRedeemedArgs e)
        {
            var IdName = e.DisplayName.ToLower();
            if (!ChatUsers.ContainsKey(IdName))
            {
                var ChatUser = new ChatUser(IdName, e.DisplayName);
                ChatUsers.Add(IdName, ChatUser);
            }

            var Nazwa = e.RewardTitle;
            var Koszt = e.RewardCost;

            if (Nazwa.ToLower().Contains("pyr"))
            {
                ChatUsers[IdName].Pyry += Koszt;
                Console.WriteLine($"{IdName} +{Koszt}");
                ChatUser.AddPendingRewardMessage(IdName, Koszt);

                if (!RewardCd.Enabled)
                {
                    RewardCd.Enabled = true; ;
                    RewardCd.AutoReset = true;
                    RewardCd.Elapsed += RewardMessage;
                }
                return;
            }
        }

        private void Client_OnChatCommandReceived(object sender, OnChatCommandReceivedArgs e)
        {
            var IdName = e.Command.ChatMessage.Username;

            if (simpleCommands.ContainsKey(e.Command.CommandText.ToLower()))
            {
                client.SendMessage(TwitchInfo.ChannelName, simpleCommands[e.Command.CommandText.ToLower()]);
                return;
            }

            string AtIdName = null;
            string Message = null;

            switch (e.Command.CommandText.ToLower())
            {
                case "pyry":
                    {
                        if (e.Command.ArgumentsAsList.Count() == 3)
                        {
                            switch (e.Command.ArgumentsAsList[0])
                            {
                                case "gib":
                                case "daj":
                                case "give":
                                    {
                                        AtIdName = e.Command.ArgumentsAsList[1].ToLower().Replace("@", "");
                                        if (!ChatUsers.ContainsKey(AtIdName))
                                        {
                                            client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName}, użytkownika {AtIdName} nie ma w systemie!");
                                            return;
                                        }
                                        if (!Helper.IsNumeric(e.Command.ArgumentsAsList[2]) || Convert.ToInt32(e.Command.ArgumentsAsList[2]) < 1)
                                        {
                                            client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName}, musisz podać poprawną Liczbę Pyr!");
                                            return;
                                        }
                                        int Pyry = Convert.ToInt32(e.Command.ArgumentsAsList[2]);

                                        if (ChatUsers[IdName].Pyry < Pyry)
                                        {
                                            client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName}, masz za mało Pyr!");
                                            return;
                                        }
                                        if (IdName == AtIdName)
                                        {
                                            client.SendMessage(TwitchInfo.ChannelName, $"Gratulacje {ChatUsers[IdName].DisplayedName}! Dałeś Sobie {Pyry} {Helper.EndingPyry(Pyry)}!");
                                            return;
                                        }

                                        ChatUsers[AtIdName].Pyry += Pyry;
                                        ChatUsers[IdName].Pyry -= Pyry;
                                        client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[AtIdName].DisplayedName}, {ChatUsers[IdName].DisplayedName} {Helper.EndingOther(IdName, "dał")} Ci {Pyry} {Helper.EndingPyry(Pyry)}!");
                                    }
                                    return;
                                case "add":
                                    {
                                        if (!ChatUsers[IdName].IsModerator) { return; }

                                        AtIdName = e.Command.ArgumentsAsList[1].ToLower().Replace("@", "");
                                        if (!ChatUsers.ContainsKey(AtIdName))
                                        {
                                            client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName}, użytkownika {AtIdName} nie ma w systemie!");
                                            return;
                                        }
                                        if (!Helper.IsNumeric(e.Command.ArgumentsAsList[2]))
                                        {
                                            client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName}, musisz podać poprawną Liczbę Pyr!");
                                            return;
                                        }
                                        ChatUsers[AtIdName].Pyry += Convert.ToInt32(e.Command.ArgumentsAsList[2]);
                                        client.SendMessage(TwitchInfo.ChannelName, $"{ChatUsers[AtIdName].DisplayedName} ma teraz {ChatUsers[AtIdName].Pyry} {Helper.EndingPyry(ChatUsers[IdName].Pyry)}!");
                                    }
                                    return;
                                case "delete":
                                case "remove":
                                    {
                                        if (!ChatUsers[IdName].IsModerator) { return; }

                                        AtIdName = e.Command.ArgumentsAsList[1].ToLower().Replace("@", "");
                                        if (!ChatUsers.ContainsKey(AtIdName))
                                        {
                                            client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName}, użytkownika {AtIdName} nie ma w systemie!");
                                            return;
                                        }
                                        if (!Helper.IsNumeric(e.Command.ArgumentsAsList[2]))
                                        {
                                            client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName}, musisz podać poprawną liczbę Pyr!");
                                            return;
                                        }
                                        ChatUsers[AtIdName].Pyry -= Convert.ToInt32(e.Command.ArgumentsAsList[2]);

                                        if (ChatUsers[AtIdName].Pyry < 0)
                                            ChatUsers[AtIdName].Pyry = 0;

                                        client.SendMessage(TwitchInfo.ChannelName, $"{ChatUsers[AtIdName].DisplayedName} ma teraz {ChatUsers[AtIdName].Pyry} {Helper.EndingPyry(ChatUsers[IdName].Pyry)}!");
                                    }
                                    return;
                            }
                        }
                        var Ranking = ChatUsers.OrderByDescending(x => x.Value.Pyry).Select(x => x.Value.IdName).ToList();

                        if (e.Command.ArgumentsAsList.Count() == 1)
                        {
                            AtIdName = e.Command.ArgumentsAsList[0].Replace("@", "").ToLower();
                            if (!ChatUsers.ContainsKey(AtIdName))
                            {
                                client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName}, użytkownika {AtIdName} nie ma w systemie!");
                                return;
                            }
                            int Rank = Ranking.IndexOf(AtIdName);
                            client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName}, {ChatUsers[AtIdName].DisplayedName} ma {ChatUsers[AtIdName].Pyry} {Helper.EndingPyry(ChatUsers[AtIdName].Pyry)} i jest na {++Rank}. miejscu!");
                        }
                        else
                        {
                            int Rank = Ranking.IndexOf(IdName);
                            client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName}, masz {ChatUsers[IdName].Pyry} {Helper.EndingPyry(ChatUsers[IdName].Pyry)} i jesteś na {++Rank}. miejscu!");
                        }
                    }
                    return;
                case "duel":
                case "walcz":
                case "wyzwij":
                    {
                        if (ChatUsers[IdName].IsBanned) { return; }

                        if (e.Command.ArgumentsAsList[0] == "info" || e.Command.ArgumentsAsList[0] == "i")
                        {
                            if (string.IsNullOrEmpty(ChatUsers[IdName].Wyzwania))
                            {
                                client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName}, nie jesteś {Helper.EndingOther(IdName, "zapisany")} na żadną walkę!");
                                return;
                            }
                            if (ChatUsers[IdName].Wyzwania.StartsWith("."))
                            {
                                client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName}, {Helper.EndingOther(IdName, "wyzwałeś")} {ChatUsers[ChatUsers[IdName].Wyzwania.Substring(1)].DisplayedName} na walkę o {ChatUsers[IdName].Stawka} {Helper.EndingPyry(ChatUsers[IdName].Stawka)}!");
                                return;
                            }
                            client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName}, {ChatUsers[ChatUsers[IdName].Wyzwania].DisplayedName} {Helper.EndingOther(IdName, "wyzwał")} Cię na walkę o {ChatUsers[IdName].Stawka} {Helper.EndingPyry(ChatUsers[IdName].Stawka)}!");
                            return;
                        }

                        if (ChatUser.SinceLastFight(ChatUsers, IdName) < Helper.FightCooldown)
                        {
                            client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName}, musisz odpocząć po ostatniej walce! Jeszcze {Helper.FightCooldown - ChatUser.SinceLastFight(ChatUsers, IdName) + 10}s");
                            return;
                        }

                        if (e.Command.ArgumentsAsList.Count() == 0)
                        {
                            client.SendMessage(TwitchInfo.ChannelName, "Poprawny zapis: !wyzwij (Użytkownik) (kwota zakładu)");
                            return;
                        }

                        AtIdName = e.Command.ArgumentsAsList[0].ToLower().Replace("@", "");
                        int Stawka = 0;

                        if (AtIdName == IdName)
                        {
                            client.SendMessage(TwitchInfo.ChannelName, "...");
                            return;
                        }
                        if (!ChatUsers.ContainsKey(AtIdName))
                        {
                            client.SendMessage(TwitchInfo.ChannelName, $"Użytkownika {AtIdName} nie ma w systemie!");
                            return;
                        }
                        if (ChatUsers[AtIdName].IsBanned)
                        {
                            client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName}, {ChatUsers[AtIdName].DisplayedName} jest zbanowany xD");
                            return;
                        }
                        if (!string.IsNullOrEmpty(ChatUsers[IdName].Wyzwania))
                        {
                            client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName}, już jesteś {Helper.EndingOther(IdName, "zapisany")} na walkę!");
                            return;
                        }
                        if (!string.IsNullOrEmpty(ChatUsers[AtIdName].Wyzwania))
                        {
                            if (ChatUsers[AtIdName].Wyzwania == IdName)
                            {
                                client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName}, już jesteś {Helper.EndingOther(IdName, "zapisany")} na walkę z {ChatUsers[AtIdName].DisplayedName}!");
                                return;
                            }
                            client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName}, {ChatUsers[AtIdName].DisplayedName} jest {Helper.EndingOther(AtIdName, "zapisany")} na inną walkę!");
                            return;
                        }
                        if (e.Command.ArgumentsAsList.Count() == 2)
                        {
                            if (!Helper.IsNumeric(e.Command.ArgumentsAsList[1]) || Convert.ToInt32(e.Command.ArgumentsAsList[1]) < 0)
                            {
                                client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName}, musisz podać poprawną kwotę zakładu!");
                                return;
                            }
                            Stawka = Convert.ToInt32(e.Command.ArgumentsAsList[1]);
                        }

                        if (ChatUsers[IdName].Pyry < Stawka)
                        {
                            client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName}, nie masz wystarczająco pyr!");
                            return;
                        }
                        if (ChatUsers[AtIdName].Pyry < Stawka)
                        {
                            client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName}, {ChatUsers[AtIdName].DisplayedName} nie ma wystarczająco pyr!");
                            return;
                        }
                        ChatUsers[AtIdName].Wyzwania = IdName;
                        ChatUsers[IdName].Wyzwania = $".{AtIdName}";
                        ChatUsers[AtIdName].Stawka = Stawka;
                        ChatUsers[IdName].Stawka = Stawka;

                        Message = $"@{ChatUsers[AtIdName].DisplayedName}, {ChatUsers[IdName].DisplayedName} {Helper.EndingOther(IdName, "wyzwał")} Cię na ";

                        if (Stawka == 0)
                        {
                            Message += "towarzyski pojedynek! ";
                        }
                        else
                        {
                            Message += $"pojedynek o {Stawka} {Helper.EndingPyry(Stawka)}! ";
                            if (Stawka == 69) { Message += "Nice! "; }
                        }
                        Message += "Czy podejmiesz się walki? Użyj !tak albo !nie GivePLZ SirSword SirShield TakeNRG";

                        client.SendMessage(TwitchInfo.ChannelName, Message);
                    }
                    return;
                case "y":
                case "tak":
                case "accept":
                case "akceptuj":
                    {
                        if (ChatUsers[IdName].IsBanned) { return; }
                        if (string.IsNullOrEmpty(ChatUsers[IdName].Wyzwania))
                        {
                            client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName}, nie {Helper.EndingOther(IdName, "byłeś")} {Helper.EndingOther(IdName, "wyzwany")} na żaden pojedynek!");
                            return;
                        }
                        if (ChatUsers[IdName].Wyzwania.StartsWith("."))
                        {
                            client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName}, nie możesz rozpocząć tego pojedynku!");
                            return;
                        }
                        string Wygrany = ChatUsers[IdName].Wyzwania;
                        string Przegrany = IdName;
                        int Stawka = ChatUsers[IdName].Stawka;

                        Random Random = new Random();
                        int RandomValue = Random.Next(1, 3);

                        if (RandomValue == 1)
                        {
                            Wygrany = IdName;
                            Przegrany = ChatUsers[IdName].Wyzwania;
                            WyzwanyWin++;
                        }
                        else { WyzywającyWin++; }

                        ChatUsers[Wygrany].Pyry += Stawka;
                        ChatUsers[Przegrany].Pyry -= Stawka;

                        ChatUser.Streaks(Wygrany, Przegrany, ChatUsers);

                        if (Stawka != 0)
                        {
                            Message = $"{ChatUsers[Wygrany].DisplayedName} {Helper.EndingOther(Wygrany, "wygrał")} pojedynek z {ChatUsers[Przegrany].DisplayedName} o {Stawka} {Helper.EndingPyry(Stawka)}!";
                            if (Stawka == 69) { Message += " Nice!"; }
                        }
                        else
                        {
                            Message = $"{ChatUsers[Wygrany].DisplayedName} {Helper.EndingOther(Wygrany, "wygrał")} towarzyski pojedynek z {ChatUsers[Przegrany].DisplayedName}!";
                        }

                        if (RandomValue == 1)
                        {
                            Message += " SirShield TakeNRG";
                        }
                        else
                        {
                            Message += " GivePLZ SirSword";
                        }

                        if (ChatUsers[Wygrany].Streak > 2 && Stawka >= 100)
                        {
                            Message += $" {ChatUsers[Wygrany].DisplayedName} {Helper.EndingOther(Wygrany, "wygrał")} {ChatUsers[Wygrany].Streak} razy z rzędu!";
                        }
                        else if (ChatUsers[Przegrany].Streak < -2)
                        {
                            Message += $" {ChatUsers[Wygrany].DisplayedName} {Helper.EndingOther(Wygrany, "przegrał")} {ChatUsers[Wygrany].Streak * -1} razy z rzędu!";
                        }

                        ChatUsers[ChatUsers[IdName].Wyzwania].Cooldown = DateTime.Now;
                        ChatUsers[Wygrany].Stawka = 0;
                        ChatUsers[Przegrany].Stawka = 0;
                        ChatUsers[Wygrany].Wyzwania = "";
                        ChatUsers[Przegrany].Wyzwania = "";

                        client.SendMessage(TwitchInfo.ChannelName, Message);
                    }
                    return;
                case "n":
                case "nie":
                case "odrzuć":
                case "odrzuc":
                case "cancel":
                case "deny":
                    {
                        if (ChatUsers[IdName].IsBanned) { return; }
                        if (string.IsNullOrEmpty(ChatUsers[IdName].Wyzwania))
                        {
                            client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName}, nie {Helper.EndingOther(IdName, "byłeś")} {Helper.EndingOther(IdName, "wyzwany")} na żaden pojedynek!");
                            return;
                        }
                        string Wyzwany = "";
                        string Wyzywający = "";

                        if (ChatUsers[IdName].Wyzwania.StartsWith("."))
                        {
                            Wyzywający = IdName;
                            Wyzwany = ChatUsers[IdName].Wyzwania.Substring(1);
                            Message = $"@{ChatUsers[Wyzwany].DisplayedName}, {ChatUsers[Wyzywający].DisplayedName} {Helper.EndingOther(Wyzywający, "anulował")} pojedynek!";
                        }
                        else
                        {
                            Wyzywający = ChatUsers[IdName].Wyzwania;
                            Wyzwany = IdName;
                            Message = $"@{ChatUsers[Wyzywający].DisplayedName}, {ChatUsers[Wyzwany].DisplayedName} nie {Helper.EndingOther(Wyzwany, "podjął")} się walki!";

                            if (ChatUsers[Wyzwany].Stawka >= 100 && ChatUsers[Wyzwany].Streak > 0)
                            {
                                if (ChatUsers[Wyzwany].Streak > ChatUsers[Wyzwany].MaxWinStreak)
                                {
                                    ChatUsers[Wyzwany].MaxWinStreak = ChatUsers[Wyzwany].Streak;
                                }
                                ChatUsers[Wyzwany].Streak = 0;
                            }
                        }
                        ChatUsers[Wyzwany].Stawka = 0;
                        ChatUsers[Wyzywający].Stawka = 0;
                        ChatUsers[Wyzwany].Wyzwania = "";
                        ChatUsers[Wyzywający].Wyzwania = "";
                        client.SendMessage(TwitchInfo.ChannelName, Message);
                    }
                    return;
                case "stats":
                    {

                        double Winrate = 100;
                        if (e.Command.ArgumentsAsList.Count() == 1)
                        {
                            AtIdName = e.Command.ArgumentsAsList[0].ToLower().Replace("@", "");
                            if (!ChatUsers.ContainsKey(AtIdName))
                            {
                                client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName}, użytkownika {AtIdName} nie ma w systemie!");
                                return;
                            }
                            Winrate *= ChatUsers[AtIdName].DuelsWon;
                            Winrate /= ChatUsers[AtIdName].DuelsPlayed;
                            Winrate = Math.Round(Winrate, 2);
                            client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName}, {ChatUsers[AtIdName].DisplayedName}: Zwycięstwa: {ChatUsers[AtIdName].DuelsWon} | Rozegrane walki: {ChatUsers[AtIdName].DuelsPlayed} | Winrate: {Winrate}% | Max win/lose streak: {ChatUsers[AtIdName].MaxWinStreak}/{ChatUsers[AtIdName].MaxLoseStreak * -1} | Obecny streak: {ChatUsers[AtIdName].Streak}");
                            return;
                        }
                        Winrate *= ChatUsers[IdName].DuelsWon;
                        Winrate /= ChatUsers[IdName].DuelsPlayed;
                        Winrate = Math.Round(Winrate, 2);
                        client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName}: Zwycięstwa: {ChatUsers[IdName].DuelsWon} | Rozegrane walki: {ChatUsers[IdName].DuelsPlayed} | Winrate: {Winrate}% | Max win/lose streak: {ChatUsers[IdName].MaxWinStreak}/{ChatUsers[IdName].MaxLoseStreak * -1} | Obecny streak: {ChatUsers[IdName].Streak}");
                    }
                    return;
                case "ranking":
                case "leaderboard":
                    {
                        var Ranking = ChatUsers.OrderByDescending(x => x.Value.Pyry).Select(x => x.Value.IdName).ToList();
                        int Rank = Ranking.IndexOf(IdName);
                        List<string> RankMessage = new List<string>();
                        int Nr = 0;
                        foreach (var Key in Ranking)
                        {
                            if (Nr >= 5) { break; }
                            Nr++;
                            RankMessage.Add($"#{Nr} {ChatUsers[Key].DisplayedName}-{ChatUsers[Key].Pyry}");
                        }
                        client.SendMessage(TwitchInfo.ChannelName, string.Join(" | ", RankMessage));
                    }
                    return;
                case "commands":
                    {
                        Message = "Dostępne komendy: !grafik !walcz !pyry";
                        foreach (string Key in simpleCommands.Keys)
                            Message += $"!{Key} ";

                        if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster)
                            Message += "!title !game !next !ban !unban !command add/delete";

                        client.SendMessage(TwitchInfo.ChannelName, Message);
                    }
                    return;
                case "grafik":
                    {
                        if (ChatUsers[IdName].IsModerator && e.Command.ArgumentsAsList.Count() != 0)
                        {
                            try
                            {
                                if (e.Command.ArgumentsAsList[0].ToLower() == "c")
                                {
                                    Grafik.Clear();
                                    Grafik.Add("Grafik jest w przygotowaniu.");
                                    client.SendMessage(TwitchInfo.ChannelName, "Wyczyszczono grafik!");
                                    return;
                                }
                                int L_Streamow = 3, j = -2;

                                if (Helper.IsNumeric(e.Command.ArgumentsAsList[0]))
                                    L_Streamow = e.Command.ArgumentsAsList[0].Count();

                                List<string> Gry = e.Command.ArgumentsAsString.Split(";").ToList();
                                List<string> Test_Grafik = new List<string>();
                                Gry[0] = Gry[0].TrimStart(digits);

                                if (Gry.Count() != L_Streamow)
                                {
                                    client.SendMessage(TwitchInfo.ChannelName, "Niezgodna liczba dni i gier!");
                                    return;
                                }
                                string Gra = "";
                                for (int i = 1; i <= L_Streamow; i++)
                                {
                                    if (Helper.IsNumeric(e.Command.ArgumentsAsList[0]))
                                    {
                                        j = Convert.ToInt32(e.Command.ArgumentsAsString.Substring(i - 1, 1)) - 1;
                                        if (j < 0 || j > 6)
                                        {
                                            client.SendMessage(TwitchInfo.ChannelName, "Przy podawaniu dni, liczby muszą być w zakresie 1-7!");
                                            return;
                                        }
                                    }
                                    else { j += 2; }

                                    Gra = Gry[i - 1].Trim();
                                    Test_Grafik.Add($"{Dzien[j]}: {Gra}");
                                }
                                Grafik.Clear();
                                Grafik = Test_Grafik;
                                client.SendMessage(TwitchInfo.ChannelName, "Zaktualizowano grafik!");
                            }
                            catch (Exception err)
                            {
                                Console.WriteLine(err.Message);
                                client.SendMessage(TwitchInfo.ChannelName, "Coś poszło nie tak :/");
                            }
                        }
                        else
                        {
                            for (int i = 0; i < Grafik.Count(); i++)
                                client.SendMessage(TwitchInfo.ChannelName, Grafik[i]);
                        }
                    }
                    return;
                case "game":
                    {
                        if (ChatUsers[IdName].IsModerator && e.Command.ArgumentsAsList.Count() != 0)
                        {
                            try
                            {
                                Game = e.Command.ArgumentsAsString;
                                var t = Task.Run(async () => { await API.V5.Channels.UpdateChannelAsync(TwitchInfo.ChannelID, Title, Game); });
                                t.Wait();
                                client.SendMessage(TwitchInfo.ChannelName, $"Zmieniono grę na: {Game}");
                                return;
                            }
                            catch (Exception err)
                            {
                                Console.WriteLine(err.Message);
                                client.SendMessage(TwitchInfo.ChannelName, "Coś poszło nie tak :/");
                                return;
                            }
                        }
                        client.SendMessage(TwitchInfo.ChannelName, $"Obecna gra: {Game}");
                    }
                    return; ;
                case "title":
                    {
                        if (ChatUsers[IdName].IsModerator && e.Command.ArgumentsAsList.Count() != 0)
                        {
                            try
                            {
                                Title = e.Command.ArgumentsAsString;
                                var t = Task.Run(async () => { await API.V5.Channels.UpdateChannelAsync(TwitchInfo.ChannelID, Title, Game); });
                                t.Wait();
                                client.SendMessage(TwitchInfo.ChannelName, $"Zmieniono tytuł na: {Title}");
                                return;
                            }
                            catch (Exception err)
                            {
                                Console.WriteLine(err.Message);
                                client.SendMessage(TwitchInfo.ChannelName, "Twitch nie lubi tego tytułu :frowning:");
                                return;
                            }
                        }
                        client.SendMessage(TwitchInfo.ChannelName, $"Obecny tytuł: {Title}");
                    }
                    break;
            }

            if (!ChatUsers[IdName].IsModerator) { return; }

            switch (e.Command.CommandText.ToLower())
            {
                case "błąd":

                    List<string> Mistake = e.Command.ArgumentsAsString.ToLower().Split(";").ToList();
                    Mistakes.Add(Mistake[0], Mistake[1]);
                    client.SendMessage(TwitchInfo.ChannelName, "Dodano błąd");

                    break;
                case "ban":

                    if (e.Command.ArgumentsAsList.Count != 0)
                    {
                        AtIdName = e.Command.ArgumentsAsList[0].ToLower().Replace("@", "");
                        if (!ChatUsers.ContainsKey(AtIdName))
                        {
                            client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName}, użytkownika {AtIdName} nie ma w systemie!");
                            return;
                        }
                        if (ChatUsers[AtIdName].IsModerator)
                        {
                            client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName}, nie można zbanować moderatora!");
                            return;
                        }
                        if (!ChatUsers[AtIdName].IsBanned)
                        {
                            ChatUsers[AtIdName].IsBanned = true;
                            client.SendMessage(TwitchInfo.ChannelName, $"Zbanowano {ChatUsers[AtIdName].DisplayedName} :partying:");
                            return;
                        }
                        client.SendMessage(TwitchInfo.ChannelName, $"{AtIdName} już jest zbanowany!");
                        return;
                    }
                    client.SendMessage(TwitchInfo.ChannelName, "Poprawny zapis: !ban (Użytkownik)");

                    break;
                case "unban":

                    if (e.Command.ArgumentsAsList.Count != 0)
                    {
                        AtIdName = e.Command.ArgumentsAsList[0].ToLower().Replace("@", "");
                        if (!ChatUsers.ContainsKey(AtIdName))
                        {
                            client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName}, użytkownika {AtIdName} nie ma w systemie!");
                            return;
                        }
                        if (ChatUsers[AtIdName].IsBanned)
                        {
                            ChatUsers[AtIdName].IsBanned = false;
                            client.SendMessage(TwitchInfo.ChannelName, $"Odbanowano {ChatUsers[AtIdName].DisplayedName} :frowning:");
                            return;
                        }
                        client.SendMessage(TwitchInfo.ChannelName, $"{ChatUsers[AtIdName].DisplayedName} nie jest zbanowany!");
                        return;
                    }
                    client.SendMessage(TwitchInfo.ChannelName, "Poprawny zapis: !unban (Użytkownik)");

                    break;
                case "command":
                    {
                        if (e.Command.ArgumentsAsList.Count == 0)
                        {
                            client.SendMessage(TwitchInfo.ChannelName, "Poprawny zapis: !command add (Wywołanie) (Treść)");
                            client.SendMessage(TwitchInfo.ChannelName, "Poprawny zapis: !command delete (Wywołanie)");
                            return;
                        }
                        switch (e.Command.ArgumentsAsList[0])
                        {
                            case "add":

                                if (e.Command.ArgumentsAsList.Count < 3)
                                {
                                    client.SendMessage(TwitchInfo.ChannelName, "Poprawny zapis: !command add (Wywołanie) (Treść)");
                                    return;
                                }
                                if (simpleCommands.ContainsKey(e.Command.ArgumentsAsList[1].ToLower()))
                                {
                                    client.SendMessage(TwitchInfo.ChannelName, "Taka komenda już istnieje!");
                                    return;
                                }
                                simpleCommands.Add(e.Command.ArgumentsAsList[1].ToLower(), e.Command.ArgumentsAsString.Substring(4 + e.Command.ArgumentsAsList[1].Length));
                                client.SendMessage(TwitchInfo.ChannelName, $"Komenda !{e.Command.ArgumentsAsList[1]} została dodana.");

                                break;
                            case "delete":

                                if (e.Command.ArgumentsAsList.Count != 2)
                                {
                                    client.SendMessage(TwitchInfo.ChannelName, "Poprawny zapis: !command delete (Wywołanie)");
                                    return;
                                }
                                if (!simpleCommands.ContainsKey(e.Command.ArgumentsAsList[1].ToLower()))
                                {
                                    client.SendMessage(TwitchInfo.ChannelName, "Taka komenda nie istnieje!");
                                    return;
                                }
                                simpleCommands.Remove(e.Command.ArgumentsAsList[1].ToLower());
                                client.SendMessage(TwitchInfo.ChannelName, $"Komenda !{e.Command.ArgumentsAsList[1]} została usunięta.");

                                break;
                            default:

                                client.SendMessage(TwitchInfo.ChannelName, "Poprawny zapis: !command add (Wywołanie) (Treść)");
                                client.SendMessage(TwitchInfo.ChannelName, "Poprawny zapis: !command delete (Wywołanie)");
                                return;
                        }
                    }
                    return;
                case "next":
                    {
                        if (e.Command.ArgumentsAsList.Count() == 0)
                        {
                            if (String.IsNullOrEmpty(NextTitle) && String.IsNullOrEmpty(NextGame))
                            {
                                client.SendMessage(TwitchInfo.ChannelName, $"Nie ustawiono następnego tytułu i gry!");
                                return;
                            }
                            if (String.IsNullOrEmpty(NextTitle))
                            {
                                client.SendMessage(TwitchInfo.ChannelName, $"Nie ustawiono następnego tytułu!");
                                return;
                            }
                            if (String.IsNullOrEmpty(NextGame))
                            {
                                client.SendMessage(TwitchInfo.ChannelName, $"Nie ustawiono następnej gry!");
                                return;
                            }
                            try
                            {
                                var ChannelInfo = API.V5.Channels.GetChannelAsync(API.Settings.AccessToken);
                                LastTitle = ChannelInfo.Result.Status;
                                LastGame = ChannelInfo.Result.Game;
                                var t = Task.Run(async () => { await API.V5.Channels.UpdateChannelAsync(TwitchInfo.ChannelID, NextTitle, NextGame); });
                                t.Wait();
                                client.SendMessage(TwitchInfo.ChannelName, $"Zmieniono tytuł na: {NextTitle}");
                                client.SendMessage(TwitchInfo.ChannelName, $"Zmieniono grę na: {NextGame}");

                                if (NextGame.ToLower() == "drawful 2" && simpleCommands.ContainsKey("jackbox"))
                                    client.SendMessage(TwitchInfo.ChannelName, simpleCommands["jackbox"]);

                                Title = NextTitle;
                                NextTitle = null;
                                Game = NextGame;
                                NextGame = null;
                                return;
                            }
                            catch (Exception err)
                            {
                                Console.WriteLine(err.Message);
                                client.SendMessage(TwitchInfo.ChannelName, "Twitch nie lubi tego tytułu :frowning:");
                            }
                        }

                        switch (e.Command.ArgumentsAsList[0].ToLower())
                        {
                            case "c":

                                NextTitle = null;
                                NextGame = null;
                                client.SendMessage(TwitchInfo.ChannelName, $"Usunięto następne ustawienia!");

                                break;
                            case "title":

                                if (e.Command.ArgumentsAsList.Count() == 1)
                                {
                                    if (String.IsNullOrEmpty(NextTitle))
                                    {
                                        client.SendMessage(TwitchInfo.ChannelName, $"Nie ustawiono następnego tytułu!");
                                        return;
                                    }
                                    client.SendMessage(TwitchInfo.ChannelName, $"Następny tytuł: {NextTitle}");
                                    return;
                                }
                                NextTitle = e.Command.ArgumentsAsString.Substring(6);
                                client.SendMessage(TwitchInfo.ChannelName, $"Ustawiono następny tytuł na: {NextTitle}");

                                break;
                            case "game":

                                if (e.Command.ArgumentsAsList.Count() == 1)
                                {
                                    if (String.IsNullOrEmpty(NextGame))
                                    {
                                        client.SendMessage(TwitchInfo.ChannelName, $"Nie ustawiono następnej gry!");
                                        return;
                                    }
                                    client.SendMessage(TwitchInfo.ChannelName, $"Następna gra: {NextGame}");
                                    return;
                                }
                                NextGame = e.Command.ArgumentsAsString.Substring(5);
                                client.SendMessage(TwitchInfo.ChannelName, $"Ustawiono następną grę na: {NextGame}");

                                break;
                        }
                    }
                    break;
                case "previous":
                    {
                        if (String.IsNullOrEmpty(LastTitle))
                        {
                            client.SendMessage(TwitchInfo.ChannelName, $"Brak ostatnich ustawień!");
                            return;
                        }
                        var ChannelInfo = API.V5.Channels.GetChannelAsync(API.Settings.AccessToken);
                        NextTitle = ChannelInfo.Result.Status;
                        NextGame = ChannelInfo.Result.Game;
                        var t = Task.Run(async () => { await API.V5.Channels.UpdateChannelAsync(TwitchInfo.ChannelID, LastTitle, LastGame); });
                        t.Wait();
                        client.SendMessage(TwitchInfo.ChannelName, $"Cofnięto do poprzednich ustawień!");
                        Title = LastTitle;
                        LastTitle = null;
                        Game = LastGame;
                        LastGame = null;
                    }
                    break;
            }
        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            var IdName = e.ChatMessage.Username;

            if (!ChatUsers.ContainsKey(IdName))
            {
                var ChatUser = new ChatUser(IdName, e.ChatMessage.DisplayName);
                ChatUsers.Add(IdName, ChatUser);
            }
            if (e.ChatMessage.IsModerator != ChatUsers[IdName].IsModerator && !e.ChatMessage.IsBroadcaster)
                ChatUsers[IdName].IsModerator = e.ChatMessage.IsModerator;

            if (e.ChatMessage.Message.StartsWith('!')) { return; }

            if (e.ChatMessage.Message.Length >= 200)
                ChatUsers[IdName].tldr++;

            string Message = null;
            {
                List<string> KeyList = new List<string>(Mistakes.Keys);
                foreach (var key in KeyList)
                {
                    if (e.ChatMessage.Message.ToLower().Contains(key))
                        Message += $"{Mistakes[key]}* ";
                }
                if (!String.IsNullOrEmpty(Message))
                {
                    client.SendMessage(TwitchInfo.ChannelName, Message);
                    Message = null;
                    return;
                }
            }

            if (ChatUsers[IdName].IsBanned) { return; }

            if (e.ChatMessage.Message.Length > 200)
            {
                Message = "Duuuuuud, don't ściana tekstu me :rage:";
                if (ChatUsers[IdName].tldr > 2)
                    Message += $" To już {ChatUsers[IdName].tldr} raz!";

                client.SendMessage(TwitchInfo.ChannelName, Message);
                Message = null;
            }

            if (e.ChatMessage.Message.ToLower().Contains("siema pyrzy") || e.ChatMessage.Message.ToLower().Contains("cześć pyrzy") || e.ChatMessage.Message.ToLower().Contains("czesc pyrzy"))
            {
                client.SendMessage(TwitchInfo.ChannelName, $"Cześć {ChatUsers[IdName].DisplayedName}!");
                return;
            }

            if (LSM.AddSeconds(5) > DateTime.Now) { return; }

            if (e.ChatMessage.Message.ToLower().StartsWith("czy "))
            {
                if (ChatUsers[IdName].CzySpam > 7) { return; }

                ChatUsers[IdName].CzySpam++;
                LSM = DateTime.Now;
                if (ChatUsers[IdName].CzySpam > 5)
                {
                    client.SendMessage(TwitchInfo.ChannelName, $"Przestań zadawać tyle pytań {ChatUsers[IdName].DisplayedName} :rage:");
                    ChatUsers[IdName].CzySpam++;
                    return;
                }
                Random Random = new Random();
                double RandomValue = Random.NextDouble() * Weights.Sum();
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

            if (Regex.Match(e.ChatMessage.Message, Helper.DotDotDotPattern).Success)
            {
                LSM = DateTime.Now;
                var length = e.ChatMessage.Message.Length <= 10 ? e.ChatMessage.Message.Length : 10;
                var sb = new StringBuilder().Insert(0, "kropka ", length);
                sb.Append($"do Ciebie też {ChatUsers[IdName].DisplayedName} :rage:");

                client.SendMessage(TwitchInfo.ChannelName, sb.ToString());
                return;
            }

            if (e.ChatMessage.Message.Contains("xD") || e.ChatMessage.Message.Contains("XD"))
            {
                Random Random = new Random();
                int RandomValue = Random.Next(1, 101);
                if (RandomValue >= 20) { return; }
                LSM = DateTime.Now;
                if (IdName == "ananieana" && RandomValue == 1)
                {
                    client.SendMessage(TwitchInfo.ChannelName, "UwU");
                    return;
                }
                client.SendMessage(TwitchInfo.ChannelName, "xD");
            }
        }

        private void Client_OnLog(object sender, OnLogArgs e)
        {
            Console.WriteLine(e.Data);
        }
        private void Client_pubsub_OnPubSubServiceConnected(object sender, EventArgs e)
        {
            client_pubsub.SendTopics();
        }

        private void SetTimer()
        {
            DSCTimer = new Timer(1200000);
            DSCTimer.Elapsed += DSCEvent;
            DSCTimer.AutoReset = true;
            DSCTimer.Enabled = true;

            CzyTimer = new Timer(120000);
            CzyTimer.Elapsed += CzySpamEvent;
            CzyTimer.AutoReset = true;
            CzyTimer.Enabled = true;
        }

        private void DSCEvent(Object source, ElapsedEventArgs e)
        {
            if (simpleCommands.ContainsKey("discord"))
                client.SendMessage(TwitchInfo.ChannelName, simpleCommands["discord"]);
        }

        private void CzySpamEvent(object source, ElapsedEventArgs e)
        {
            List<string> KeyList = new List<string>(ChatUsers.Keys);
            foreach (var IdName in KeyList)
            {
                ChatUsers[IdName].CzySpam = ChatUsers[IdName].CzySpam > 0 ? ChatUsers[IdName].CzySpam - 1 : 0;
            }
        }

        internal void Disconnect()
        {
            ChatUser.ClearWyzwania(ChatUsers);

            var saveFileTemplate = new SaveFileTemplate
            {
                ChatUsers = ChatUsers,
                Responces = Responces,
                Weights = Weights,
                simpleCommands = simpleCommands,
                Grafik = Grafik,
                Mistakes = Mistakes,
                NextGame = NextGame,
                NextTitle = NextTitle,
            };

            string json = JsonConvert.SerializeObject(saveFileTemplate);

            System.IO.File.WriteAllText(Environment.CurrentDirectory + @"\path.json", json);
            client_pubsub.Disconnect();
            client.Disconnect();
        }
    }
}
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
        List<string> Mods;
        List<string> Banned_Users;
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

        DateTime LSM = DateTime.Now;
        private Timer DSCTimer;
        private Timer CzyTimer;

        private static TwitchAPI API;
        private LiveStreamMonitorService Monitor;

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
                Banned_Users = savefile.Banned_Users;
                Mods = savefile.Mods;
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
            SetTimer();
        }
        private void Monitor_OnStreamOffline(object sender, TwitchLib.Api.Services.Events.LiveStreamMonitor.OnStreamOfflineArgs e)
        {
            CzyTimer.Stop();
            CzyTimer.Dispose();
            DSCTimer.Stop();
            DSCTimer.Dispose();
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
                client.SendMessage(TwitchInfo.ChannelName, $"{ChatUsers[IdName].DisplayedName} odebrał {Nazwa} za {Koszt} Punktów");
                return;
            }
        }

        private void Client_OnChatCommandReceived(object sender, OnChatCommandReceivedArgs e)
        {
            var IdName = e.Command.ChatMessage.Username;

            if (!ChatUsers.ContainsKey(IdName))
            {
                var ChatUser = new ChatUser(IdName, e.Command.ChatMessage.DisplayName);
                ChatUsers.Add(IdName, ChatUser);
            }

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

                    if (e.Command.ArgumentsAsList.Count() == 3)
                    {
                        switch (e.Command.ArgumentsAsList[0])
                        {
                            case "add":                                 

                                if (e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator)
                                {
                                    AtIdName = e.Command.ArgumentsAsList[1].ToLower().Replace("@", "");
                                    if (!ChatUsers.ContainsKey(AtIdName))
                                    {
                                        client.SendMessage(TwitchInfo.ChannelName, "Ten Użytkownik nie odebrał jeszcze żadnych Pyr");
                                        return;
                                    }
                                    if (!Helper.IsNumeric(e.Command.ArgumentsAsList[2]))
                                    {
                                        client.SendMessage(TwitchInfo.ChannelName, "Musisz podać poprawną Liczbę Pyr!");
                                        return;
                                    }
                                    ChatUsers[AtIdName].Pyry += Convert.ToInt32(e.Command.ArgumentsAsList[2]);
                                    client.SendMessage(TwitchInfo.ChannelName, $"{ChatUsers[AtIdName].DisplayedName} ma {ChatUsers[AtIdName].Pyry} {Helper.Ending(ChatUsers[IdName].Pyry)}!");
                                }

                                return;
                            case "remove":

                                if (e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator)
                                {
                                    AtIdName = e.Command.ArgumentsAsList[1].ToLower().Replace("@", "");
                                    if (!ChatUsers.ContainsKey(AtIdName))
                                    {
                                        client.SendMessage(TwitchInfo.ChannelName, "Ten Użytkownik nie odebrał jeszcze żadnych Pyr");
                                        return;
                                    }
                                    if (!Helper.IsNumeric(e.Command.ArgumentsAsList[2]))
                                    {
                                        client.SendMessage(TwitchInfo.ChannelName, "Musisz podać poprawną liczbę Pyr!");
                                        return;
                                    }
                                    ChatUsers[AtIdName].Pyry -= Convert.ToInt32(e.Command.ArgumentsAsList[2]);

                                    if (ChatUsers[AtIdName].Pyry < 0)
                                        ChatUsers[AtIdName].Pyry = 0;

                                    client.SendMessage(TwitchInfo.ChannelName, $"{ChatUsers[AtIdName].DisplayedName} ma {ChatUsers[AtIdName].Pyry} {Helper.Ending(ChatUsers[IdName].Pyry)}!");
                                }

                                return;
                        }
                    }

                    if (e.Command.ArgumentsAsList.Count() == 1)
                    {
                        AtIdName = e.Command.ArgumentsAsList[0].Replace("@", "").ToLower();
                        if (!ChatUsers.ContainsKey(AtIdName))
                        {
                            client.SendMessage(TwitchInfo.ChannelName, "Ten Użytkownik nie odebrał jeszcze żadnych Pyr");
                            return;
                        }
                        client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName} {ChatUsers[AtIdName].DisplayedName} Ma {ChatUsers[AtIdName].Pyry} {Helper.Ending(ChatUsers[AtIdName].Pyry)}!");
                    }
                    else { client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName} Masz {ChatUsers[IdName].Pyry} {Helper.Ending(ChatUsers[IdName].Pyry)}!"); }

                    break;
                case "wyzwij":
                    {
                        int Stawka;
                        if (e.Command.ArgumentsAsList.Count() == 0 || e.Command.ArgumentsAsList.Count() > 2)
                        {
                            client.SendMessage(TwitchInfo.ChannelName, "Poprawny zapis: !wyzwij (Użytkownik) (kwota zakładu)");
                            return;
                        }
                        AtIdName = e.Command.ArgumentsAsList[0].ToLower().Replace("@", "");
                        if (!Helper.IsUsername(AtIdName))
                        {
                            client.SendMessage(TwitchInfo.ChannelName, "Musisz podać poprawną nazwę użytkownika!");
                            return;
                        }
                        if (!ChatUsers.ContainsKey(AtIdName))
                        {
                            client.SendMessage(TwitchInfo.ChannelName, "Ten Użytkownik nie odebrał jeszcze żadnych pyr");
                            return;
                        }
                        List<string> KeyList = new List<string>(ChatUsers.Where(x => !string.IsNullOrEmpty(x.Value.Wyzwania)).Select(x => x.Value.Wyzwania));
                        foreach (var key in KeyList)
                        {
                            if (key == AtIdName)
                            {
                                if (ChatUsers[key].Wyzwania == IdName)
                                {
                                    client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName}, {ChatUsers[AtIdName].DisplayedName} już wyzwał Cię na pojedynek!");
                                    return;
                                }
                                client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName}, {ChatUsers[AtIdName].DisplayedName} jest juz zapisany na inną walkę!");
                                return;
                            }
                        }
                        if (e.Command.ArgumentsAsList.Count() == 2)
                        {
                            if (!Helper.IsNumeric(e.Command.ArgumentsAsList[1]))
                            {
                                client.SendMessage(TwitchInfo.ChannelName, "Musisz podać poprawną kwotę zakładu!");
                                return;
                            }
                            Stawka = Convert.ToInt32(e.Command.ArgumentsAsList[1]);
                        }
                        else { Stawka = 0; }

                        if (ChatUsers[IdName].Pyry < Stawka)
                        {
                            client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName} nie masz wystarczająco Pyr!");
                            return;
                        }
                        if (ChatUsers[AtIdName].Pyry < Stawka)
                        {
                            client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName}, {ChatUsers[AtIdName].DisplayedName} nie ma wystarczająco Pyr!");
                            return;
                        }
                        ChatUsers[AtIdName].Wyzwania = IdName;
                        ChatUsers[IdName].Wyzwania = AtIdName;
                        ChatUsers[AtIdName].Stawka = Stawka;
                        ChatUsers[IdName].Stawka = Stawka;

                        client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[AtIdName].DisplayedName}, {ChatUsers[IdName].DisplayedName} wyzwał Cię na pojedynek o {Stawka} {Helper.Ending(Stawka)}! Użyj !walcz albo !uciekaj");
                    }
                    break;
                case "walcz":
                    {
                        List<string> KeyList = new List<string>(ChatUsers.Where(x => !string.IsNullOrEmpty(x.Value.Wyzwania)).Select(x => x.Value.Wyzwania));
                        foreach (var key in KeyList)
                        {
                            if (ChatUsers[key].Wyzwania == IdName)
                            {
                                int Stawka = ChatUsers[key].Stawka;
                                ChatUsers[IdName].Wyzwania = "";
                                ChatUsers[key].Wyzwania = "";
                                ChatUsers[IdName].Stawka = 0;
                                ChatUsers[key].Stawka = 0;
                                Random Random = new Random();
                                int RandomValue = Random.Next(1, 3);
                                if (RandomValue == 1)
                                {
                                    ChatUsers[key].Pyry += Stawka;
                                    ChatUsers[IdName].Pyry -= Stawka;
                                    client.SendMessage(TwitchInfo.ChannelName, $"{ChatUsers[key].DisplayedName} wygrał pojedynek z {ChatUsers[IdName].DisplayedName} o {Stawka} {Helper.Ending(Stawka)}!");
                                    return;
                                }
                                ChatUsers[key].Pyry -= Stawka;
                                ChatUsers[IdName].Pyry += Stawka;
                                client.SendMessage(TwitchInfo.ChannelName, $"{ChatUsers[IdName].DisplayedName} wygrał pojedynek z {ChatUsers[key].DisplayedName} o {Stawka} {Helper.Ending(Stawka)}!");
                                return;
                            }
                        }
                        client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName}, nie byłeś wyzwany na żaden pojedynek!");
                    }
                    break;
                case "uciekaj":
                    {
                        List<string> KeyList = new List<string>(ChatUsers.Where(x => !string.IsNullOrEmpty(x.Value.Wyzwania)).Select(x => x.Value.Wyzwania));
                        foreach (var key in KeyList)
                        {
                            if (ChatUsers[key].Wyzwania == IdName)
                            {
                                ChatUsers[IdName].Wyzwania = "";
                                ChatUsers[key].Wyzwania = "";
                                ChatUsers[IdName].Stawka = 0;
                                ChatUsers[key].Stawka = 0;
                                client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[key].DisplayedName}, {ChatUsers[IdName].DisplayedName} nie podjął wyzwania i uciekł!");
                                return;
                            }
                        }
                        client.SendMessage(TwitchInfo.ChannelName, $"@{ChatUsers[IdName].DisplayedName}, nie byłeś wyzwany na żaden pojedynek!");
                    }
                    break;
                case "commands":

                    Message = "Dostępne komendy: !grafik ";
                    foreach (string Key in simpleCommands.Keys)
                        Message += $"!{Key} ";

                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster)
                        Message += "!title !game !next !ban !unban !command add/delete";

                    client.SendMessage(TwitchInfo.ChannelName, Message);

                    break;
                case "grafik":

                    if ((e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator) && e.Command.ArgumentsAsList.Count() != 0)
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
                            string Gra = string.Empty;
                            string Argument = string.Empty;
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

                    break;
                case "game":
                    {
                        if ((e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator) && e.Command.ArgumentsAsList.Count() != 0)
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
                    break;
                case "title":
                    {
                        if ((e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator) && e.Command.ArgumentsAsList.Count() != 0)
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

            if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster)
            {
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
                            AtIdName = e.Command.ArgumentsAsList[0].Replace("@", "");
                            if (!Helper.IsUsername(AtIdName))
                            {
                                client.SendMessage(TwitchInfo.ChannelName, "Musisz podać poprawną nazwę użytkownika!");
                                return;
                            }
                            if (Mods.Contains(AtIdName.ToLower()))
                            {
                                client.SendMessage(TwitchInfo.ChannelName, "Nie można zbanować moderatora!");
                                return;
                            }
                            if (!Banned_Users.Contains(AtIdName.ToLower()))
                            {
                                Banned_Users.Add(AtIdName.ToLower());
                                client.SendMessage(TwitchInfo.ChannelName, $"Zbanowano {AtIdName} :partying:");
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
                            AtIdName = e.Command.ArgumentsAsList[0].Replace("@", "");
                            if (!Helper.IsUsername(AtIdName))
                            {
                                client.SendMessage(TwitchInfo.ChannelName, "Musisz podać poprawną nazwę użytkownika!");
                                return;
                            }
                            if (Banned_Users.Contains(AtIdName.ToLower()))
                            {
                                Banned_Users.Remove(AtIdName.ToLower());
                                client.SendMessage(TwitchInfo.ChannelName, $"Odbanowano {AtIdName} :frowning:");
                                return;
                            }
                            client.SendMessage(TwitchInfo.ChannelName, $"{AtIdName} nie jest zbanowany!");
                            return;
                        }
                        client.SendMessage(TwitchInfo.ChannelName, "Poprawny zapis: !unban (Użytkownik)");

                        break;
                    case "command":

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

                                client.SendMessage(TwitchInfo.ChannelName, "Poprawny zapis: !command add (Nazwa) (Treść)");
                                client.SendMessage(TwitchInfo.ChannelName, "Poprawny zapis: !command delete (Nazwa)");
                                return;
                        }
                        break;
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
        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            var IdName = e.ChatMessage.Username;
            if (!ChatUsers.ContainsKey(IdName))
            {
                var ChatUser = new ChatUser(IdName, e.ChatMessage.DisplayName);
                ChatUsers.Add(IdName, ChatUser);
            }

            if (e.ChatMessage.Message.StartsWith('!')) { return; }

            string Message = null;

            if (e.ChatMessage.Message.Length > 200)
            {
                ChatUsers[IdName].tldr++;
            }

            //Ortografia check 
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

            if (Banned_Users.Contains(e.ChatMessage.Username)) { return; }

            if (e.ChatMessage.Message.Length > 200)
            {
                Message = "Duuuuuud, don't ściana tekstu me :rage:";
                if (ChatUsers[IdName].tldr > 2)
                    Message += $" to już {ChatUsers[IdName].tldr} raz!";

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
                sb.Append($"do Ciebie też {e.ChatMessage.Username} :rage:");

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
            var saveFileTemplate = new SaveFileTemplate
            {
                ChatUsers = ChatUsers,
                Banned_Users = Banned_Users,
                Mods = Mods,
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
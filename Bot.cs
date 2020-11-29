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

namespace PyRZyBot
{
    internal class Bot
    {
        TwitchClient client;
        List<string> Mods;
        List<string> Banned_Users;
        List<int> Weights;
        List<string> Responces;
        List<string> Grafik;
        Dictionary<string, int> tldrCounter;
        Dictionary<string, string> simpleCommands;
        Dictionary<string, int> czySpam = new Dictionary<string, int>();

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

        string Title;
        string Game;

        string _dotDotDotPattern = @"^(\.)+$";

        public static bool IsUsername(string username)
        {
            string pattern = @"^[a-zA-Z0-9]{3,24}";
            Regex regex = new Regex(pattern);
            return regex.IsMatch(username);
        }

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
                Banned_Users = savefile.Banned_Users;
                Mods = savefile.Mods;
                Responces = savefile.Responces;
                Weights = savefile.Weights;
                tldrCounter = savefile.tldrCounter;
                simpleCommands = savefile.simpleCommands;
                Grafik = savefile.Grafik;
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

            API = new TwitchAPI();
            API.Settings.ClientId = TwitchInfo.ClientID;
            API.Settings.AccessToken = TwitchInfo.AccessToken;
            var ChannelInfo = API.V5.Channels.GetChannelAsync(API.Settings.AccessToken);
            Game = ChannelInfo.Result.Game;
            Title = ChannelInfo.Result.Status;
        }

        private void Client_OnChatCommandReceived(object sender, OnChatCommandReceivedArgs e)
        {
            if (simpleCommands.ContainsKey(e.Command.CommandText.ToLower()))
            {
                client.SendMessage(TwitchInfo.ChannelName, simpleCommands[e.Command.CommandText.ToLower()]);
                return;
            }

            switch (e.Command.CommandText.ToLower())
            {
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
                            var IsNumeric = int.TryParse(e.Command.ArgumentsAsList[0], out _);

                            if (IsNumeric)
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
                                if (IsNumeric)
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
                            Game = e.Command.ArgumentsAsString;
                            var t = Task.Run(async () => { await API.V5.Channels.UpdateChannelAsync(TwitchInfo.ChannelID, Title, Game); });
                            t.Wait();
                            client.SendMessage(TwitchInfo.ChannelName, $"Zmieniono grę na: {Game}");
                            return;
                        }
                        client.SendMessage(TwitchInfo.ChannelName, $"Obecna gra: {Game}");
                    }
                    break;
                case "title":
                    {
                        if ((e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator) && e.Command.ArgumentsAsList.Count() != 0)
                        {
                            Title = e.Command.ArgumentsAsString;
                            var t = Task.Run(async () => { await API.V5.Channels.UpdateChannelAsync(TwitchInfo.ChannelID, Title, Game); });
                            t.Wait();
                            client.SendMessage(TwitchInfo.ChannelName, $"Zmieniono tytuł na: {Title}");
                            return;
                        }
                        client.SendMessage(TwitchInfo.ChannelName, $"Obecny tytuł: {Title}");
                    }
                    break;
                case "commands":

                    String Message = "Dostępne komendy: ";
                    foreach (string Key in simpleCommands.Keys)
                        Message += $"!{Key} ";

                    Message += "!grafik !title !game";
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster)
                        Message += "!ban !unban !command add/delete";

                    client.SendMessage(TwitchInfo.ChannelName, Message);

                    break;
            }

            if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster)
            {
                string Username = string.Empty;
                switch (e.Command.CommandText.ToLower())
                {
                    case "ban":

                        if (e.Command.ArgumentsAsList.Count == 0)
                        {
                            client.SendMessage(TwitchInfo.ChannelName, "Poprawny zapis: !ban (Użytkownik)");
                            return;
                        }
                        Username = e.Command.ArgumentsAsList[0].Replace("@", "");
                        if (!IsUsername(Username))
                        {
                            client.SendMessage(TwitchInfo.ChannelName, "Musisz podać poprawną nazwę użytkownika!");
                            return;
                        }
                        if (!Banned_Users.Contains(Username.ToLower()))
                        {
                            if (Mods.Contains(Username.ToLower()))
                            {
                                client.SendMessage(TwitchInfo.ChannelName, "Nie można zbanować moderatora!");
                                return;
                            }
                            Banned_Users.Add(Username.ToLower());
                            client.SendMessage(TwitchInfo.ChannelName, $"Zbanowano {Username} :partying:");
                            return;
                        }
                        client.SendMessage(TwitchInfo.ChannelName, $"{Username} już jest zbanowany!");

                        break;
                    case "unban":

                        if (e.Command.ArgumentsAsList.Count == 0)
                        {
                            client.SendMessage(TwitchInfo.ChannelName, "Poprawny zapis: !unban (Użytkownik)");
                            return;
                        }
                        Username = e.Command.ArgumentsAsList[0].Replace("@", "");
                        if (!IsUsername(Username))
                        {
                            client.SendMessage(TwitchInfo.ChannelName, "Musisz podać poprawną nazwę użytkownika!");
                            return;
                        }
                        if (Banned_Users.Contains(Username.ToLower()))
                        {
                            Banned_Users.Remove(Username.ToLower());
                            client.SendMessage(TwitchInfo.ChannelName, $"Odbanowano {Username} :frowning:");
                            return;
                        }
                        client.SendMessage(TwitchInfo.ChannelName, $"{Username} nie jest zbanowany!");

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
                                    client.SendMessage(TwitchInfo.ChannelName, "Musisz podać nazwę i treść komendy!");
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
                                    client.SendMessage(TwitchInfo.ChannelName, "Musisz podać nazwę komendy!");
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
                }
            }
        }


        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            var Username = e.ChatMessage.Username;

            if (e.ChatMessage.Message.Length > 200)
            {
                if (!tldrCounter.ContainsKey(e.ChatMessage.Username))
                    tldrCounter.Add(e.ChatMessage.Username, 0);

                tldrCounter[e.ChatMessage.Username]++;
                var output = "Duuuuuud, don't ściana tekstu me :rage:";
                if (tldrCounter[e.ChatMessage.Username] > 2)
                    output += $" to już {tldrCounter[e.ChatMessage.Username]} raz!";

                client.SendMessage(TwitchInfo.ChannelName, output);
                return;
            }
            if (Banned_Users.Contains(e.ChatMessage.Username)) { return; }

            if (e.ChatMessage.Message.ToLower().Contains("siema pyrzy") || e.ChatMessage.Message.ToLower().Contains("cześć pyrzy") || e.ChatMessage.Message.ToLower().Contains("czesc pyrzy"))
            {
                client.SendMessage(TwitchInfo.ChannelName, $"Cześć {Username.TrimEnd(digits)}!");
                return;
            }

            if (LSM.AddSeconds(5) > DateTime.Now) { return; }

            if (e.ChatMessage.Message.ToLower().StartsWith("czy "))
            {
                if (!czySpam.ContainsKey(e.ChatMessage.Username))
                    czySpam.Add(e.ChatMessage.Username, 0);

                if (czySpam[e.ChatMessage.Username] > 7) { return; }

                czySpam[e.ChatMessage.Username]++;
                LSM = DateTime.Now;
                if (czySpam[e.ChatMessage.Username] > 5)
                {
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

            if (Regex.Match(e.ChatMessage.Message, _dotDotDotPattern).Success)
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
                double R = Random.NextDouble();
                if (R <= 0.9) { return; }
                LSM = DateTime.Now;
                client.SendMessage(TwitchInfo.ChannelName, "xD");
                return;
            }
        }

        private void Client_OnLog(object sender, OnLogArgs e)
        {
            Console.WriteLine(e.Data);
        }

        private void SetTimer()
        {
            CzyTimer = new Timer(120000);
            CzyTimer.Elapsed += CzyEvent;
            CzyTimer.AutoReset = true;
            CzyTimer.Enabled = true;
            DSCTimer = new Timer(1200000);
            DSCTimer.Elapsed += DSCEvent;
            DSCTimer.AutoReset = true;
            DSCTimer.Enabled = true;
        }

        private void CzyEvent(object source, ElapsedEventArgs e)
        {
            List<string> KeyList = new List<string>(czySpam.Keys);
            foreach (var key in KeyList)
            {
                czySpam[key] = czySpam[key] > 0 ? czySpam[key] - 1 : 0;
            }
        }

        private void DSCEvent(Object source, ElapsedEventArgs e)
        {
            client.SendMessage(TwitchInfo.ChannelName, "Chcesz wiedzieć, kiedy będzie kolejny stream? zapraszam na Discorda: https://discord.gg/jtGZQFa");
        }

        internal void Disconnect()
        {
            CzyTimer.Stop();
            CzyTimer.Dispose();
            DSCTimer.Stop();
            DSCTimer.Dispose();
            var saveFileTemplate = new SaveFileTemplate
            {
                Banned_Users = Banned_Users,
                Mods = Mods,
                Responces = Responces,
                Weights = Weights,
                tldrCounter = tldrCounter,
                simpleCommands = simpleCommands,
                Grafik = Grafik
            };

            string json = JsonConvert.SerializeObject(saveFileTemplate);

            System.IO.File.WriteAllText(Environment.CurrentDirectory + @"\path.json", json);
            client.Disconnect();
        }
    }
}
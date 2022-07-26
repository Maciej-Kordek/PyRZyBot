using PyRZyBot_2._0.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PyRZyBot_2._0
{
    class InternalCommands_Channel
    {
        static Dictionary<string, string> LastTitle = new Dictionary<string, string> { { "kyrzy", "" }, { "ananieana", "" } };
        static Dictionary<string, GameInfo> LastGame = new Dictionary<string, GameInfo> { { "kyrzy", new GameInfo() }, { "ananieana", new GameInfo() } };

        public static void Title(string Channel, string Name, List<string> Arguments)
        {
            if (Database.IsBanned(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono użycia komendy !title użytkownikowi {Name} (Użytkownik Zbanowany)");
                return;
            }
            if (Database.IsTimeouted(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono użycia komendy !title użytkownikowi {Name} (Użytkownik wykluczony czasOwO)");
                return;
            }
            if (Database.GetAccessLevel(Channel, Name) < (int)AccessLevels.mod)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !title użytkownikowi {Name} (Brak uprawnień)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Nie posiadasz odpowiednich uprawnień");
                return;
            }

            string Gameid = string.Empty;
            string Title = string.Empty;
            string ChannelId = string.Empty;

            var Task = System.Threading.Tasks.Task.Run(async () =>
            {
                using (var context = new Database())
                {
                    ChannelId = context.ChannelInfo.FirstOrDefault(x => x.Channel == Channel && x.Info == "ChannelId").Value;
                    var ChannelInfo = await Bot.APIs[Channel].Helix.Channels.GetChannelInformationAsync(ChannelId, Bot.APIs[Channel].Settings.AccessToken);
                    Gameid = ChannelInfo.Data[0].GameId;
                    Title = ChannelInfo.Data[0].Title;
                    ChannelId = ChannelInfo.Data[0].BroadcasterId;
                }
            });
            Task.Wait();

            switch (Arguments.Count())
            {
                case 1:
                    {
                        Bot.SendMessage(Channel, 2, true, $"@{Name}, Obecny tytuł to: {Title}");
                    }
                    return;
                default:
                    {
                        var StringBuilder = new StringBuilder(Arguments[1]);
                        for (int i = 2; i < Arguments.Count; i++)
                            StringBuilder.Append($" {Arguments[i]}");

                        Title = StringBuilder.ToString();

                        try
                        {
                            var Task2 = System.Threading.Tasks.Task.Run(async () =>
                            {
                                await Bot.APIs[Channel].Helix.Channels.ModifyChannelInformationAsync(ChannelId,
                                    new TwitchLib.Api.Helix.Models.Channels.ModifyChannelInformation.ModifyChannelInformationRequest { Title = Title, GameId = Gameid },
                                    Bot.APIs[Channel].Settings.AccessToken);
                            });
                            Task2.Wait();
                            Bot.SendMessage(Channel, 2, true, $"@{Name}, Zmieniono tytuł na: {Title}");
                        }
                        catch
                        {
                            Bot.LogEvent(Channel, 2, $"Zmiana tytułu nie powiodła się");
                            Bot.SendMessage(Channel, 1, false, $"@{Name}, Nie udało się zmienić tytułu");
                        }
                    }
                    return;
            }
        }
        public static void Game(string Channel, string Name, List<string> Arguments)
        {
            if (Database.IsBanned(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono użycia komendy !game użytkownikowi {Name} (Użytkownik Zbanowany)");
                return;
            }
            if (Database.IsTimeouted(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono użycia komendy !game użytkownikowi {Name} (Użytkownik wykluczony czasOwO)");
                return;
            }
            if (Database.GetAccessLevel(Channel, Name) < (int)AccessLevels.mod)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !game użytkownikowi {Name} (Brak uprawnień)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Nie posiadasz odpowiednich uprawnień");
                return;
            }

            string GameName = string.Empty;
            string Title = string.Empty;
            string ChannelId = string.Empty;

            var Task = System.Threading.Tasks.Task.Run(async () =>
            {
                using (var context = new Database())
                {
                    ChannelId = context.ChannelInfo.FirstOrDefault(x => x.Channel == Channel && x.Info == "ChannelId").Value;
                    var ChannelInfo = await Bot.APIs[Channel].Helix.Channels.GetChannelInformationAsync(ChannelId, Bot.APIs[Channel].Settings.AccessToken);
                    GameName = ChannelInfo.Data[0].GameName;
                    Title = ChannelInfo.Data[0].Title;
                    ChannelId = ChannelInfo.Data[0].BroadcasterId;
                }
            });
            Task.Wait();

            switch (Arguments.Count())
            {
                case 1:
                    {
                        Bot.SendMessage(Channel, 2, true, $"@{Name}, Obecna gra to: {GameName}");
                    }
                    return;
                default:
                    {
                        var StringBuilder = new StringBuilder(Arguments[1]);
                        for (int i = 2; i < Arguments.Count; i++)
                            StringBuilder.Append($" {Arguments[i]}");

                        GameName = StringBuilder.ToString();

                        try
                        {
                            var Task2 = System.Threading.Tasks.Task.Run(async () =>
                            {
                                var Result = await Bot.APIs[Channel].Helix.Games.GetGamesAsync(gameNames: new List<string> { GameName });
                                if (Result.Games.Length == 0)
                                    throw new Exception();

                                GameName = Result.Games[0].Name;
                                var GameId = Result.Games[0].Id;
                                await Bot.APIs[Channel].Helix.Channels.ModifyChannelInformationAsync(ChannelId,
                                    new TwitchLib.Api.Helix.Models.Channels.ModifyChannelInformation.ModifyChannelInformationRequest { Title = Title, GameId = GameId },
                                    Bot.APIs[Channel].Settings.AccessToken);
                            });
                            Task2.Wait();
                            Bot.SendMessage(Channel, 2, true, $"@{Name}, Zmieniono grę na: {GameName}");
                        }
                        catch(Exception e)
                        {
                            Bot.LogEvent(Channel, 2, $"Zmiana gry nie powiodła się");
                            Bot.SendMessage(Channel, 1, false, $"@{Name}, Nie udało się znaleźć gry");
                        }
                    }
                    return;
            }
        }
        public static void Next(string Channel, string Name, List<string> Arguments)
        {
            switch (Arguments.Count())
            {
                case 1:
                    {
                        ApplyNext(Channel, Name);
                    }
                    return;

                case 2:
                    {
                        switch (Arguments[1].ToLower())
                        {
                            case "title":
                                {
                                    DisplayNextTitle(Channel, Name);
                                }
                                return;

                            case "game":
                                {
                                    DisplayNextGame(Channel, Name);
                                }
                                return;

                            case "undo":
                                {
                                    UndoNext(Channel, Name);
                                }
                                return;
                        }
                    }
                    return;

                default:
                    {
                        switch (Arguments[1].ToLower())
                        {
                            case "title":
                                {
                                    SetNextTitle(Channel, Name, Arguments);
                                }
                                return;

                            case "game":
                                {
                                    SetNextGame(Channel, Name, Arguments);
                                }
                                return;
                        }
                    }
                    return;
            }
        }
        static void ApplyNext(string Channel, string Name)
        {
            if (Database.IsBanned(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono użycia komendy !next użytkownikowi {Name} (Użytkownik Zbanowany)");
                return;
            }
            if (Database.IsTimeouted(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono użycia komendy !next użytkownikowi {Name} (Użytkownik wykluczony czasOwO)");
                return;
            }
            if (Database.GetAccessLevel(Channel, Name) < (int)AccessLevels.mod)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !next użytkownikowi {Name} (Brak uprawnień)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Nie posiadasz odpowiednich uprawnień");
                return;
            }
            string NextTitle;
            string NextGame;

            using (var context = new Database())
            {
                var ChannelNextTitle = context.ChannelInfo.FirstOrDefault(x => x.Channel == Channel && x.Info == "NextTitle");
                var ChannelNextGame = context.ChannelInfo.FirstOrDefault(x => x.Channel == Channel && x.Info == "NextGame");
                NextTitle = ChannelNextTitle.Value;
                NextGame = ChannelNextGame.Value;
                ChannelNextTitle.Value = "";
                ChannelNextGame.Value = "";
                context.Update(ChannelNextTitle);
                context.Update(ChannelNextGame);
                context.SaveChanges();
            }

            if (string.IsNullOrEmpty(NextTitle) || string.IsNullOrEmpty(NextGame))
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !next użytkownikowi {Name} (Nieustawiony następny tutuł i/lub gra)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Nie ustawiono następnego tytułu i/lub gry");
                return;
            }

            try
            {
                var Task = System.Threading.Tasks.Task.Run(async () =>
                {
                    using (var context = new Database())
                    {
                        var ChannelId = context.ChannelInfo.FirstOrDefault(x => x.Channel == Channel && x.Info == "ChannelId").Value;
                        var ChannelInfo = await Bot.APIs[Channel].Helix.Channels.GetChannelInformationAsync(ChannelId, Bot.APIs[Channel].Settings.AccessToken);
                        string GameId = ChannelInfo.Data[0].GameId;
                        string GameName = ChannelInfo.Data[0].GameName;
                        string Title = ChannelInfo.Data[0].Title;
                        LastGame[Channel].GameId = GameId;
                        LastGame[Channel].GameName = GameName;
                        LastTitle[Channel] = Title;

                        var Result = await Bot.APIs[Channel].Helix.Games.GetGamesAsync(gameNames: new List<string> { NextGame });
                        if (Result.Games.Length == 0)
                            throw new Exception();

                        var NextGameId = Result.Games[0].Id;

                        await Bot.APIs[Channel].Helix.Channels.ModifyChannelInformationAsync(ChannelId,
                                    new TwitchLib.Api.Helix.Models.Channels.ModifyChannelInformation.ModifyChannelInformationRequest { Title = NextTitle, GameId = NextGameId },
                                    Bot.APIs[Channel].Settings.AccessToken);
                    }
                });
                Task.Wait();
                Bot.SendMessage(Channel, 2, true, $"@{Name}, Zmieniono tytuł i grę na: {NextTitle} | {NextGame}");
            }
            catch
            {
                LastGame[Channel].GameId = "";
                LastGame[Channel].GameName = "";
                LastTitle[Channel] = "";
                Bot.LogEvent(Channel, 2, $"Zmiana tytułu i gry nie powiodła się");
                Bot.SendMessage(Channel, 1, false, $"@{Name}, Nie udało się zmienić tytułu i gry");
            }
        }
        static void UndoNext(string Channel, string Name)
        {
            if (Database.IsBanned(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono użycia komendy !next undo użytkownikowi {Name} (Użytkownik Zbanowany)");
                return;
            }
            if (Database.IsTimeouted(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono użycia komendy !next undo użytkownikowi {Name} (Użytkownik wykluczony czasOwO)");
                return;
            }
            if (Database.GetAccessLevel(Channel, Name) < (int)AccessLevels.mod)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !next undo użytkownikowi {Name} (Brak uprawnień)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Nie posiadasz odpowiednich uprawnień");
                return;
            }
            if (string.IsNullOrEmpty(LastTitle[Channel]) || string.IsNullOrEmpty(LastGame[Channel].GameName))
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !next undo użytkownikowi {Name} (Brak ostatniego tutułu i gry)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Brak ostatniego tytułu i gry");
                return;
            }

            try
            {
                string GameId = string.Empty;
                string GameName = string.Empty;
                string Title = string.Empty;
                var Task = System.Threading.Tasks.Task.Run(async () =>
                {
                    using (var context = new Database())
                    {
                        var ChannelId = context.ChannelInfo.FirstOrDefault(x => x.Channel == Channel && x.Info == "ChannelId").Value;
                        var ChannelInfo = await Bot.APIs[Channel].Helix.Channels.GetChannelInformationAsync(ChannelId, Bot.APIs[Channel].Settings.AccessToken);
                        GameName = ChannelInfo.Data[0].GameName;
                        GameId = ChannelInfo.Data[0].GameId;
                        Title = ChannelInfo.Data[0].Title;
                        await Bot.APIs[Channel].Helix.Channels.ModifyChannelInformationAsync(ChannelId,
                                    new TwitchLib.Api.Helix.Models.Channels.ModifyChannelInformation.ModifyChannelInformationRequest { Title = LastTitle[Channel], GameId = LastGame[Channel].GameId },
                                    Bot.APIs[Channel].Settings.AccessToken);
                    }
                });
                Task.Wait();
                Bot.SendMessage(Channel, 2, true, $"@{Name}, Zmieniono tytuł i grę na: {LastTitle[Channel]} | {LastGame[Channel].GameName}");
                LastGame[Channel].GameId = "";
                LastGame[Channel].GameName = "";
                LastTitle[Channel] = "";
                using (var context = new Database())
                {
                    var ChannelNextTitle = context.ChannelInfo.FirstOrDefault(x => x.Channel == Channel && x.Info == "NextTitle");
                    var ChannelNextGame = context.ChannelInfo.FirstOrDefault(x => x.Channel == Channel && x.Info == "NextGame");
                    ChannelNextTitle.Value = Title;
                    ChannelNextGame.Value = GameName;
                    context.Update(ChannelNextTitle);
                    context.Update(ChannelNextGame);
                    context.SaveChanges();
                }
            }
            catch
            {
                Bot.LogEvent(Channel, 2, $"Zmiana tytułu i gry nie powiodła się");
                Bot.SendMessage(Channel, 1, false, $"@{Name}, Nie udało się zmienić tytułu i gry");
            }
        }
        static void SetNextTitle(string Channel, string Name, List<string> Arguments)
        {
            if (Database.IsBanned(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono użycia komendy !next title użytkownikowi {Name} (Użytkownik Zbanowany)");
                return;
            }
            if (Database.IsTimeouted(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono użycia komendy !next title użytkownikowi {Name} (Użytkownik wykluczony czasOwO)");
                return;
            }
            if (Database.GetAccessLevel(Channel, Name) < (int)AccessLevels.mod)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !next title użytkownikowi {Name} (Brak uprawnień)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Nie posiadasz odpowiednich uprawnień");
                return;
            }

            var StringBuilder = new StringBuilder(Arguments[2]);
            for (int i = 3; i < Arguments.Count; i++)
                StringBuilder.Append($" {Arguments[i]}");

            string NextTitle = StringBuilder.ToString();

            if (NextTitle.Length > 140)
            {
                Bot.SendMessage(Channel, 2, true, $"@{Name}, Tytuł jest za długi");
                return;
            }

            using (var context = new Database())
            {
                var ChannelNextTitle = context.ChannelInfo.FirstOrDefault(x => x.Channel == Channel && x.Info == "NextTitle");
                ChannelNextTitle.Value = NextTitle;
                context.Update(ChannelNextTitle);
                context.SaveChanges();
            }
            Bot.SendMessage(Channel, 2, true, $"@{Name}, Zmieniono następny tytuł na: {NextTitle}");
        }
        static void DisplayNextTitle(string Channel, string Name)
        {
            if (Database.IsBanned(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono użycia komendy !next title użytkownikowi {Name} (Użytkownik Zbanowany)");
                return;
            }
            if (Database.IsTimeouted(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono użycia komendy !next title użytkownikowi {Name} (Użytkownik wykluczony czasOwO)");
                return;
            }
            if (Database.GetAccessLevel(Channel, Name) < (int)AccessLevels.mod)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !next title użytkownikowi {Name} (Brak uprawnień)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Nie posiadasz odpowiednich uprawnień");
                return;
            }
            string NextTitle;

            using (var context = new Database())
                NextTitle = context.ChannelInfo.FirstOrDefault(x => x.Channel == Channel && x.Info == "NextTitle").Value;

            Bot.SendMessage(Channel, 2, true, $"@{Name}, Następny tytuł to: {NextTitle}");
        }
        static void SetNextGame(string Channel, string Name, List<string> Arguments)
        {
            if (Database.IsBanned(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono użycia komendy !next game użytkownikowi {Name} (Użytkownik Zbanowany)");
                return;
            }
            if (Database.IsTimeouted(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono użycia komendy !next game użytkownikowi {Name} (Użytkownik wykluczony czasOwO)");
                return;
            }
            if (Database.GetAccessLevel(Channel, Name) < (int)AccessLevels.mod)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !next game użytkownikowi {Name} (Brak uprawnień)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Nie posiadasz odpowiednich uprawnień");
                return;
            }

            var StringBuilder = new StringBuilder(Arguments[2]);
            for (int i = 3; i < Arguments.Count; i++)
                StringBuilder.Append($" {Arguments[i]}");

            string NextGame = StringBuilder.ToString();

            var Task = System.Threading.Tasks.Task.Run(async () =>
            {
                var Result = await Bot.APIs[Channel].Helix.Games.GetGamesAsync(gameNames: new List<string> { NextGame });
                if (Result.Games.Length == 0)
                {
                    Bot.LogEvent(Channel, 2, $"Zmiana następnej gry nie powiodła się");
                    Bot.SendMessage(Channel, 1, false, $"@{Name}, Nie udało się znaleźć gry");
                    return;
                }
                NextGame = Result.Games[0].Name;
                Bot.SendMessage(Channel, 2, true, $"@{Name}, Zmieniono następną grę na: {NextGame}");
                using (var context = new Database())
                {
                    var ChannelNextTitle = context.ChannelInfo.FirstOrDefault(x => x.Channel == Channel && x.Info == "NextGame");
                    ChannelNextTitle.Value = NextGame;
                    context.Update(ChannelNextTitle);
                    context.SaveChanges();
                }
            });
            Task.Wait();
        }
        static void DisplayNextGame(string Channel, string Name)
        {
            if (Database.IsBanned(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono użycia komendy !next game użytkownikowi {Name} (Użytkownik Zbanowany)");
                return;
            }
            if (Database.IsTimeouted(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono użycia komendy !next game użytkownikowi {Name} (Użytkownik wykluczony czasOwO)");
                return;
            }
            if (Database.GetAccessLevel(Channel, Name) < (int)AccessLevels.mod)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !next game użytkownikowi {Name} (Brak uprawnień)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Nie posiadasz odpowiednich uprawnień");
                return;
            }
            string NextGame;

            using (var context = new Database())
                NextGame = context.ChannelInfo.FirstOrDefault(x => x.Channel == Channel && x.Info == "NextGame").Value;

            Bot.SendMessage(Channel, 2, true, $"@{Name}, Następna gra to: {NextGame}");
        }
    }

    class GameInfo
    {
        public string GameId { get; set; }
        public string GameName { get; set; }
    }
}

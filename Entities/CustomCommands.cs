using Microsoft.EntityFrameworkCore;
using System;
using System.Timers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PyRZyBot_2._0.Entities
{
    class CustomCommands
    {
        public static List<TimedCommands> Timers = new List<TimedCommands>();
        public static List<int> Weights = new List<int> { 8, 8, 3, 1, 100 };
        public static List<string> Responses = new List<string> { "Tak", "Nie", "Oj nie wiem nie wiem", "xD", "Paaaaanie, bota o to pytasz? litości... 🙄" };

        public static void Command(string Channel, string Name, List<string> Arguments)
        {
            switch (Arguments[1].ToLower())
            {
                case "add":
                    AddCommand(Channel, Name, Arguments);
                    break;

                case "delete":
                    DeleteCommand(Channel, Name, Arguments);
                    break;

                case "edit":
                    EditCommand(Channel, Name, Arguments);
                    break;
            }
        }
        static void AddCommand(string Channel, string Name, List<string> Arguments)
        {
            if (Database.IsBanned(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono użycia komendy !command add użytkownikowi {Name} (Użytkownik Zbanowany)");
                return;
            }
            if (Database.IsTimeouted(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono użycia komendy !command add użytkownikowi {Name} (Użytkownik wykluczony czasOwO)");
                return;
            }
            if (Database.GetAccessLevel(Channel, Name) < (int)AccessLevels.mod)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !command add użytkownikowi {Name} (Brak uprawnień)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Nie posiadasz odpowiednich uprawnień");
                return;
            }
            if (Arguments.Count < 3)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !command add użytkownikowi {Name} (Niewłaściwa liczba argumentów)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Podano niewłaściwą liczbę argumentów (!command add <wywołanie> <odpowiedź>)");
                return;
            }
            if (!Arguments[2].StartsWith('!'))
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !command add użytkownikowi {Name} (Niewłaściwe wywołanie)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Wywołanie komendy musi zaczynać się od '!'");
                return;
            }
            using (var context = new Database())
            {
                var Command = context.ChannelCommands.FirstOrDefault(x => x.Channel == Channel && x.CommandName == Arguments[2]);
                if (Command != null)
                {
                    Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !command add użytkownikowi {Name} (Komenda już istnieje)");
                    Bot.SendMessage(Channel, 0, false, $"@{Name}, Taka komenda już istnieje");
                    return;
                }
                var Alias = context.Aliases.Where(x => x.Channel == Channel && x.Alias == Arguments[2]).Include(x => x.ChannelCommands).FirstOrDefault();
                if (Alias != null)
                {
                    Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !command add użytkownikowi {Name} (Nazwa zajęta przez alias)");
                    Bot.SendMessage(Channel, 0, false, $"@{Name}, Ta nazwa jest zajęta przez alias komendy {Alias.ChannelCommands.CommandName}");
                    return;
                }
                string Response = "";
                if (Arguments.Count > 3)
                {
                    for (int i = 3; i < Arguments.Count; i++)
                    {
                        if (Arguments[i].StartsWith('{'))
                            Arguments[i] = Arguments[i].ToLower();
                    }

                    var StringBuilder = new StringBuilder(Arguments[3]);
                    for (int i = 4; i < Arguments.Count; i++)
                        StringBuilder.Append($" {Arguments[i]}");

                    Response = StringBuilder.ToString();
                }
                Command = new ChannelCommands(Arguments[2].ToLower(), Response, Channel);
                context.Add(Command);
                context.SaveChanges();
                Bot.SendMessage(Channel, 2, true, $"@{Name}, Komenda {Arguments[2]} została dodana");
            }
        }
        static void EditCommand(string Channel, string Name, List<string> Arguments)
        {
            if (Database.IsBanned(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono użycia komendy !command add użytkownikowi {Name} (Użytkownik Zbanowany)");
                return;
            }
            if (Database.IsTimeouted(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono użycia komendy !command add użytkownikowi {Name} (Użytkownik wykluczony czasOwO)");
                return;
            }
        }
        static void DeleteCommand(string Channel, string Name, List<string> Arguments)
        {
            if (Database.IsBanned(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono użycia komendy !command delete użytkownikowi {Name} (Użytkownik Zbanowany)");
                return;
            }
            if (Database.IsTimeouted(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono użycia komendy !command delete użytkownikowi {Name} (Użytkownik wykluczony czasOwO)");
                return;
            }
            if (Database.GetAccessLevel(Channel, Name) < (int)AccessLevels.mod)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !command add użytkownikowi {Name} (Brak uprawnień)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Nie posiadasz odpowiednich uprawnień");
                return;
            }
            if (Arguments.Count != 3)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !command add użytkownikowi {Name} (Niewłaściwa liczba argumentów)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Podano niewłaściwą liczbę argumentów (!command delete <wywołanie>)");
                return;
            }
            if (!Arguments[2].StartsWith('!'))
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !command add użytkownikowi {Name} (Niewłaściwe wywołanie)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Wywołanie komendy musi zaczynać się od '!'");
                return;
            }
            using (var context = new Database())
            {
                var Command = CustomCommands.FindCommand(Arguments[2], Channel);
                if (Command == null)
                {
                    Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !command delete użytkownikowi {Name} (Komenda nie istnieje)");
                    Bot.SendMessage(Channel, 0, false, $"@{Name}, Taka komenda nie istnieje");
                    return;
                }
                if (Database.GetAccessLevel(Channel, Name) < Command.EditLevel)
                {
                    Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !command delete użytkownikowi {Name} (Niewystarczające uprawnienia)");
                    Bot.SendMessage(Channel, 0, false, $"@{Name}, nie posiadasz wystarczających uprawnień");
                    return;
                }
                var SubCommands = context.ChannelCommands.Where(x => x.ParentCommand == Command.Id);
                context.RemoveRange(SubCommands);
                context.Remove(Command);
                context.SaveChanges();
                Bot.SendMessage(Channel, 2, true, $"@{Name}, Komenda {Arguments[2]} została usunięta");
            }
        }

        internal static bool ChannelCommands(string Channel, string Name, List<string> Arguments)
        {
            string CommandName = Arguments[0];
            using (var context = new Database())
            {
                var Command = FindCommand(CommandName, Channel);
                if (Command == null)
                {
                    Bot.LogEvent(Channel, 0, $"Nie znaleziono komendy {CommandName}");
                    return false;
                }//CZY KOMENDA ISTNIEJE
                if (Database.IsBanned(Name, Channel))
                {
                    Bot.LogEvent(Channel, 0, $"Odmówiono użycia komendy {Command.CommandName} użytkownikowi {Name} (Użytkownik Zbanowany)");
                    return false;
                }//CZY UŻYTKOWNIK JEST ZBANOWANY
                if (Database.IsTimeouted(Name, Channel))
                {
                    Bot.LogEvent(Channel, 0, $"Odmówiono użycia komendy {Command.CommandName} użytkownikowi {Name} (Użytkownik wykluczony czasOwO)");
                    return false;
                }//CZY UŻYTKOWNIK JEST WYKLUCZONY CZASOWO
                if (!Command.IsEnabled)
                {
                    Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy {Command.CommandName} użytkownikowi {Name} (Komenda jest wyłączona)");
                    Bot.SendMessage(Channel, 0, true, $"@{Name}, Komenda {Command.CommandName} jest wyłączona)");
                    return false;
                }//CZY KOMENDA JEST WŁĄCZONA
                if (Database.GetAccessLevel(Channel, Name) < Command.AccessLevel)
                {
                    Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy {Command.CommandName} użytkownikowi {Name} (Brak uprawnień)");
                    Bot.SendMessage(Channel, 0, false, $"@{Name}, Nie posiadasz odpowiednich uprawnień");
                    return false;
                }//CZY UŻYTKOWNIK MA UPRAWNIENIA
                if (Command.Cooldown != 0 && Command.LastUsed.AddSeconds(Command.Cooldown) > DateTime.Now)
                {
                    Bot.LogEvent(Channel, 0, $"Odmówiono użycia komendy {Command.CommandName} użytkownikowi {Name} (Komenda na cooldownie, pozostało {(Command.LastUsed.AddSeconds(Command.Cooldown) - DateTime.Now).Seconds}s)");
                    return false;
                }//CZY KOMENDA JEST NA COOLDOWNIE

                Command.TimesUsed++;
                Command.LastUsed = DateTime.Now;
                context.Update(Command);
                context.SaveChanges();

                if (!string.IsNullOrEmpty(Command.Response))
                {
                    string BotResponse = SwapVariables(Command, Name);
                    Bot.SendMessage(Channel, 2, true, BotResponse);
                }

                if (Command.IsComplex)
                    ComplexCommands(Command.CommandName, Channel, Name, Arguments);

                return true;
            }
        }
        static void ComplexCommands(string CommandName, string Channel, string Name, List<string> Arguments)
        {
            switch (CommandName)
            {
                case "!punkty":
                    Points.PointsCommands(Channel, Name, Arguments);
                    break;

                case "!ranking":
                    Points.Leaderboard(Channel);
                    break;

                case "!walcz":
                    Duels.StartDuel(Channel, Name, Arguments);
                    break;

                case "!tak":
                    Duels.AcceptDuel(Channel, Name, Arguments);
                    break;

                case "!nie":
                    Duels.DenyDuel(Channel, Name, Arguments);
                    break;
                
                case "!staty":
                    Duels.Stats(Channel, Name, Arguments);
                    break;

                case "!anuluj":
                    Duels.CancelDuel(Channel, Name, Arguments);
                    break;

                case "czy":
                    {
                        Random Random = new Random();
                        double RandomValue = Random.NextDouble() * Weights.Sum();
                        for (int i = 0; i < Weights.Count; i++)
                        {
                            RandomValue -= Weights[i];
                            if (RandomValue <= 0)
                            {
                                Bot.SendMessage(Channel, 2, true, Responses[i]);
                                return;
                            }
                        }
                    }
                    break;

                default:
                    Errors.UnaccountedForInput((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber(),
                        MethodBase.GetCurrentMethod().DeclaringType.Name, CommandName);
                    Bot.SendMessage(Channel, 1, false, $"Nie znaleziono odnośnika do złożonej komendy!");
                    break;
            }
        }

        static ChannelCommands FindCommand(string CommandName, string Channel)
        {
            using (var context = new Database())
            {
                var Command = context.ChannelCommands.FirstOrDefault(x => x.CommandName == CommandName && x.Channel == Channel);
                if (Command == null)
                {
                    var Alias = context.Aliases.Where(x => x.Channel == Channel && x.Alias == CommandName).Include(x => x.ChannelCommands).FirstOrDefault();
                    if (Alias == null)
                    {
                        return null;
                    }
                    Command = Alias.ChannelCommands;
                }
                return Command;
            }
        }
        static string SwapVariables(ChannelCommands Command, string Name)
        {
            string Count = $"{++Command.TimesUsed}";
            return Command.Response.Replace("{count}", Count)
                                   .Replace("{user}", Name);
        }
        public static void StartTimers(string Channel)
        {
            using (var context = new Database())
            {
                var TimedCommands = context.ChannelCommands.Where(x => x.Channel == Channel && x.Timer != 0);

                foreach (var Command in TimedCommands)
                {
                    var Timer = new Timer(Command.Timer * 60000);
                    Timer.Elapsed += (o, e) => Bot.SendMessage(Channel, 2, true, Command.Response);
                    Timer.AutoReset = true;
                    Timer.Enabled = true;
                    var TimedCommand = new TimedCommands();
                    TimedCommand.Timer = Timer;
                    TimedCommand.Channel = Channel;
                    Timers.Add(TimedCommand);
                }
            }
        }
        public static void StopTimers(string Channel)
        {
            var TimedCommands = Timers.Where(x => x.Channel == Channel).ToList();
            foreach (var Command in TimedCommands)
            {
                Command.Timer.Stop();
                Command.Timer.Dispose();
                Timers.Remove(Command);
            }
        }
    }
}

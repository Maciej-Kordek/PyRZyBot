using PyRZyBot_2._0.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PyRZyBot_2._0
{
    class InternalCommands_Moderation
    {
        public static void Sudo(string Channel, string Name, List<string> Arguments)
        {
            if (Database.IsBanned(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono użycia komendy !sudo użytkownikowi {Name} (Użytkownik Zbanowany)");
                return;
            }
            if (Database.IsTimeouted(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono użycia komendy !sudo użytkownikowi {Name} (Użytkownik wykluczony czasOwO)");
                return;
            }
            if (Database.GetAccessLevel(Channel, Name) < (int)AccessLevels.mod)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !sudo użytkownikowi {Name} (Brak uprawnień)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, nie posiadasz odpowiednich uprawnień");
                return;
            }
            if (Arguments.Count < 3)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !sudo użytkownikowi {Name} (Niewłaściwa liczba argumentów)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, podano niewłaściwą liczbę argumentów (!sudo <użytkownik> <wiadomość>)");
                return;
            }
            using (var context = new Database())
            {
                var User = context.ChatUsers.FirstOrDefault(x => x.Name == Arguments[1].Trim('@') && x.Channel == Channel);
                if (User == null)
                {
                    Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !sudo użytkownikowi {Name} (Nie znaleziono oznaczonego użytkownika)");
                    Bot.SendMessage(Channel, 1, false, $"@{Name}, nie znaleziono użytkownika {Arguments[1].ToLower().Trim('@')}");
                    return;
                }
                if (Database.GetAccessLevel(Channel, Name) < User.AccessLevel)
                {
                    Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !sudo użytkownikowi {Name} (Niewystarczające uprawnienia)");
                    Bot.SendMessage(Channel, 0, false, $"@{Name}, nie posiadasz wystarczających uprawnień");
                    return;
                }
                var StringBuilder = new StringBuilder(Arguments[2]);
                for (int i = 3; i < Arguments.Count; i++)
                    StringBuilder.Append($" {Arguments[i]}");

                string Message = StringBuilder.ToString();

                Bot.LogEvent(Channel, 1, $"Wykonuję komendę !sudo: <{User.Name}> {Message}");
                Bot.CheckMessage(Channel, User.Name, Message);
            }
        }
        public static void Ban(string Channel, string Name, List<string> Arguments)
        {
            if (Database.IsBanned(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono użycia komendy !ban użytkownikowi {Name} (Użytkownik Zbanowany)");
                return;
            }
            if (Database.IsTimeouted(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono użycia komendy !ban użytkownikowi {Name} (Użytkownik wykluczony czasOwO)");
                return;
            }
            if (Database.GetAccessLevel(Channel, Name) < (int)AccessLevels.mod)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !ban użytkownikowi {Name} (Brak uprawnień)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Nie posiadasz odpowiednich uprawnień");
                return;
            }
            if (Arguments.Count != 2)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !ban użytkownikowi {Name} (Niewłaściwa liczba argumentów)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Podano niewłaściwą liczbę argumentów (!ban <użytkownik>)");
                return;
            }
            using (var context = new Database())
            {
                var User = context.ChatUsers.FirstOrDefault(x => x.Channel == Channel && x.Name == Arguments[1].Trim('@'));
                if (User == null)
                {
                    Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !ban użytkownikowi {Name} (Nie znaleziono oznaczonego użytkownika)");
                    Bot.SendMessage(Channel, 1, false, $"@{Name}, Nie znaleziono użytkownika {Arguments[1].ToLower().Trim('@')}");
                    return;
                }
                if (Database.GetAccessLevel(Channel, Name) <= User.AccessLevel)
                {
                    Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !ban użytkownikowi {Name} (Niewystarczające uprawnienia)");
                    Bot.SendMessage(Channel, 0, false, $"@{Name}, Nie posiadasz wystarczających uprawnień");
                    return;
                }
                User.AccessLevel = (int)AccessLevels.banned;
                context.Update(User);
                context.SaveChanges();
                Bot.SendMessage(Channel, 2, true, $"@{Name}, Użytkownik {User.Name} został zbanowany z używania bota");
            }
        }
        public static void SoftBan(string Channel, string Name, List<string> Arguments)
        {
            if (Database.IsBanned(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono użycia komendy !softban użytkownikowi {Name} (Użytkownik Zbanowany)");
                return;
            }
            if (Database.IsTimeouted(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono użycia komendy !softban użytkownikowi {Name} (Użytkownik wykluczony czasOwO)");
                return;
            }
            if (Database.GetAccessLevel(Channel, Name) < (int)AccessLevels.mod)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !softban użytkownikowi {Name} (Brak uprawnień)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Nie posiadasz odpowiednich uprawnień");
                return;
            }
            if (Arguments.Count != 2)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !softban użytkownikowi {Name} (Niewłaściwa liczba argumentów)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Podano niewłaściwą liczbę argumentów (!ban <użytkownik>)");
                return;
            }
            using (var context = new Database())
            {
                var User = context.ChatUsers.FirstOrDefault(x => x.Channel == Channel && x.Name == Arguments[1].Trim('@'));
                if (User == null)
                {
                    Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !softban użytkownikowi {Name} (Nie znaleziono oznaczonego użytkownika)");
                    Bot.SendMessage(Channel, 1, false, $"@{Name}, Nie znaleziono użytkownika {Arguments[1].ToLower().Trim('@')}");
                    return;
                }
                if (Database.GetAccessLevel(Channel, Name) <= User.AccessLevel)
                {
                    Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !softban użytkownikowi {Name} (Niewystarczające uprawnienia)");
                    Bot.SendMessage(Channel, 0, false, $"@{Name}, Nie posiadasz wystarczających uprawnień");
                    return;
                }
                User.AccessLevel = (int)AccessLevels.softbanned;
                context.Update(User);
                context.SaveChanges();
                Bot.SendMessage(Channel, 2, true, $"@{Name}, Funkcjonalność bota została ograniczona użytkownikowi {User.Name}");
            }
        }
        public static void Unban(string Channel, string Name, List<string> Arguments)
        {
            if (Database.IsBanned(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono użycia komendy !unban użytkownikowi {Name} (Użytkownik Zbanowany)");
                return;
            }
            if (Database.IsTimeouted(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono użycia komendy !unban użytkownikowi {Name} (Użytkownik wykluczony czasOwO)");
                return;
            }
            if (Database.GetAccessLevel(Channel, Name) < (int)AccessLevels.mod)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !unban użytkownikowi {Name} (Brak uprawnień)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Nie posiadasz odpowiednich uprawnień");
                return;
            }
            if (Arguments.Count != 2)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !unban użytkownikowi {Name} (Niewłaściwa liczba argumentów)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Podano niewłaściwą liczbę argumentów (!unban <użytkownik>)");
                return;
            }
            using (var context = new Database())
            {
                var User = context.ChatUsers.FirstOrDefault(x => x.Channel == Channel && x.Name == Arguments[1].Trim('@'));
                if (User == null)
                {
                    Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !unban użytkownikowi {Name} (Nie znaleziono oznaczonego użytkownika)");
                    Bot.SendMessage(Channel, 1, false, $"@{Name}, Nie znaleziono użytkownika {Arguments[1].ToLower().Trim('@')}");
                    return;
                }
                if (Database.GetAccessLevel(Channel, Name) <= User.AccessLevel)
                {
                    Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !unban użytkownikowi {Name} (Niewystarczające uprawnienia)");
                    Bot.SendMessage(Channel, 0, false, $"@{Name}, Nie posiadasz wystarczających uprawnień");
                    return;
                }
                User.AccessLevel = (int)AccessLevels.user;
                context.Update(User);
                context.SaveChanges();
                Bot.SendMessage(Channel, 2, true, $"@{Name}, Użytkownikowi {User.Name} zostały przywrócone domyślne uprawnienia");
            }
        }
    }
}

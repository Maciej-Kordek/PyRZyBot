using Microsoft.EntityFrameworkCore;
using PyRZyBot_2._0.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PyRZyBot_2._0
{
    class InternalCommands_Set
    {
        public static void Set(string Channel, string Name, List<string> Arguments)
        {
            if (Arguments.Count() < 1) { return; }

            switch (Arguments[1].ToLower())
            {
                case "nickname":
                    SetNickname(Channel, Name, Arguments);
                    return;

                case "gender":
                    SetGender(Channel, Name, Arguments);
                    return;

                case "duels":
                    SetDuels(Channel, Name, Arguments);
                    return;

                case "maxbet":
                    SetMaxBet(Channel, Name, Arguments);
                    return;

                case "accesslevel":
                    SetAccessLevel(Channel, Name, Arguments);
                    return;

                case "pointsname":
                    SetPointsName(Channel, Name, Arguments);
                    return;

                case "feedback":
                    SetFeedbackLevel(Channel, Name, Arguments);
                    return;

                default:
                    Bot.LogEvent(Channel, 0, $"Nieznana komenda !set {Arguments[1]}");
                    return;
            }
        }
        static void SetNickname(string Channel, string Name, List<string> Arguments)
        {
            if (Database.IsBanned(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono zmiany nickname'u użytkownikowi {Name} (Użytkownik Zbanowany)");
                return;
            }
            if (Database.IsTimeouted(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono zmiany nickname'u użytkownikowi {Name} (Użytkownik wykluczony czasOwO)");
                return;
            }
            if (Database.GetAccessLevel(Channel, Name) < (int)AccessLevels.user || Name == "KyRZy")
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono zmiany nickname'u użytkownikowi {Name} (Brak uprawnień)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Nie posiadasz odpowiednich uprawnień");
                return;
            }

            using (var context = new Database())
            {
                string Nickname = "";
                var User = context.ChatUsers.FirstOrDefault(x => x.Name == Name && x.Channel == Channel);
                if (User == null)
                {
                    Errors.UserNotFound((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber(),
                        MethodBase.GetCurrentMethod().DeclaringType.Name, Name, Channel);
                    Bot.SendMessage(Channel, 1, false, $"Wystąpił niespodziewany błąd!");
                    return;
                }
                if (Arguments.Count > 2)
                {
                    var StringBuilder = new StringBuilder(Arguments[2]);
                    for (int i = 3; i < Arguments.Count; i++)
                        StringBuilder.Append($" {Arguments[i]}");

                    Nickname = StringBuilder.ToString();
                }
                if (!Database.IsNicknamelegal(Channel, Nickname) && !string.IsNullOrEmpty(Nickname))
                {
                    Bot.LogEvent(Channel, 1, $"Odmówiono zmiany nickname'u użytkownikowi {Name} (Niezgodny nickname)");
                    Bot.SendMessage(Channel, 1, false, $"@{Name}, Ta nazwa jest niezgodna z szablonem (4-24 litery, bez znaków specjalnych)");
                    return;
                }
                if (Database.IsNicknameTaken(Channel, Nickname) && !string.IsNullOrEmpty(Nickname))
                {
                    Bot.LogEvent(Channel, 1, $"Odmówiono zmiany nickname'u użytkownikowi {Name} (Zajęty nickname)");
                    Bot.SendMessage(Channel, 0, false, $"@{Name}, Ta nazwa jest zajęta");
                    return;
                }
                User.Nickname = Nickname;
                context.Update(User);
                context.SaveChanges();

                if (!string.IsNullOrEmpty(Nickname))
                {
                    Bot.SendMessage(Channel, 2, true, $"@{Name}, Ustawiono nickname na: {Nickname}");
                }
                else Bot.SendMessage(Channel, 2, true, $"@{Name}, Twój nickname został usunięty");
            }
        }
        static void SetGender(string Channel, string Name, List<string> Arguments)
        {
            if (Database.IsBanned(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono zmiany płci użytkownikowi {Name} (Użytkownik Zbanowany)");
                return;
            }
            if (Database.IsTimeouted(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono zmiany płci użytkownikowi {Name} (Użytkownik wykluczony czasOwO)");
                return;
            }
            if (Database.GetAccessLevel(Channel, Name) < (int)AccessLevels.user)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono zmiany płci użytkownikowi {Name} (Brak uprawnień)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Nie posiadasz odpowiednich uprawnień");
                return;
            }
            if (Arguments.Count != 3)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono zmiany płci użytkownikowi {Name} (Niewłaściwa liczba argumentów)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Podano niewłaściwą liczbę argumentów (!set gender <płeć>)");
                return;
            }
            if (!Enums.IsGenderDefined(Arguments[2].ToLower()))
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono zmiany płci użytkownikowi {Name} (Niezdefiniowana wartość)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Podano niezdefiniowaną wartość (0/female, 1/male, 2/none)");
                return;
            }
            int Gender = (int)Enum.Parse(typeof(GenderEnum), Arguments[2].ToLower());
            Database.SetGender(Name, Gender, Channel);
            Bot.SendMessage(Channel, 2, true, $"@{Name}, Zmieniono płeć na {Enum.GetName(typeof(GenderEnum), Gender)}");

        }
        static void SetMaxBet(string Channel, string Name, List<string> Arguments)
        {
            if (Database.IsBanned(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono zmiany maksymalnej kwoty wyzwań użytkownikowi {Name} (Użytkownik Zbanowany)");
                return;
            }
            if (Database.IsTimeouted(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono zmiany maksymalnej kwoty wyzwań użytkownikowi {Name} (Użytkownik wykluczony czasOwO)");
                return;
            }
            if (Database.GetAccessLevel(Channel, Name) < (int)AccessLevels.user)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono zmiany maksymalnej kwoty wyzwań użytkownikowi {Name} (Brak uprawnień)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Nie posiadasz odpowiednich uprawnień");
                return;
            }
            if (Arguments.Count != 3)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono zmiany maksymalnej kwoty wyzwań użytkownikowi {Name} (Niewłaściwa liczba argumentów)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Podano niewłaściwą liczbę argumentów (!set maxbet <kwota>)");
                return;
            }
            if (!Enums.IsInt(Arguments[2]) || (int.Parse(Arguments[2]) < 100 && int.Parse(Arguments[2]) != -1))
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono zmiany maksymalnej kwoty wyzwań użytkownikowi {Name} (Niewłaściwa maksymalna kwota)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Podano niewłaściwą kwotę (kwota = -1, kwota >= 100)");
                return;
            }
            int MaxBet = int.Parse(Arguments[2]);
            using (var context = new Database())
            {
                var User = context.ChatUsers.Where(x => x.Name == Name).Include(x => x.ChatUsers_S).FirstOrDefault();
                if (User == null)
                {
                    Errors.UserNotFound((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber(),
                                            MethodBase.GetCurrentMethod().DeclaringType.Name, Name, Channel);
                    Bot.SendMessage(Channel, 1, false, $"Wystąpił niespodziewany błąd!");
                    return;
                }
                User.ChatUsers_S.MaxDuelBet = MaxBet;
                context.Update(User);
                context.SaveChanges();
                if (MaxBet != -1)
                    Bot.SendMessage(Channel, 2, true, $"@{Name}, Zmieniono maksymalną kwotę wyzwania na {MaxBet}");
                else Bot.SendMessage(Channel, 2, true, $"@{Name}, Usunięto limit na kwotę wyzwania");
            }
        }
        static void SetDuels(string Channel, string Name, List<string> Arguments)
        {
            if (Database.IsBanned(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono zmiany przyjmowania wyzwań użytkownikowi {Name} (Użytkownik Zbanowany)");
                return;
            }
            if (Database.IsTimeouted(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono zmiany przyjmowania wyzwań użytkownikowi {Name} (Użytkownik wykluczony czasOwO)");
                return;
            }
            if (Database.GetAccessLevel(Channel, Name) < (int)AccessLevels.user)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono zmiany przyjmowania wyzwań użytkownikowi {Name} (Brak uprawnień)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Nie posiadasz odpowiednich uprawnień");
                return;
            }
            if (Arguments.Count != 3)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono zmiany przyjmowania wyzwań użytkownikowi {Name} (Niewłaściwa liczba argumentów)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Podano niewłaściwą liczbę argumentów (!set duels <on/off>)");
                return;
            }
            if (!Enums.IsOnOffDefined(Arguments[2].ToLower()))
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono zmiany przyjmowania wyzwań użytkownikowi {Name} (Niezdefiniowa wartość)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Podano niezdefiniowaną wartość (0/off, 1/on)");
                return;
            }
            using (var context = new Database())
            {
                var User = context.ChatUsers.Where(x => x.Name == Name).Include(x => x.ChatUsers_S).FirstOrDefault();
                if (User == null)
                {
                    Errors.UserNotFound((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber(),
                                            MethodBase.GetCurrentMethod().DeclaringType.Name, Name, Channel);
                    Bot.SendMessage(Channel, 1, false, $"Wystąpił niespodziewany błąd!");
                    return;
                }

                int DuelsStatus = (int)Enum.Parse(typeof(OnOff), Arguments[2].ToLower());
                if (DuelsStatus == 1)
                {
                    User.ChatUsers_S.AcceptsDuels = true;
                    Bot.SendMessage(Channel, 2, true, $"@{Name}, Włączono przyjmowanie wyzwań");
                }
                else
                {
                    User.ChatUsers_S.AcceptsDuels = false;
                    Bot.SendMessage(Channel, 2, true, $"@{Name}, Wyłączono przyjmowanie wyzwań");
                }
                context.Update(User);
                context.SaveChanges();
            }
        }
        static void SetAccessLevel(string Channel, string Name, List<string> Arguments)
        {
            if (Database.IsBanned(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono zmiany uprawnień użytkownikowi {Name} (Użytkownik Zbanowany)");
                return;
            }
            if (Database.IsTimeouted(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono zmiany uprawnień użytkownikowi {Name} (Użytkownik wykluczony czasOwO)");
                return;
            }
            if (Database.GetAccessLevel(Channel, Name) < (int)AccessLevels.mod)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono zmiany uprawnień użytkownikowi {Name} (Brak uprawnień)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Nie posiadasz odpowiednich uprawnień");
                return;
            }
            if (Arguments.Count != 4)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono zmiany uprawnień użytkownikowi {Name} (Niewłaściwa liczba argumentów)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Podano niewłaściwą liczbę argumentów (!set accesslevel <użytkownik> <poziom uprawnień>)");
                return;
            }
            if (!Enums.IsAccessLevelDefined(Arguments[3].ToLower()))
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono zmiany uprawnień użytkownikowi {Name} (Niezdefiniowana wartość)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Podano niezdefiniowaną wartość (0/banned, 1/softbanned, 2/user, 3/vip, 4/trusted, 5/mod, 6/headmod, 7/broadcaster)");
                return;
            }
            int AccessLevel = (int)Enum.Parse(typeof(AccessLevels), Arguments[3].ToLower());
            using (var context = new Database())
            {
                var User = context.ChatUsers.FirstOrDefault(x => x.Channel == Channel && x.Name == Arguments[2].Trim('@'));
                if (User == null)
                {
                    Bot.LogEvent(Channel, 1, $"Odmówiono zmiany uprawnień użytkownikowi {Name} (Nie znaleziono oznaczonego użytkownika)");
                    Bot.SendMessage(Channel, 1, false, $"@{Name}, Nie znaleziono użytkownika {Arguments[2].ToLower().Trim('@')}");
                    return;
                }
                if (Database.GetAccessLevel(Channel, Name) <= User.AccessLevel)
                {
                    Bot.LogEvent(Channel, 1, $"Odmówiono zmiany uprawnień użytkownikowi {Name} (Niewystarczające uprawnienia)");
                    Bot.SendMessage(Channel, 0, false, $"@{Name}, Nie posiadasz wystarczających uprawnień");
                    return;
                }
                Database.SetAccessLevel(User.Name, AccessLevel, Channel);
                Bot.SendMessage(Channel, 2, true, $"@{Name}, Zmieniono uprawnienia użytkownika {User.Name} na {Enum.GetName(typeof(AccessLevels), AccessLevel)}");
            }
        }
        static void SetPointsName(string Channel, string Name, List<string> Arguments)
        {
            if (Database.IsBanned(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono zmiany nazwy punktów użytkownikowi {Name} (Użytkownik Zbanowany)");
                return;
            }
            if (Database.IsTimeouted(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono zmiany nazwy punktów użytkownikowi {Name} (Użytkownik wykluczony czasOwO)");
                return;
            }
            if (Database.GetAccessLevel(Channel, Name) < (int)AccessLevels.mod)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono zmiany nazwy punktów użytkownikowi {Name} (Brak uprawnień)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Nie posiadasz odpowiednich uprawnień");
                return;
            }
            if (Arguments.Count != 4)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono zmiany nazwy punktów użytkownikowi {Name} (Niewłaściwa liczba argumentów)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Podano niewłaściwą liczbę argumentów (!set pointsname <nazwa w liczbie mnogiej> <wywołanie nagrody>)");
                return;
            }
            using (var context = new Database())
            {
                var PointsName = context.ChannelInfo.FirstOrDefault(x => x.Channel == Channel && x.Info == "PointsName");
                var RedeemOn = context.ChannelInfo.FirstOrDefault(x => x.Channel == Channel && x.Info == "RedeemOn");
                var Alias = context.Aliases.FirstOrDefault(x => x.Channel == Channel && x.Alias == $"!{PointsName.Value}");
                if (PointsName == null || RedeemOn == null)
                {
                    Errors.ElementNotFound((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber(),
                        MethodBase.GetCurrentMethod().DeclaringType.Name, "PointsName lub RedeemOn", Channel);
                    Bot.SendMessage(Channel, 1, false, $"Wystąpił niespodziewany błąd!");
                    return;
                }

                if (Alias == null)
                {
                    var PointsCommand = context.ChannelCommands.FirstOrDefault(x => x.Channel == Channel && x.CommandName == "!punkty");
                    if (PointsCommand == null)
                    {
                        PointsCommand = new ChannelCommands("!punkty", "", Channel);
                        PointsCommand.IsComplex = true;
                        context.Add(PointsCommand);
                        context.SaveChanges();
                    }
                    Alias = new Aliases(PointsCommand.Id, "", Channel);
                    context.Add(Alias);
                    context.SaveChanges();
                }

                Alias.Alias = $"!{Arguments[2].ToLower()}";
                context.Update(Alias);

                PointsName.Value = Arguments[2].ToLower();
                RedeemOn.Value = Arguments[3].ToLower();
                context.Update(PointsName);
                context.Update(RedeemOn);
                context.SaveChanges();
                Bot.SendMessage(Channel, 2, true, $"@{Name}, Zmieniono nazwę punktów na {PointsName.Value}");
            }
        }
        static void SetFeedbackLevel(string Channel, string Name, List<string> Arguments)
        {
            if (Database.IsBanned(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono zmiany poziomu feedbacku użytkownikowi {Name} (Użytkownik Zbanowany)");
                return;
            }
            if (Database.IsTimeouted(Name, Channel))
            {
                Bot.LogEvent(Channel, 0, $"Odmówiono zmiany poziomu feedbacku użytkownikowi {Name} (Użytkownik wykluczony czasOwO)");
                return;
            }
            if (Database.GetAccessLevel(Channel, Name) < (int)AccessLevels.mod)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono zmiany poziomu feedbacku użytkownikowi {Name} (Brak uprawnień)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Nie posiadasz odpowiednich uprawnień");
                return;
            }
            if (Arguments.Count != 3)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono zmiany poziomu feedbacku użytkownikowi {Name} (Niewłaściwa liczba argumentów)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Podano niewłaściwą liczbę argumentów (!set feedback <poziom feedbacku>)");
                return;
            }
            if (!Enums.IsFeedbackLevelDefined(Arguments[2]))
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono zmiany poziomu feedbacku użytkownikowi {Name} (Niezdefiniowana wartość)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Podano niezdefiniowaną wartość (0/least, 1/normal, 2/everything)");
                return;
            }
            int FeedbackLevel = (int)Enum.Parse(typeof(FeedbackLevels), Arguments[2].ToLower());
            using (var context = new Database())
            {
                var CurrentFeedbackLevel = context.ChannelInfo.FirstOrDefault(x => x.Channel == Channel && x.Info == "ChatFeedback");
                CurrentFeedbackLevel.Value = FeedbackLevel.ToString();
                context.Update(CurrentFeedbackLevel);
                context.SaveChanges();
                Bot.SendMessage(Channel, 2, true, $"@{Name}, Zmieniono poziom feedbacku na {Enum.GetName(typeof(FeedbackLevels), FeedbackLevel)}");
            }
        }

    }
}

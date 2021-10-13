using Microsoft.EntityFrameworkCore;
using PyRZyBot_2._0.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Timers;

namespace PyRZyBot_2._0
{
    class Points
    {
        static Timer RewardMessageCD = new Timer(500);
        static Dictionary<string, int> AmmountRedeemed = new Dictionary<string, int>();
        static Dictionary<string, string> ChannelRedeemedOn = new Dictionary<string, string>();
        static Dictionary<string, DateTime> WaitingMessages = new Dictionary<string, DateTime>();

        public static void OnPointsRedeemed(string Name, int Cost, string Channel)
        {
            using (var context = new Database())
            {
                var User = context.ChatUsers.Where(x => x.Name == Name).Include(x => x.ChatUsers_S).FirstOrDefault();
                if (User == null)
                {
                    Bot.LogEvent(Channel, 2, $"Użytkownikowi {Name} nie dodano {PointsEnding(Channel, Cost)}!");
                    Bot.SendMessage(Channel, 2, false, $"@{Name}, Nie dodano punktów! Nie znaleziono Cię w bazie!");
                    return;
                }
                User.ChatUsers_S.Points += Cost;
                context.Update(User);
                context.SaveChanges();
            }

            List<string> Waiting = new List<string>(WaitingMessages.Keys);
            if (!Waiting.Contains(Name))
            {
                WaitingMessages.Add(Name, DateTime.Now);
                ChannelRedeemedOn.Add(Name, Channel);
                AmmountRedeemed.Add(Name, Cost);
            }
            else
            {
                WaitingMessages[Name] = DateTime.Now;
                AmmountRedeemed[Name] += Cost;
            }

            if (!RewardMessageCD.Enabled)
            {
                RewardMessageCD.Enabled = true;
                RewardMessageCD.AutoReset = true;
                RewardMessageCD.Elapsed += CheckRewardMessage;
            }
        }
        static void CheckRewardMessage(Object source, ElapsedEventArgs e)
        {
            List<string> Waiting = new List<string>(WaitingMessages.Keys);

            foreach (string Name in Waiting)
            {
                if (WaitingMessages[Name].AddSeconds(3) < DateTime.Now)
                {
                    Bot.LogEvent(ChannelRedeemedOn[Name], 2, $"Użytkownikowi {Name} dodano {PointsEnding(ChannelRedeemedOn[Name], AmmountRedeemed[Name])}");
                    Bot.SendMessage(ChannelRedeemedOn[Name], 2, false, $"{Enums.GenderSpecific(ChannelRedeemedOn[Name], Name, "odebrał")} {PointsEnding(ChannelRedeemedOn[Name], AmmountRedeemed[Name])}");
                    WaitingMessages.Remove(Name);
                    AmmountRedeemed.Remove(Name);
                    ChannelRedeemedOn.Remove(Name);
                    if (WaitingMessages.Count == 0)
                    { RewardMessageCD.Stop(); }
                }
            }
        }

        public static void PointsMenu(string Channel, string Name, List<string> Arguments)
        {
            switch (Arguments.Count)
            {
                case 1:
                    CheckSelfPoints(Channel, Name);
                    return;

                default:
                    switch (Arguments[1].ToLower())
                    {
                        case "add":
                            AddPoints();
                            return;

                        case "set":
                            SetPoints();
                            return;

                        case "remove":
                            RemovePoints();
                            return;

                        case "give":
                            GivePoints(Channel, Name, Arguments);
                            return;

                        default:
                            CheckOthersPoints(Channel, Name, Arguments);
                            return;
                    }
            }
        }
        static void CheckSelfPoints(string Channel, string Name)
        {
            using (var context = new Database())
            {
                var User = context.ChatUsers.Where(x => x.Channel == Channel && x.Name == Name).Include(x => x.ChatUsers_S).FirstOrDefault();
                if (User == null)
                {
                    Errors.UserNotFound((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber(),
                        MethodBase.GetCurrentMethod().DeclaringType.Name, Name, Channel);
                    Bot.SendMessage(Channel, 1, false, $"Wystąpił niespodziewany błąd!");
                    return;
                }
                int Points = User.ChatUsers_S.Points;
                var Leaderboard = context.ChatUsers.Where(x => x.Channel == Channel && x.ChatUsers_S.ToRank == true).OrderByDescending(x => x.ChatUsers_S.Points).Select(x => x.Name).ToList();
                int Rank = Leaderboard.IndexOf(Name);
                string Message = $"@{Name}, masz {PointsEnding(Channel, Points)}";

                if (Rank != -1)
                    Message += $" i jesteś na {++Rank}. miejscu!";

                Bot.SendMessage(Channel, 2, true, Message);
            }
        }
        static void CheckOthersPoints(string Channel, string Name, List<string> Arguments)
        {
            if (Arguments.Count != 2)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !points <użytkownik> użytkownikowi {Name} (Niewłaściwa liczba argumentów)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, podano niewłaściwą liczbę argumentów (!points <użytkownik>)");
                return;
            }
            string AtName = Arguments[1].ToLower().Trim('@');
            if (Name.ToLower() == AtName)
            {
                CheckSelfPoints(Channel, Name);
                return;
            }
            using (var context = new Database())
            {
                var User = context.ChatUsers.Where(x => x.Name == AtName).Include(x => x.ChatUsers_S).FirstOrDefault();
                if (User == null)
                {
                    Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !points <użytkownik> użytkownikowi {Name} (Nie znaleziono oznaczonego użytkownika)");
                    Bot.SendMessage(Channel, 1, true, $"@{Name}, nie znaleziono użytkownika {AtName}");
                    return;
                }
                int Points = User.ChatUsers_S.Points;
                var Leaderboard = context.ChatUsers.Where(x => x.Channel == Channel && x.ChatUsers_S.ToRank == true).OrderByDescending(x => x.ChatUsers_S.Points).Select(x => x.Name).ToList();
                int Rank = Leaderboard.IndexOf(User.Name);
                string Message = $"@{Name}, {User.Name} ma {PointsEnding(Channel, Points)}";

                if (Rank != -1)
                    Message += $" i jest na {++Rank}. miejscu!";

                Bot.SendMessage(Channel, 2, true, Message);
            }
        }
        static void AddPoints() { }
        static void SetPoints() { }
        static void RemovePoints() { }
        static void GivePoints(string Channel, string Name, List<string> Arguments)
        {
            if(Arguments.Count != 4)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !points give użytkownikowi {Name} (Niewłaściwa liczba argumentów)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, podano niewłaściwą liczbę argumentów (!points give <użytkownik> <kwota>)");
                return;
            }

            using (var context = new Database())
            {
                var Gifter = context.ChatUsers.Where(x => x.Channel == Channel && x.Name == Name).Include(x => x.ChatUsers_S).FirstOrDefault();
                var Reciever = context.ChatUsers.Where(x => x.Channel == Channel && x.Name == Arguments[1].Trim('@')).Include(x => x.ChatUsers_S).FirstOrDefault();

                if (Gifter == null)
                {
                    Errors.UserNotFound((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber(),
                        MethodBase.GetCurrentMethod().DeclaringType.Name, Name, Channel);
                    Bot.SendMessage(Channel, 1, false, $"Wystąpił niespodziewany błąd!");
                    return;
                }

                if (Reciever == null)
                {
                    Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !punkty give użytkownikowi {Name} (Nie znaleziono oznaczonego użytkownika)");
                    Bot.SendMessage(Channel, 0, false, $"@{Name}, Nie znaleziono użytkownika {Arguments[1].Trim('@')}");
                    return;
                }

                if (Gifter.ChatUsers_S.DuelId != 0)
                {
                    if (!Duels.DuelList.ContainsKey(Gifter.ChatUsers_S.DuelId))
                    {
                        Errors.ElementNotFound((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber(),
                            MethodBase.GetCurrentMethod().DeclaringType.Name, "Duel w liście walk", Channel);
                        Gifter.ChatUsers_S.DuelId = 0;
                        context.Update(Gifter);
                        context.SaveChanges();
                    }
                    else
                    {
                        Duels Duel = Duels.DuelList[Gifter.ChatUsers_S.DuelId];

                        if (Duel.Expiration < DateTime.Now)
                        {
                            Duels.DuelList.Remove(Gifter.ChatUsers_S.DuelId);
                            Gifter.ChatUsers_S.DuelId = 0;
                            context.Update(Gifter);
                            context.SaveChanges();
                        }
                        else
                        {

                            Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !punkty give użytkownikowi {Name} (Użytkownik zapisany na walkę)");
                            Bot.SendMessage(Channel, 0, false, $"@{Name}, Możesz nie mieć wystarczająco pyr po pojedynku");
                            return;
                        }
                    }
                }


            }
        }

        public static void Leaderboard(string Channel)
        {
            using (var context = new Database())
            {
                var Ranking = context.ChatUsers.Where(x => x.Channel == Channel && x.ChatUsers_S.ToRank == true).OrderByDescending(x => x.ChatUsers_S.Points).Select(x => new { Name = x.Name, Points = x.ChatUsers_S.Points }).ToList();
                List<string> RankMessage = new List<string>();
                int Nr = 0;
                foreach (var User in Ranking)
                {
                    if (Nr >= 5) { break; }
                    Nr++;
                    RankMessage.Add($"#{Nr} {User.Name}-{User.Points}");
                }
                Bot.SendMessage(Channel, 2, true, string.Join(" | ", RankMessage));
            }
        }

        public static string PointsEnding(string Channel, int Ammount)
        {
            string PointsName;
            using (var context = new Database())
            { PointsName = context.ChannelInfo.FirstOrDefault(x => x.Info == "PointsName" && x.Channel == Channel).Value; }

            switch (PointsName)
            {
                case "pyry":
                    return PyryEnding(Ammount);
                case "kyrze":
                    return KyRZeEnding(Ammount);
                default:
                    return $"{PointsName} w ilości {Ammount}";
            }
        }
        static string PyryEnding(int Ammount)
        {
            if (Ammount == 1)
                return $"{Ammount} pyrę";

            if (Ammount % 100 > 10 && Ammount % 100 < 20)
                return $"{Ammount} pyr";

            switch (Ammount % 10)
            {
                case 2:
                case 3:
                case 4:
                    return $"{Ammount} pyry";

                default:
                    return $"{Ammount} pyr";
            }
        }
        static string KyRZeEnding(int Ammount)
        {
            if (Ammount == 1)
                return $"{Ammount} KyRZego";

            if (Ammount % 100 > 10 && Ammount % 100 < 20)
                return $"{Ammount} KyRZych";

            switch (Ammount % 10)
            {
                case 2:
                case 3:
                case 4:
                    return $"{Ammount} KyRZe";

                default:
                    return $"{Ammount} KyRZych";
            }
        }
    }
}

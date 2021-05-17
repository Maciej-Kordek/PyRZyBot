using Microsoft.EntityFrameworkCore;
using PyRZyBot_2._0.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PyRZyBot_2._0
{
    class Duels
    {
        public Duels(int attackerid, int defenderid, int pointspool)
        {
            DuelId = attackerid;

            AttackerId = attackerid;

            DefenderId = defenderid;

            PointsPool = pointspool;

            Expiration = DateTime.Now.AddMinutes(2);
        }
        public int DuelId { get; set; }
        public int AttackerId { get; set; }
        public int DefenderId { get; set; }
        public int PointsPool { get; set; }
        public DateTime Expiration { get; set; }

        static Dictionary<int, Duels> DuelList = new Dictionary<int, Duels>();

        internal static void StartDuel(string Channel, string Name, List<string> Arguments)
        {
            if (Arguments.Count < 2 || Arguments.Count > 3)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !walcz użytkownikowi {Name} (Niewłaściwa liczba argumentów)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Podano niewłaściwą liczbę argumentów (!walcz <użytkownik> <kwota>)");
                return;
            }
            using (var context = new Database())
            {
                var Attacker = context.ChatUsers.Where(x => x.Channel == Channel && x.Name == Name).Include(x => x.ChatUsers_S).FirstOrDefault();
                var Defender = context.ChatUsers.Where(x => x.Channel == Channel && x.Name == Arguments[1].Trim('@')).Include(x => x.ChatUsers_S).FirstOrDefault();

                if (Attacker == null)
                {
                    Errors.UserNotFound((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber(),
                        MethodBase.GetCurrentMethod().DeclaringType.Name, Name, Channel);
                    Bot.SendMessage(Channel, 1, false, $"Wystąpił niespodziewany błąd!");
                    return;
                }

                if (Defender == null)
                {
                    Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !walcz użytkownikowi {Name} (Nie znaleziono oznaczonego użytkownika)");
                    Bot.SendMessage(Channel, 0, false, $"@{Name}, Nie znaleziono użytkownika {Arguments[1].Trim('@')}");
                    return;
                }

                if (Database.IsTimeouted(Name, Channel))
                {
                    Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !walcz użytkownikowi {Name} (Oznaczony użytkownik wykluczony czasOwO)");
                    Bot.SendMessage(Channel, 1, false, $"@{Name}, {Database.GetNickname(Channel, Defender.Name)} jest wykluczony czasOwO");
                    return;
                }

                if (Defender.AccessLevel < (int)AccessLevels.user)
                {
                    Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !walcz użytkownikowi {Name} (Oznaczony użytkownik jest zbanowany)");
                    Bot.SendMessage(Channel, 1, false, $"@{Name}, {Database.GetNickname(Channel, Defender.Name)} jest zbanowany");
                    return;
                }

                if (Attacker.TwitchId == Defender.TwitchId)
                {
                    Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !walcz użytkownikowi {Name} (...)");
                    Bot.SendMessage(Channel, 1, false, $"@{Name}, ...");
                    return;
                }

                if (Attacker.ChatUsers_S.DuelId != 0 && DuelList[Attacker.ChatUsers_S.DuelId].Expiration > DateTime.Now)
                {
                    Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !walcz użytkownikowi {Name} (Użytkownik jest zapisany na pojedynek)");
                    Bot.SendMessage(Channel, 1, false, $"@{Name}, Już jesteś zapisany pojedynek");
                    return;
                }

                if (Defender.ChatUsers_S.DuelId != 0 && DuelList[Defender.ChatUsers_S.DuelId].Expiration > DateTime.Now)
                {
                    Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !walcz użytkownikowi {Name} (Oznaczony użytkownik jest zapisany na pojedynek)");
                    Bot.SendMessage(Channel, 1, false, $"@{Name}, {Database.GetNickname(Channel, Defender.Name)} jest zapisany na inny pojedynek");
                    return;
                }

                if (!Defender.IsOnline)
                {
                    Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !walcz użytkownikowi {Name} (Oznaczonego użytkownika nie ma na czacie)");
                    Bot.SendMessage(Channel, 1, false, $"@{Name}, {Defender.Name} jest offline");
                    return;
                }

                if (!Attacker.ChatUsers_S.AcceptsDuels)
                {
                    Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !walcz użytkownikowi {Name} (Użytkownik nie przyjmuje wyzwań)");
                    Bot.SendMessage(Channel, 1, false, $"@{Name}, Nie możesz rozpocząć pojedynku mając wyłączone walki");
                    return;
                }

                if (!Defender.ChatUsers_S.AcceptsDuels)
                {
                    Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !walcz użytkownikowi {Name} (Oznaczony użytkownik nie przyjmuje wyzwań)");
                    Bot.SendMessage(Channel, 1, false, $"@{Name}, {Database.GetNickname(Channel, Defender.Name)} nie przyjmuje wyzwań");
                    return;
                }

                int PointsPool = 0;
                if (Arguments.Count == 3)
                {
                    if (!Enums.IsInt(Arguments[2]) || int.Parse(Arguments[2]) < 0)
                    {
                        Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !walcz użytkownikowi {Name} (Kwota nie jest liczbą naturalną)");
                        Bot.SendMessage(Channel, 1, false, $"@{Name}, Podana kwota nie jest liczbą naturalną");
                        return;
                    }
                    PointsPool = int.Parse(Arguments[2]);

                    if (PointsPool > Attacker.ChatUsers_S.Points)
                    {
                        Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !walcz użytkownikowi {Name} (Użytkownik ma za mało punktów)");
                        Bot.SendMessage(Channel, 1, false, $"@{Name}, Nie posiadasz tyle punktów");
                        return;
                    }

                    if (Attacker.ChatUsers_S.MaxDuelBet != -1 && PointsPool > Attacker.ChatUsers_S.MaxDuelBet)
                    {
                        Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !walcz użytkownikowi {Name} (Kwota wykracza poza maksymalną ustawioną wartość)");
                        Bot.SendMessage(Channel, 1, false, $"@{Name}, Podana kwota wykracza poza Twój ustalony limit");
                        return;
                    }

                    if (PointsPool > Defender.ChatUsers_S.Points)
                    {
                        Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !walcz użytkownikowi {Name} (Oznaczony użytkownik ma za mało punktów)");
                        Bot.SendMessage(Channel, 1, false, $"@{Name}, {Database.GetNickname(Channel, Defender.Name)} nie posiada tyle punktów");
                        return;
                    }

                    if (Defender.ChatUsers_S.MaxDuelBet != -1 && PointsPool > Defender.ChatUsers_S.MaxDuelBet)
                    {
                        Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !walcz użytkownikowi {Name} (Kwota wykracza poza maksymalną ustawioną wartość oznaczonego użytkownika)");
                        Bot.SendMessage(Channel, 1, false, $"@{Name}, {Database.GetNickname(Channel, Defender.Name)} przyjmuje wyzwania do {Defender.ChatUsers_S.MaxDuelBet}");
                        return;
                    }
                }

                if (Attacker.ChatUsers_S.DuelId != 0)
                {
                    if (DuelList.ContainsKey(Attacker.ChatUsers_S.DuelId))
                        DuelList.Remove(Attacker.ChatUsers_S.DuelId);

                    var OldDefender = context.ChatUsers_S.FirstOrDefault(x => x.DuelId == Attacker.ChatUsers_S.DuelId && x.TwitchId != Attacker.TwitchId);
                    if (OldDefender != null)
                    {
                        OldDefender.DuelId = 0;
                        context.Update(OldDefender);
                        context.SaveChanges();
                    }
                }

                if (Defender.ChatUsers_S.DuelId != 0)
                {
                    if (DuelList.ContainsKey(Defender.ChatUsers_S.DuelId))
                        DuelList.Remove(Defender.ChatUsers_S.DuelId);

                    var OldAttacker = context.ChatUsers_S.FirstOrDefault(x => x.DuelId == Defender.ChatUsers_S.DuelId && x.TwitchId != Defender.TwitchId);
                    if (OldAttacker != null)
                    {
                        OldAttacker.DuelId = 0;
                        context.Update(OldAttacker);
                        context.SaveChanges();
                    }
                }

                Duels Duel = new Duels(Attacker.TwitchId, Defender.TwitchId, PointsPool);
                DuelList.Add(Attacker.TwitchId, Duel);

                Attacker.ChatUsers_S.DuelId = Attacker.TwitchId;
                Defender.ChatUsers_S.DuelId = Attacker.TwitchId;
                context.Update(Attacker);
                context.Update(Defender);
                context.SaveChanges();

                string Message = $"GivePLZ SirSword SirShield TakeNRG @{Defender.Name}, {Enums.GenderSpecific(Channel, Name, "wyzwał")} Cię na ";
                Message += PointsPool == 0 ? "towarzyski pojedynek! " : $"pojedynek o {Points.PointsEnding(Channel, PointsPool)}! ";
                if (PointsPool == 6969) { Message += "Double Nice! "; }
                if (PointsPool == 69) { Message += "Nice! "; }
                Message += "Czy podejmiesz się walki? Użyj !tak albo !nie ";

                Bot.SendMessage(Channel, 2, true, Message);
            }
        }
        internal static void AcceptDuel(string Channel, string Name, List<string> Arguments)
        {
            if (Arguments.Count > 1)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !tak użytkownikowi {Name} (Niewłaściwa liczba argumentów)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Podano niewłaściwą liczbę argumentów (!tak)... To nie takie trudne...");
                return;
            }

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

                if (User.ChatUsers_S.DuelId == 0)
                {
                    Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !tak użytkownikowi {Name} (Użytkownik nie był wyzwany na pojedynek)");
                    Bot.SendMessage(Channel, 0, false, $"@{Name}, {Enums.GenderSpecific(Channel, Name, "Nie byłeś")} wyzwany na żaden pojedynek");
                    return;
                }

                if (User.ChatUsers_S.DuelId == User.TwitchId)
                {
                    Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !tak użytkownikowi {Name} (Użytkownik rozpoczął ten pojedynek)");
                    Bot.SendMessage(Channel, 1, false, $"@{Name}, Nie możesz rozpocząć tego pojedynku");
                    return;
                }

                if (!DuelList.ContainsKey(User.ChatUsers_S.DuelId))
                {
                    Errors.ElementNotFound((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber(),
                        MethodBase.GetCurrentMethod().DeclaringType.Name, "Duel w liście", Channel);
                    Bot.SendMessage(Channel, 1, false, $"Wystąpił niespodziewany błąd!");
                    User.ChatUsers_S.DuelId = 0;
                    context.Update(User);
                    context.SaveChanges();
                    return;
                }

                Duels Duel = DuelList[User.ChatUsers_S.DuelId];
                DuelList.Remove(User.ChatUsers_S.DuelId);
                var Attacker = context.ChatUsers.Where(x => x.TwitchId == Duel.AttackerId).Include(x => x.ChatUsers_S).FirstOrDefault();
                var Defender = context.ChatUsers.Where(x => x.TwitchId == Duel.DefenderId).Include(x => x.ChatUsers_S).FirstOrDefault();
                Attacker.ChatUsers_S.LastDuel = DateTime.Now;
                context.Update(Attacker);
                context.SaveChanges();

                if (Attacker == null || Defender == null)
                {
                    Errors.UserNotFound((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber(),
                        MethodBase.GetCurrentMethod().DeclaringType.Name, Name, Channel);
                    Bot.SendMessage(Channel, 1, false, $"Wystąpił niespodziewany błąd!");
                    return;
                }

                var ChannelDuelsPlayed = context.ChannelInfo.FirstOrDefault(x => x.Channel == Channel && x.Info == "DuelsPlayed");
                int DuelsPlayed = int.Parse(ChannelDuelsPlayed.Value);
                ChannelDuelsPlayed.Value = $"{++DuelsPlayed}";
                context.Update(ChannelDuelsPlayed);
                context.SaveChanges();

                ChatUsers Winner;
                ChatUsers Loser;
                string Message = "";

                Random Random = new Random();
                switch (Random.Next(2))
                {
                    case 0:
                        {
                            Winner = Attacker;
                            Loser = Defender;
                            var ChannelAttackerWins = context.ChannelInfo.FirstOrDefault(x => x.Channel == Channel && x.Info == "AttWin");
                            int AttWin = int.Parse(ChannelAttackerWins.Value);
                            ChannelAttackerWins.Value = $"{++AttWin}";
                            context.Update(ChannelAttackerWins);
                            context.SaveChanges();
                            Message += "GivePLZ SirSword ";
                        }
                        break;
                    case 1:
                        {
                            Winner = Defender;
                            Loser = Attacker;
                            var ChannelDefenderWins = context.ChannelInfo.FirstOrDefault(x => x.Channel == Channel && x.Info == "DefWin");
                            int DefWin = int.Parse(ChannelDefenderWins.Value);
                            ChannelDefenderWins.Value = $"{++DefWin}";
                            context.Update(ChannelDefenderWins);
                            context.SaveChanges();
                            Message += "SirShield TakeNRG ";
                        }
                        break;
                    default:
                        Errors.UnexpectedInput((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber(),
                            MethodBase.GetCurrentMethod().DeclaringType.Name, Name, "");
                        Bot.SendMessage(Channel, 1, false, $"Wystąpił niespodziewany błąd!");
                        return;
                }

                FinishFight(Winner.ChatUsers_S, Loser.ChatUsers_S, Duel);

                Message += $"{Enums.GenderSpecific(Channel, Winner.Name, "wygrał")} ";
                Message += Duel.PointsPool > 0 ? $"pojedynek o {Points.PointsEnding(Channel, Duel.PointsPool)} " : "towarzyski pojedynek ";
                Message += $"z {Loser.Name}!";
                if (Winner.ChatUsers_S.Streak > 2)
                {
                    Message += $" {Enums.GenderSpecific(Channel, Winner.Name, "wygrał")} {Winner.ChatUsers_S.Streak} razy z rzędu ";
                    for (int i = 0; i < Winner.ChatUsers_S.Streak; i++)
                        Message += "PogChamp ";
                }
                else if (Loser.ChatUsers_S.Streak < -2)
                {
                    Message += $" {Enums.GenderSpecific(Channel, Loser.Name, "przegrał")} {Loser.ChatUsers_S.Streak * -1} razy z rzędu x";
                    for (int i = 0; i > Loser.ChatUsers_S.Streak; i--)
                        Message += "D";
                }
                Bot.SendMessage(Channel, 2, true, Message);
            }
        }
        internal static void DenyDuel(string Channel, string Name, List<string> Arguments)
        {
            if (Arguments.Count > 1)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !nie użytkownikowi {Name} (Niewłaściwa liczba argumentów)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Podano niewłaściwą liczbę argumentów (!nie)... To nie takie trudne...");
                return;
            }

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

                if (User.ChatUsers_S.DuelId == 0)
                {
                    Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !nie użytkownikowi {Name} (Użytkownik nie był wyzwany na pojedynek)");
                    Bot.SendMessage(Channel, 0, false, $"@{Name}, Nie {Enums.GenderSpecific(Channel, Name, "byłeś wyzwany")} na żaden pojedynek");
                    return;
                }

                if (!DuelList.ContainsKey(User.ChatUsers_S.DuelId))
                {
                    Errors.ElementNotFound((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber(),
                        MethodBase.GetCurrentMethod().DeclaringType.Name, "Duel w liście walk", Channel);
                    Bot.SendMessage(Channel, 1, false, $"Wystąpił niespodziewany błąd!");
                    User.ChatUsers_S.DuelId = 0;
                    context.Update(User);
                    context.SaveChanges();
                    return;
                }

                if (User.ChatUsers_S.DuelId == User.TwitchId)
                {
                    CancelDuel(Channel, Name, Arguments);
                    return;
                }

                Duels Duel = DuelList[User.ChatUsers_S.DuelId];
                DuelList.Remove(User.ChatUsers_S.DuelId);

                var Attacker = context.ChatUsers.Where(x => x.Channel == Channel && x.TwitchId == Duel.AttackerId).Include(x => x.ChatUsers_S).FirstOrDefault();
                var Defender = context.ChatUsers.Where(x => x.Channel == Channel && x.TwitchId == Duel.DefenderId).Include(x => x.ChatUsers_S).FirstOrDefault();

                if (Duel.Expiration < DateTime.Now)
                {
                    Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !nie użytkownikowi {Name} (Pojedynek wygasł)");
                    Bot.SendMessage(Channel, 0, false, $"@{Name}, Nie {Enums.GenderSpecific(Channel, Name, "byłeś wyzwany")} na żaden pojedynek");

                    Attacker.ChatUsers_S.DuelId = 0;
                    Defender.ChatUsers_S.DuelId = 0;
                    context.Update(Attacker);
                    context.Update(Defender);
                    context.SaveChanges();
                    return;
                }

                if (Defender.ChatUsers_S.Streak > 0)
                {
                    if (Defender.ChatUsers_S.MaxWinStreak < Defender.ChatUsers_S.Streak)
                        Defender.ChatUsers_S.MaxWinStreak = Defender.ChatUsers_S.Streak;

                    Defender.ChatUsers_S.Streak = 0;
                    context.Update(Defender);
                    context.SaveChanges();
                }

                Attacker.ChatUsers_S.DuelId = 0;
                Defender.ChatUsers_S.DuelId = 0;
                context.Update(Attacker);
                context.Update(Defender);
                context.SaveChanges();

                Bot.SendMessage(Channel, 2, true, $"@{Attacker.Name}, {Enums.GenderSpecific(Channel, Defender.Name, "nie podjął")} się walki!");
            }
        }
        internal static void CancelDuel(string Channel, string Name, List<string> Arguments)
        {
            if (Arguments.Count > 1)
            {
                Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !anuluj użytkownikowi {Name} (Niewłaściwa liczba argumentów)");
                Bot.SendMessage(Channel, 0, false, $"@{Name}, Podano niewłaściwą liczbę argumentów (!anuluj)... To nie takie trudne...");
                return;
            }

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

                if (User.ChatUsers_S.DuelId == 0)
                {
                    Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !nie użytkownikowi {Name} (Użytkownik nie był wyzwany na pojedynek)");
                    Bot.SendMessage(Channel, 0, false, $"@{Name}, Nie {Enums.GenderSpecific(Channel, Name, "byłeś wyzwany")} na żaden pojedynek");
                    return;
                }

                if (!DuelList.ContainsKey(User.ChatUsers_S.DuelId))
                {
                    Errors.ElementNotFound((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber(),
                        MethodBase.GetCurrentMethod().DeclaringType.Name, "Duel w liście walk", Channel);
                    Bot.SendMessage(Channel, 1, false, $"Wystąpił niespodziewany błąd!");
                    User.ChatUsers_S.DuelId = 0;
                    context.Update(User);
                    context.SaveChanges();
                    return;
                }

                Duels Duel = DuelList[User.ChatUsers_S.DuelId];
                DuelList.Remove(User.ChatUsers_S.DuelId);

                var Attacker = context.ChatUsers.Where(x => x.Channel == Channel && x.TwitchId == Duel.AttackerId).Include(x => x.ChatUsers_S).FirstOrDefault();
                var Defender = context.ChatUsers.Where(x => x.Channel == Channel && x.TwitchId == Duel.DefenderId).Include(x => x.ChatUsers_S).FirstOrDefault();

                Attacker.ChatUsers_S.DuelId = 0;
                Defender.ChatUsers_S.DuelId = 0;
                context.Update(Attacker);
                context.Update(Defender);
                context.SaveChanges();

                if (Duel.Expiration < DateTime.Now)
                {
                    Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !nie użytkownikowi {Name} (Pojedynek wygasł)");
                    Bot.SendMessage(Channel, 0, false, $"@{Name}, Nie {Enums.GenderSpecific(Channel, Name, "byłeś wyzwany")} na żaden pojedynek");
                    return;
                }

                Bot.SendMessage(Channel, 2, true, $"@{Defender.Name}, {Enums.GenderSpecific(Channel, Attacker.Name, "anulował")} pojedynek!");
            }
        }
        internal static void Stats(string Channel, string Name, List<string> Arguments)
        {
            using (var context = new Database())
            {
                double Winrate = 100;
                if (Arguments.Count == 2 && Name.ToLower() != Arguments[1].ToLower().Replace("@", ""))
                {
                    var AtUser = context.ChatUsers.Where(x => x.Channel == Channel && x.Name == Arguments[1].Replace("@", "")).Include(x => x.ChatUsers_S).FirstOrDefault();
                    if (AtUser == null)
                    {
                        Bot.LogEvent(Channel, 1, $"Odmówiono użycia komendy !staty użytkownikowi {Name} (Nie znaleziono oznaczonego użytkownika)");
                        Bot.SendMessage(Channel, 0, false, $"@{Name}, Nie znaleziono użytkownika {Arguments[1].Trim('@')}");
                        return;
                    }
                    Winrate *= AtUser.ChatUsers_S.DuelsWon;
                    Winrate /= AtUser.ChatUsers_S.DuelsPlayed;
                    Winrate = Math.Round(Winrate, 2);
                    Bot.SendMessage(Channel, 2, true, $"@{Name}, {Database.GetNickname(Channel, AtUser.Name)}: Zwycięstwa: {AtUser.ChatUsers_S.DuelsWon} | Rozegrane walki: {AtUser.ChatUsers_S.DuelsPlayed} | Winrate: {Winrate}% | Max win/lose streak: {AtUser.ChatUsers_S.MaxWinStreak}/{AtUser.ChatUsers_S.MaxLoseStreak * -1} | Obecny streak: {AtUser.ChatUsers_S.Streak}");
                    return;
                }
                var User = context.ChatUsers.Where(x => x.Channel == Channel && x.Name == Name).Include(x => x.ChatUsers_S).FirstOrDefault();
                Winrate *= User.ChatUsers_S.DuelsWon;
                Winrate /= User.ChatUsers_S.DuelsPlayed;
                Winrate = Math.Round(Winrate, 2);
                Bot.SendMessage(Channel, 2, true, $"@{Name}: Zwycięstwa: {User.ChatUsers_S.DuelsWon} | Rozegrane walki: {User.ChatUsers_S.DuelsPlayed} | Winrate: {Winrate}% | Max win/lose streak: {User.ChatUsers_S.MaxWinStreak}/{User.ChatUsers_S.MaxLoseStreak * -1} | Obecny streak: {User.ChatUsers_S.Streak}");

            }
        }
        static void FinishFight(ChatUsers_S Winner, ChatUsers_S Loser, Duels Duel)
        {
            using (var context = new Database())
            {
                Winner.DuelsWon++;
                Winner.DuelsPlayed++;
                Loser.DuelsPlayed++;

                Winner.Points += Duel.PointsPool;
                Loser.Points -= Duel.PointsPool;

                Winner.DuelId = 0;
                Loser.DuelId = 0;

                if (Winner.Streak >= 0)
                {
                    Winner.Streak++;
                }
                else
                {
                    if (Winner.Streak < Winner.MaxLoseStreak)
                        Winner.MaxLoseStreak = Winner.Streak;

                    Winner.Streak = 1;
                }

                if (Loser.Streak <= 0)
                {
                    Loser.Streak--;
                }
                else
                {
                    if (Loser.Streak > Loser.MaxWinStreak)
                        Loser.MaxWinStreak = Loser.Streak;

                    Loser.Streak = -1;
                }
                context.Update(Winner);
                context.Update(Loser);
                context.SaveChanges();
            }
        }
    }
}

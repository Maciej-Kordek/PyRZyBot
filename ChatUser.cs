using System;
using System.Collections.Generic;
using System.Text;

namespace PyRZyBot
{
    public class ChatUser
    {
        public ChatUser(string idName, string displayedName)
        {
            IdName = idName;
            DisplayedName = displayedName;
            NickName = "";

            Pyry = 1000;
            Stawka = 0;
            Wyzwania = "";
            Streak = 0;
            MaxWinStreak = 0;
            MaxLoseStreak = 0;
            DuelsWon = 0;
            DuelsPlayed = 0;
            Cooldown = DateTime.Now.AddMinutes(-2);

            tldr = 0;
            CzySpam = 0;

            IsBanned = false;
            IsModerator = false;
        }

        public string IdName { get; set; }
        public string DisplayedName { get; set; }
        public string NickName { get; set; }

        public int Pyry { get; set; }
        public int Streak { get; set; }
        public int MaxWinStreak { get; set; }
        public int MaxLoseStreak { get; set; }
        public int DuelsWon { get; set; }
        public int DuelsPlayed { get; set; }
        public int Stawka { get; set; }
        public string Wyzwania { get; set; }
        public DateTime Cooldown { get; set; }

        public int tldr { get; set; }
        public int CzySpam { get; set; }

        public bool IsBanned { get; set; }
        public bool IsModerator { get; set; }


        public static void Streaks(string Winner, string Loser, Dictionary<string, ChatUser> Chatusers)
        {
            Chatusers[Winner].DuelsWon++;
            Chatusers[Winner].DuelsPlayed++;
            Chatusers[Loser].DuelsPlayed++;

            if (Chatusers[Winner].Stawka >= 100)
            {
                if (Chatusers[Winner].Streak >= 0)
                {
                    Chatusers[Winner].Streak++;
                }
                else
                {
                    if (Chatusers[Winner].Streak < Chatusers[Winner].MaxLoseStreak)
                    {
                        Chatusers[Winner].MaxLoseStreak = Chatusers[Winner].Streak;
                    }
                    Chatusers[Winner].Streak = 1;
                }

                if (Chatusers[Loser].Streak <= 0)
                {
                    Chatusers[Loser].Streak--;
                }
                else
                {
                    if (Chatusers[Loser].Streak > Chatusers[Loser].MaxWinStreak)
                    {
                        Chatusers[Loser].MaxWinStreak = Chatusers[Loser].Streak;
                    }
                    Chatusers[Loser].Streak = -1;
                }
            }
        }
        public static int SinceLastFight(Dictionary<string, ChatUser> Chatusers, string IdName)
        {
            TimeSpan Difference = DateTime.Now - Chatusers[IdName].Cooldown;
            int Seconds = Convert.ToInt32(Math.Round(Difference.TotalSeconds));

            return Seconds;
        }
        public static void ClearWyzwania(Dictionary<string, ChatUser> Chatusers)
        {
            List<string> IdNames = new List<string>(Chatusers.Keys);
            foreach (var IdName in IdNames)
                Chatusers[IdName].Wyzwania = "";
        }
    }
}

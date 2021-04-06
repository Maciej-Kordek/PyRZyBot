using System;
using System.Collections.Generic;

namespace PyRZyBot_2._0.Entities
{
    public class ChatUsers_S
    {
        public ChatUsers_S() { }
        public ChatUsers_S(int twitchid)
        {
            TwitchId = twitchid;
            Gender = (int)GenderEnum.none;
            Points = 1000;
            DuelsPlayed = 0;
            DuelsWon = 0;
            Streak = 0;
            MaxWinStreak = 0;
            MaxLoseStreak = 0;
            DuelId = 0;
            MaxDuelBet = -1;
            AcceptsDuels = true;
            LastDuel = DateTime.MinValue;
        }

        public int Id { get; set; }
        public int TwitchId { get; set; }
        public int Gender { get; set; }
        public int Points { get; set; }
        public int DuelsPlayed { get; set; }
        public int DuelsWon { get; set; }
        public int Streak { get; set; }
        public int MaxWinStreak { get; set; }
        public int MaxLoseStreak { get; set; }
        public int DuelId { get; set; }
        public int MaxDuelBet { get; set; }
        public bool AcceptsDuels { get; set; }
        public DateTime LastDuel { get; set; }

        public List<ChatUsers> ChatUsers { get; set; }
    }
}

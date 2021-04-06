using System;

namespace PyRZyBot_2._0.Entities
{
    public class ChatUsers
    {
        public ChatUsers() { }
        public ChatUsers(int twitchid, string name, string channel)
        {
            TwitchId = twitchid;
            Name = name;
            Nickname = "";
            AccessLevel = (int)AccessLevels.user;
            Channel = channel;
            IsOnline = true;
            GotOnline = DateTime.Now;
            Watchtime = 0;
            MessagesSent = 0;
            tldr = 0;
            RequestSpam = 0;
            TimeoutTill = DateTime.Now;
        }

        public int Id { get; set; }
        public int TwitchId { get; set; }
        public string Name { get; set; }
        public string Nickname { get; set; }
        public int AccessLevel { get; set; }
        public string Channel { get; set; }
        public bool IsOnline { get; set; } 
        public DateTime GotOnline { get; set; }
        public int Watchtime { get; set; }
        public int MessagesSent { get; set; }
        public int tldr { get; set; }
        public int RequestSpam { get; set; }
        public DateTime TimeoutTill { get; set; }

        public ChatUsers_S ChatUsers_S { get; set; }
    }
}

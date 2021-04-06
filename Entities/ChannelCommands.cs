using System;
using System.Collections.Generic;
using System.Text;

namespace PyRZyBot_2._0.Entities
{
    public class ChannelCommands
    {
        public ChannelCommands() { }
        public ChannelCommands(string commandname, string response, string channel)
        {
            CommandName = commandname;
            Response = response;
            AccessLevel = 2;
            Timer = 0;
            TimesUsed = 0;
            Cooldown = 3;
            LastUsed = DateTime.Now.AddDays(-1);
            Channel = channel;
            GameSpecific = "";
            IsEnabled = true;
            IsComplex = false;
            ToDisplay = true;
            EditLevel = 5;
            ParentCommand = 0;
            Deletable = true;
        }

        public int Id { get; set; }
        public string CommandName { get; set; }
        public string Response { get; set; }
        public int AccessLevel { get; set; }
        public int Timer { get; set; }
        public int TimesUsed { get; set; }
        public int Cooldown { get; set; }
        public DateTime LastUsed { get; set; }
        public string Channel { get; set; }
        public string GameSpecific { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsComplex { get; set; }
        public bool ToDisplay { get; set; }
        public int ParentCommand { get; set; }
        public int EditLevel { get; set; }
        public bool Deletable { get; set; }

        public List<Aliases> Aliases { get; set; }        
    }
}

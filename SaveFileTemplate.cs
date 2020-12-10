using System;
using System.Collections.Generic;
using System.Text;

namespace PyRZyBot
{
    public class SaveFileTemplate
    {
        public Dictionary<string, ChatUser> ChatUsers;
        public List<string> Banned_Users;
        public List<string> Mods;
        public List<string> Responces;
        public List<int> Weights;
        public Dictionary<string, string> simpleCommands;
        public List<string> Grafik;
        public Dictionary<string, string> Mistakes;
        public string NextGame;
        public string NextTitle;
    }
}

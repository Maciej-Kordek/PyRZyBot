using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace PyRZyBot_2._0.Entities
{
    public class Database : DbContext
    {
        private string _ConnectionString = "Server=(localdb)\\mssqllocaldb;Database=PyRZyDb;Trusted_Connection=True;";

        public DbSet<ChannelInfo> ChannelInfo { get; set; }
        public DbSet<ChatUsers> ChatUsers { get; set; }
        public DbSet<ChatUsers_S> ChatUsers_S { get; set; }

        public DbSet<ChannelCommands> ChannelCommands { get; set; }
        public DbSet<Aliases> Aliases { get; set; }

        public DbSet<Quotes> Quotes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ChatUsers>()
                .HasOne(x => x.ChatUsers_S)
                .WithMany(x => x.ChatUsers)
                .HasForeignKey(x => x.TwitchId)
                .HasPrincipalKey(x => x.TwitchId);

            modelBuilder.Entity<ChannelCommands>()
                .HasMany(x => x.Aliases)
                .WithOne(x => x.ChannelCommands)
                .HasForeignKey(x => x.CommandId)
                .HasPrincipalKey(x => x.Id)
                .OnDelete(DeleteBehavior.Cascade);
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_ConnectionString);
        }

        public static void CheckUser(int TwitchId, string Name, string Channel, bool IsBroadcaster, bool IsMod, bool IsVip)
        {
            using (var context = new Database())
            {
                var User = context.ChatUsers.FirstOrDefault(x => x.Channel == Channel && x.TwitchId == TwitchId);
                if (User == null)
                    AddUser(TwitchId, Name, Channel);

                User = context.ChatUsers.FirstOrDefault(x => x.Channel == Channel && x.TwitchId == TwitchId);

                if (!User.IsOnline)
                {
                    User.IsOnline = true;
                    context.Update(User);
                    context.SaveChanges();
                }

                if (User.Name != Name)
                {
                    User.Name = Name;
                    context.Update(User);
                    context.SaveChanges();
                    CheckForIllegalNickname(Channel, Name);
                }

                if (User.AccessLevel > (int)AccessLevels.softbanned && User.AccessLevel != (int)AccessLevels.dev)
                {
                    if (IsBroadcaster && User.AccessLevel < (int)AccessLevels.broadcaster)
                        SetAccessLevel(Name, (int)AccessLevels.broadcaster, Channel);
                    else if (IsMod && User.AccessLevel < (int)AccessLevels.mod)
                        SetAccessLevel(Name, (int)AccessLevels.mod, Channel);
                    else if (IsVip && User.AccessLevel < (int)AccessLevels.vip)
                        SetAccessLevel(Name, (int)AccessLevels.vip, Channel);
                }
                return;
            }
        }
        private static void AddUser(int TwitchId, string Name, string Channel)
        {
            using (var context = new Database())
            {
                var User = new ChatUsers(TwitchId, Name, Channel);
                context.Add(User);

                Bot.LogEvent(Channel, 1, $"Użytkownik {Name} został dodany do bazy danych");

                CheckForIllegalNickname(Channel, Name);

                var User_S = context.ChatUsers_S.FirstOrDefault(x => x.TwitchId == TwitchId);
                if (User_S == null)
                {
                    User_S = new ChatUsers_S(TwitchId);
                    context.Add(User_S);
                    Bot.LogEvent(Channel, 0, $"Użytkownik {Name} został dodany do wspólnej bazy danych");
                }
                context.SaveChanges();
            }
        }

        public static void SetAccessLevel(string Name, int AccessLevel, string Channel)
        {
            using (var context = new Database())
            {
                var User = context.ChatUsers.FirstOrDefault(x => x.Name == Name && x.Channel == Channel);
                if (User == null)
                {
                    Errors.UserNotFound((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber(),
                        MethodBase.GetCurrentMethod().DeclaringType.Name, Name, Channel);
                    Bot.SendMessage(Channel, 1, false, $"Wystąpił niespodziewany błąd!");
                    return;
                }
                User.AccessLevel = AccessLevel;
                context.Update(User);
                context.SaveChanges();
            }
        }
        public static int GetAccessLevel(string Channel, string Name)
        {
            using (var context = new Database())
            {
                var AccessLevel = context.ChatUsers.FirstOrDefault(x => x.Channel == Channel && x.Name == Name)?.AccessLevel;
                if (AccessLevel == null)
                {
                    Errors.UserNotFound((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber(),
                        MethodBase.GetCurrentMethod().DeclaringType.Name, Name, Channel);
                    Bot.SendMessage(Channel, 1, false, $"Wystąpił niespodziewany błąd!");
                    return 0;
                }
                return (int)AccessLevel;
            }
        }
        public static void SetGender(string Name, int Gender, string Channel)
        {
            using (var context = new Database())
            {
                var User = context.ChatUsers.Where(x => x.Name == Name).Include(x => x.ChatUsers_S).FirstOrDefault();
                if (User == null)
                {
                    Errors.UserNotFound((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber(),
                        MethodBase.GetCurrentMethod().DeclaringType.Name, Name, "");
                    Bot.SendMessage(Channel, 1, false, $"Wystąpił niespodziewany błąd!");
                    return;
                }
                User.ChatUsers_S.Gender = Gender;
                context.Update(User);
                context.SaveChanges();
            }
        }
        public static int GetGender(string Name)
        {
            using (var context = new Database())
            {
                var Gender = context.ChatUsers.Where(x => x.Name == Name).Include(x => x.ChatUsers_S).FirstOrDefault()?.ChatUsers_S.Gender;
                if (Gender == null)
                {
                    Errors.UserNotFound((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber(),
                        MethodBase.GetCurrentMethod().DeclaringType.Name, Name, "");
                    return 2;
                }
                return (int)Gender;
            }
        }
        public static string GetNickname(string Channel, string Name)
        {
            using (var context = new Database())
            {
                var User = context.ChatUsers.FirstOrDefault(x => x.Channel == Channel && x.Name == Name);
                if(User == null)
                {
                    Errors.UserNotFound((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber(),
                        MethodBase.GetCurrentMethod().DeclaringType.Name, Name, Channel);
                    Bot.SendMessage(Channel, 1, false, $"Wystąpił niespodziewany błąd!");
                    return Name;
                }
                if (string.IsNullOrEmpty(User.Nickname))
                    return Name;

                return User.Nickname;
            }
        }

        internal static bool IsTimeouted(string Name, string Channel)
        {
            using (var context = new Database())
            {
                var User = context.ChatUsers.FirstOrDefault(x => x.Name == Name && x.Channel == Channel);
                if (User == null)
                {
                    Errors.UserNotFound((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber(),
                        MethodBase.GetCurrentMethod().DeclaringType.Name, Name, "");
                    Bot.SendMessage(Channel, 1, false, $"Wystąpił niespodziewany błąd!");
                    return true;
                }
                return (DateTime.Now < User.TimeoutTill);
            }
        }
        internal static bool IsBanned(string Name, string Channel)
        {
            using (var context = new Database())
            {
                var User = context.ChatUsers.FirstOrDefault(x => x.Name == Name && x.Channel == Channel);
                if(User == null)
                {
                    Errors.UserNotFound((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber(),
                        MethodBase.GetCurrentMethod().DeclaringType.Name, Name, "");
                    Bot.SendMessage(Channel, 1, false, $"Wystąpił niespodziewany błąd!");
                    return true;
                }
                return (User.AccessLevel == 0);
            }
        }

        public static void CheckForIllegalNickname(string Channel, string Name)
        {
            using (var context = new Database())
            {
                var NicknameUser = context.ChatUsers.FirstOrDefault(x => x.Nickname == Name && x.Channel == Channel);
                if (NicknameUser != null)
                {
                    Bot.LogEvent(Channel, 1, $"Nickname użytkownika {NicknameUser.Name} został usunięty");

                    NicknameUser.Nickname = "";
                    context.Update(NicknameUser);
                    context.SaveChanges();

                    if (NicknameUser.IsOnline)
                    { Bot.SendMessage(Channel, 2, false, $"@{NicknameUser.Name}, Twój nickname został usunięty"); }
                }
            }
        }
        public static bool IsNicknamelegal(string Channel, string Nickname)
        {
            string _Pattern = @"^[\p{L}0-9][_\p{L}0-9 ]{3,24}$";
            var Match = Regex.Match(Nickname, _Pattern);
            return Match.Success;
        }
        public static bool IsNicknameTaken(string Channel, string Nickname)
        {
            using (var context = new Database())
            {
                var NicknameUser = context.ChatUsers.FirstOrDefault(x => x.Channel == Channel && (x.Name == Nickname || x.Nickname == Nickname));
                return !(NicknameUser == null);
            }
        }
        public static void CountMessage(string Name, string Channel)
        {
            using (var context = new Database())
            {
                var User = context.ChatUsers.FirstOrDefault(x => x.Channel == Channel && x.Name == Name);
                if (User == null)
                {
                    string Class = MethodBase.GetCurrentMethod().DeclaringType.Name;
                    int Line = (new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber();
                    Errors.UserNotFound(Line, Class, Name, Channel);
                    return;
                }
                User.MessagesSent++;
                context.Update(User);
                context.SaveChanges();
            }
        }
    }
}

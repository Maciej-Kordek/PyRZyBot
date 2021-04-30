using Microsoft.EntityFrameworkCore;
using PyRZyBot_2._0.Entities;
using System;
using System.Linq;
using System.Reflection;

namespace PyRZyBot_2._0
{
    public enum GenderEnum
    {
        female,//0
        male,//1
        none//2
    }
    public enum AccessLevels
    {
        banned,//0
        softbanned,//1
        user,//2
        vip,//3
        trusted,//4
        mod,//5
        broadcaster,//6
        dev//7
    }
    public enum FeedbackLevels
    {
        least,//0
        some,//1
        all//2
    }
    public enum OnOff
    {
        off,//0
        on//1
    }
    public class Enums
    {
        public static string GenderSpecific(string Channel, string Name, string Word)
        {
            int Gender = Database.GetGender(Name);
            using (var context = new Database())
            {
                var User = context.ChatUsers.Where(x => x.Name == Name.ToLower()).Include(x => x.ChatUsers_S).FirstOrDefault();
                if (User == null)
                {
                    Errors.UserNotFound((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber(),
                        MethodBase.GetCurrentMethod().DeclaringType.Name, Name, "");
                    return $"Użytkownik {Database.GetNickname(Channel, Name)} {Word}";
                }
                Gender = User.ChatUsers_S.Gender;
            }
            switch (Word.ToLower())
            {
                case "odebrał":
                    {
                        switch (Gender)
                        {
                            case (int)GenderEnum.female:
                                return $"{Name} odebrała";
                            case (int)GenderEnum.male:
                                return $"{Name} odebrał";
                            case (int)GenderEnum.none:
                                return $"Użytkownik {Name} odebrał";
                            default:
                                Errors.UnexpectedInput((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber(),
                                    MethodBase.GetCurrentMethod().DeclaringType.Name, Name, "");
                                return $"Użytkownik {Name} odebrał";
                        }
                    }
                case "wyzwał":
                    {
                        switch (Gender)
                        {
                            case (int)GenderEnum.female:
                                return $"{Database.GetNickname(Channel, Name)} wyzwała";
                            case (int)GenderEnum.male:
                                return $"{Database.GetNickname(Channel, Name)} wyzwał";
                            case (int)GenderEnum.none:
                                return $"Użytkownik {Database.GetNickname(Channel, Name)} wyzwał";
                            default:
                                Errors.UnexpectedInput((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber(),
                                    MethodBase.GetCurrentMethod().DeclaringType.Name, Name, "");
                                return $"Użytkownik {Database.GetNickname(Channel, Name)} wyzwał";
                        }
                    }
                case "wygrał":
                    {
                        switch (Gender)
                        {
                            case (int)GenderEnum.female:
                                return $"{Name} wygrała";
                            case (int)GenderEnum.male:
                                return $"{Name} wygrał";
                            case (int)GenderEnum.none:
                                return $"Użytkownik {Name} wygrał";
                            default:
                                Errors.UnexpectedInput((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber(),
                                    MethodBase.GetCurrentMethod().DeclaringType.Name, Name, "");
                                return $"Użytkownik {Name} wygrał";
                        }
                    }
                case "przegrał":
                    {
                        switch (Gender)
                        {
                            case (int)GenderEnum.female:
                                return $"{Name} przegrała";
                            case (int)GenderEnum.male:
                                return $"{Name} przegrał";
                            case (int)GenderEnum.none:
                                return $"Użytkownik {Name} przegrał";
                            default:
                                Errors.UnexpectedInput((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber(),
                                    MethodBase.GetCurrentMethod().DeclaringType.Name, Name, "");
                                return $"Użytkownik {Name} przegrał";
                        }
                    }
                case "nie podjął":
                    {
                        switch (Gender)
                        {
                            case (int)GenderEnum.female:
                                return $"{Database.GetNickname(Channel, Name)} nie podjęła";
                            case (int)GenderEnum.male:
                                return $"{Database.GetNickname(Channel, Name)} nie podjął";
                            case (int)GenderEnum.none:
                                return $"Użytkownik {Database.GetNickname(Channel, Name)} nie podjął";
                            default:
                                Errors.UnexpectedInput((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber(),
                                    MethodBase.GetCurrentMethod().DeclaringType.Name, Name, "");
                                return $"Użytkownik {Database.GetNickname(Channel, Name)} nie podjął";
                        }
                    }
                case "byłeś wyzwany":
                    {
                        switch (Gender)
                        {
                            case (int)GenderEnum.female:
                                return $"byłaś wyzwana";
                            case (int)GenderEnum.male:
                                return $"byłeś wyzwany";
                            case (int)GenderEnum.none:
                                return $"byłeś(aś) wyzwany(a)";
                            default:
                                Errors.UnexpectedInput((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber(),
                                    MethodBase.GetCurrentMethod().DeclaringType.Name, Name, "");
                                return $"byłeś(aś) wyzwany(a)";
                        }
                    }
                case "anulował":
                    {
                        switch (Gender)
                        {
                            case (int)GenderEnum.female:
                                return $"{Database.GetNickname(Channel, Name)} anulowała";
                            case (int)GenderEnum.male:
                                return $"{Database.GetNickname(Channel, Name)} anulował";
                            case (int)GenderEnum.none:
                                return $"Użytkownik {Database.GetNickname(Channel, Name)} anulował";
                            default:
                                Errors.UnexpectedInput((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber(),
                                    MethodBase.GetCurrentMethod().DeclaringType.Name, Name, "");
                                return $"Użytkownik {Database.GetNickname(Channel, Name)} anulował";
                        }
                    }
                default:
                    Errors.UnaccountedForInput((new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber(),
                        MethodBase.GetCurrentMethod().DeclaringType.Name, Word);
                    return $"Użytkownik {Name} {Word}";
            }
        }
        public static bool IsFeedbackLevelDefined(string Value)
        {
            if (Enums.IsInt(Value))
            {
                int IntValue = int.Parse(Value);
                return Enum.IsDefined(typeof(FeedbackLevels), IntValue);
            }
            return Enum.IsDefined(typeof(FeedbackLevels), Value);
        }
        public static bool IsAccessLevelDefined(string Value)
        {
            if (Enums.IsInt(Value))
            {
                int IntValue = int.Parse(Value);
                return Enum.IsDefined(typeof(AccessLevels), IntValue);
            }
            return Enum.IsDefined(typeof(AccessLevels), Value);
        }
        public static bool IsGenderDefined(string Value)
        {
            if (Enums.IsInt(Value))
            {
                int IntValue = int.Parse(Value);
                return Enum.IsDefined(typeof(GenderEnum), IntValue);
            }
            return Enum.IsDefined(typeof(GenderEnum), Value);
        }
        public static bool IsOnOffDefined(string Value)
        {
            if (Enums.IsInt(Value))
            {
                int IntValue = int.Parse(Value);
                return Enum.IsDefined(typeof(OnOff), IntValue);
            }
            return Enum.IsDefined(typeof(OnOff), Value);
        }
        public static bool IsInt(string Input)
        {
            return int.TryParse(Input, out _);
        }
    }
}

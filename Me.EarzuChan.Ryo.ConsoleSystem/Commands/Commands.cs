using static Me.EarzuChan.Ryo.ConsoleSystem.Commands.ICommand;

namespace Me.EarzuChan.Ryo.ConsoleSystem.Commands
{

    public enum CommandRegistrationStrategy
    {
        ScanAndRegisterAutomatically,
        RegisterManually,
    }

    public interface ICommand
    {
        public const string NO_DESCRIPTION = "No description available.";

        public void Execute(ConsoleApplicationContext context);
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class CommandAttribute : Attribute
    {
        public readonly bool Scannable;

        public readonly string Name;

        public readonly string Description;

        public readonly bool IsDev;

        public CommandAttribute(string name, string info = NO_DESCRIPTION, bool isDev = false, bool scannable = true)
        {
            Name = name;
            IsDev = isDev;
            Scannable = scannable;
            Description = info;
        }
    }

    // 如 -F ，那短名就是F，然后有介绍
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class OptionalParameterAttribute : Attribute
    {
        public string Name { get; set; }

        public string Information { get; set; }

        public OptionalParameterAttribute(string name, string info = NO_DESCRIPTION)
        {
            Name = name;
            Information = info;
        }
    }

    // 没有短名，将大驼峰转成下划线连接
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class RequiredParameterAttribute : Attribute
    {
        public string Description { get; }

        public RequiredParameterAttribute(string description = NO_DESCRIPTION) => Description = description;
    }

    public class HelpCommand : ICommand
    {
        public void Execute(ConsoleApplicationContext context)
        {
            context.PrintLine($"{context.Commands.Count} Available Commands:\n-------------------");

            int i = 1;
            foreach (var cmd in context.Commands) context.PrintLine($"{i++}. {cmd.Key.Name} - {cmd.Key.Description}");

            context.PrintLine($"\nType 'exit' to exit the {context.Profile.Name}.");
        }
    }
}

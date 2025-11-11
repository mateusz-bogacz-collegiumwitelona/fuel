namespace Services.Commands
{
    public class CommandRunner
    {
        private readonly Dictionary<string, BaseCommand> _commands;

        public CommandRunner(IServiceProvider serviceProvider)
        {
            _commands = new Dictionary<string, BaseCommand>
            {
                { "user:create", new CreateUserCommand(serviceProvider) },
                { "user:promote", new PromoteUserCommand(serviceProvider) },
            };
        }

        public async Task RunAsync(string[] args)
        {
            if (args.Length == 0 || args[0] == "list")
            {
                ShowAvailableCommands();
                return;
            }

            var commandName = args[0];

            if (_commands.TryGetValue(commandName, out var command))
            {
                await command.ExecuteAsync(args.Skip(1).ToArray());
            }
            else
            {
                Console.WriteLine($"Command '{commandName}' not found.\n");
                ShowAvailableCommands();
            }
        }

        private void ShowAvailableCommands()
        {
            Console.WriteLine("Available commands:");
            Console.WriteLine();
            foreach (var cmd in _commands.Values)
            {
                Console.WriteLine($"  {cmd.Name,-20} {cmd.Description}");
            }
            Console.WriteLine();
        }
    }
}

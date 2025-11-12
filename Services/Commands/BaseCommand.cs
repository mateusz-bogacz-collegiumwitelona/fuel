using Microsoft.Extensions.DependencyInjection;

namespace Services.Commands
{
    public abstract class BaseCommand
    {
        protected readonly IServiceProvider _serviceProvider;

        protected BaseCommand(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract Task ExecuteAsync(string[] args);

        protected void PrintSuccess(string message) => Console.WriteLine($"Success: {message}");
        protected void PrintError(string message) => Console.WriteLine($"Error: {message}");
        protected void PrintWarning(string message) => Console.WriteLine($"Warning:  {message}");
        protected void PrintInfo(string message) => Console.WriteLine($"Info:  {message}");
    }
}

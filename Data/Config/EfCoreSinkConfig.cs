using Data.Context;
using Data.Enums;
using Data.Models;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;

namespace Data.Config
{
    public class EfCoreSinkConfig : IBatchedLogEventSink
    {
        private readonly IServiceProvider _serviceProvider;

        public EfCoreSinkConfig(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task EmitBatchAsync(IEnumerable<LogEvent> batch)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var logs = batch.Select(logEvent => new Logging
                {
                    Id = Guid.NewGuid(),
                    Message = logEvent.RenderMessage(),
                    StackTrace = logEvent.Exception?.ToString(),
                    Source = logEvent.Exception?.Source,
                    TypeOfLog = logEvent.Level switch
                    {
                        LogEventLevel.Warning => TypeOfLog.Warning,
                        LogEventLevel.Error => TypeOfLog.Error,
                        _ => TypeOfLog.Info
                    },
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                await db.Logging.AddRangeAsync(logs);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EfCoreSink batch error: {ex.Message}");
            }
        }

        public Task OnEmptyBatchAsync()
        {
            return Task.CompletedTask;
        }
    }
}
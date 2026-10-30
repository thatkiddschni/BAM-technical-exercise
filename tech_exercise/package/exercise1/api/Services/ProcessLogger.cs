using Microsoft.EntityFrameworkCore;
using StargateAPI.DataAccess;

namespace StargateAPI.Services
{
    public class ProcessLogger
    {
        private readonly IDbContextFactory<StargateContext> _contextFactory;

        public ProcessLogger(IDbContextFactory<StargateContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task LogAsync(
            string operation,
            string component,
            string outcome,
            string message,
            Exception? exception = null,
            CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            context.ProcessLogs.Add(new ProcessLog
            {
                Operation = operation,
                Component = component,
                Outcome = outcome,
                Message = message,
                ExceptionType = exception?.GetType().FullName,
                ExceptionDetails = exception?.ToString(),
                OccurredAtUtc = DateTime.UtcNow
            });

            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ProcessLog>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            return await context.ProcessLogs
                .AsNoTracking()
                .OrderBy(log => log.Id)
                .ToListAsync(cancellationToken);
        }
    }
}

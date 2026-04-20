using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog;

namespace MimsApi.Data.Logging
{
    /// <summary>
    /// Extension methods for database logging
    /// </summary>
    public static class DatabaseLoggingExtensions
    {
        /// <summary>
        /// Configure Entity Framework Core logging with Serilog
        /// </summary>
        public static DbContextOptionsBuilder<T> EnableDetailedLogging<T>(
            this DbContextOptionsBuilder<T> optionsBuilder,
            ILoggerFactory loggerFactory) where T : DbContext
        {
            return optionsBuilder
                .UseLoggerFactory(loggerFactory)
                .EnableSensitiveDataLogging();
        }
    }
}

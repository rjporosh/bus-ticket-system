using System.Data;
using System.Data.Common;
using BusTicketing.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Routing;

namespace BusTicketing.Infrastructure.Services;

public class QueryLoggingInterceptor : DbCommandInterceptor
{
    private readonly ICustomLogger _customLogger;
    private readonly ILogger<QueryLoggingInterceptor> _logger;
    private readonly IServiceProvider _serviceProvider;

    public QueryLoggingInterceptor(
        ICustomLogger customLogger,
        ILogger<QueryLoggingInterceptor> logger,
        IServiceProvider serviceProvider)
    {
        _customLogger = customLogger;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        return base.ReaderExecuting(command, eventData, result);
    }

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        await LogQueryAsync(command, eventData);
        return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        await LogQueryAsync(command, eventData);
        return await base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        return base.ScalarExecuting(command, eventData, result);
    }

    public override async ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        await LogQueryAsync(command, eventData);
        return await base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }

    private async Task LogQueryAsync(DbCommand command, CommandEventData eventData)
    {
        try
        {
            var httpContextAccessor = _serviceProvider.GetService<IHttpContextAccessor>();
            var httpContext = httpContextAccessor?.HttpContext;
            var routeData = httpContext?.GetRouteData();

            var controller = routeData?.Values["controller"]?.ToString() ?? "Unknown";
            var action = routeData?.Values["action"]?.ToString() ?? "Unknown";

            var query = command.CommandText;
            var parameters = string.Join(", ", command.Parameters.Cast<System.Data.Common.DbParameter>().Select(p => $"{p.ParameterName}={p.Value}"));
            var fullQuery = $"{query}\nParameters: {parameters}";

            await _customLogger.LogQueryAsync(controller, action, fullQuery, 0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log query to custom logger");
        }
    }
}

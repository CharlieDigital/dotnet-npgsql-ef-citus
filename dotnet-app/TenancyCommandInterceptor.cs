using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

/// <summary>
/// EF Core interceptor that automatically sets the tenant context in PostgreSQL
/// before executing commands. This calls the set_tenant() function to make the
/// DealershipId available to database queries and row-level security.
/// </summary>
public class TenancyCommandInterceptor : DbCommandInterceptor
{
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result
    )
    {
        SetTenantContext(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default
    )
    {
        SetTenantContext(command);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result
    )
    {
        SetTenantContext(command);
        return base.ScalarExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default
    )
    {
        SetTenantContext(command);
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result
    )
    {
        SetTenantContext(command);
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        SetTenantContext(command);
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    /// <summary>
    /// Sets the tenancy context for the specified command by prepending a call to
    /// set_tenant().  The tenancy information is retrieved from an AsyncLocal via
    /// the <see cref="TenancyScope"/>
    /// </summary>
    /// <param name="command">The current executing command.</param>
    private static void SetTenantContext(DbCommand command)
    {
        var dealershipId = TenancyScope.CurrentOrDefault;

        if (dealershipId.HasValue)
        {
            // Use PERFORM in a DO block to call set_tenant without returning a result
            // This prevents interference with EF Core's result reading which will
            // result in the Npgsql error:
            //
            //   Reading as 'System.Guid' is not supported for fields having DataTypeName 'void'
            command.CommandText =
                $"DO $$ BEGIN PERFORM set_tenant('{dealershipId.Value}'); END $$;\n{command.CommandText}";
        }
    }
}

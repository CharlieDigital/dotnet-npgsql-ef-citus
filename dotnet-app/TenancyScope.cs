using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

/// <summary>
/// Provides thread-safe access to the current dealership context using AsyncLocal storage.
/// This ensures the DealershipId flows correctly across async/await boundaries and works with TransactionScope.
/// </summary>
public static class TenancyScope
{
    private static readonly AsyncLocal<Guid?> _currentDealershipId = new();

    /// <summary>
    /// Gets the current DealershipId for this async context.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no DealershipId is set in the current context.</exception>
    public static Guid Current
    {
        get
        {
            var value = _currentDealershipId.Value;
            if (!value.HasValue)
            {
                throw new InvalidOperationException(
                    "No DealershipId is set in the current context. Use TenancyScope.SetDealershipId() or TenancyScope.Begin() to set a dealership context."
                );
            }
            return value.Value;
        }
    }

    /// <summary>
    /// Gets the current DealershipId if set, or null if not in a tenancy context.
    /// </summary>
    public static Guid? CurrentOrDefault => _currentDealershipId.Value;

    /// <summary>
    /// Sets the DealershipId for the current async context.
    /// </summary>
    /// <param name="dealershipId">The dealership ID to set.</param>
    public static void SetDealershipId(Guid dealershipId)
    {
        _currentDealershipId.Value = dealershipId;
    }

    /// <summary>
    /// Clears the DealershipId from the current async context.
    /// </summary>
    public static void Clear()
    {
        _currentDealershipId.Value = null;
    }

    /// <summary>
    /// Creates a new tenancy scope that automatically clears when disposed.
    /// </summary>
    /// <param name="dealershipId">The dealership ID to set for this scope.</param>
    /// <returns>A disposable scope that will clear the tenancy when disposed.</returns>
    public static IDisposable Begin(Guid dealershipId)
    {
        SetDealershipId(dealershipId);
        return new TenancyScopeDisposable();
    }

    private sealed class TenancyScopeDisposable : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                Clear();
                _disposed = true;
            }
        }
    }
}

using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;

/// <summary>
/// An interceptor that performs a "virtual" `ON DELETE SET NULL` for distributed
/// tables in Citus by setting the non-distribution key column in the compound
/// index to `null`, which effectively simulates the `ON DELETE SET NULL` behavior
/// for distributed tables.
/// </summary>
/// <remarks>
/// <para>
/// In Citus, composite foreign keys that include the distribution key cannot use
/// standard `ON DELETE SET NULL` because it would attempt to null the entire FK,
/// including the distribution key (which is part of the primary key).
/// </para>
/// <para>
/// This interceptor detects properties marked with <see cref="CitusSetNullOnDeleteAttribute"/>
/// and sets only those properties to null when the principal entity is deleted,
/// preserving the distribution key.
/// </para>
/// </remarks>
public class SetNullInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result
    )
    {
        if (eventData.Context is not null)
        {
            ProcessDeletedEntities(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        if (eventData.Context is not null)
        {
            ProcessDeletedEntities(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Finds all deleted entities and processes their dependents that have
    /// <see cref="CitusSetNullOnDeleteAttribute"/> marked properties.
    /// </summary>
    private static void ProcessDeletedEntities(DbContext context)
    {
        // Get all entities marked for deletion
        var deletedEntries = context
            .ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Deleted)
            .ToList();

        if (deletedEntries.Count == 0)
            return;

        // Build a map of principal types to their dependents with CitusSetNullOnDelete attributes
        var dependencyMap = BuildDependencyMap(context);

        foreach (var deletedEntry in deletedEntries)
        {
            var principalType = deletedEntry.Entity.GetType();

            if (!dependencyMap.TryGetValue(principalType, out var dependentInfos))
                continue;

            foreach (var dependentInfo in dependentInfos)
            {
                ProcessDependentEntities(context, deletedEntry, dependentInfo);
            }
        }
    }

    /// <summary>
    /// Builds a map from principal entity types to information about their dependents
    /// that have <see cref="CitusSetNullOnDeleteAttribute"/> marked properties.
    /// </summary>
    private static Dictionary<Type, List<DependentEntityInfo>> BuildDependencyMap(DbContext context)
    {
        var map = new Dictionary<Type, List<DependentEntityInfo>>();

        foreach (var entityType in context.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            // Find all properties with CitusSetNullOnDeleteAttribute
            foreach (var property in clrType.GetProperties())
            {
                var attribute = property.GetCustomAttribute<CitusSetNullOnDeleteAttribute>();
                if (attribute is null)
                    continue;

                // Find the navigation property
                var navigation = entityType.FindNavigation(attribute.NavigationName);
                if (navigation is null)
                    continue;

                var principalType = navigation.TargetEntityType.ClrType;

                // Get the FK properties for this navigation
                var foreignKey = navigation.ForeignKey;

                if (!map.TryGetValue(principalType, out var list))
                {
                    list = [];
                    map[principalType] = list;
                }

                list.Add(
                    new DependentEntityInfo
                    {
                        DependentEntityType = entityType,
                        DependentClrType = clrType,
                        PropertyToNull = property,
                        ForeignKey = foreignKey,
                        Navigation = navigation,
                    }
                );
            }
        }

        return map;
    }

    /// <summary>
    /// Finds and updates all dependent entities that reference the deleted principal.
    /// </summary>
    private static void ProcessDependentEntities(
        DbContext context,
        EntityEntry deletedEntry,
        DependentEntityInfo dependentInfo
    )
    {
        // Get the principal's key values
        var principalKeyValues = GetPrincipalKeyValues(deletedEntry, dependentInfo.ForeignKey);

        // Find tracked dependents that reference this principal
        var trackedDependents = context
            .ChangeTracker.Entries()
            .Where(e =>
                e.Entity.GetType() == dependentInfo.DependentClrType
                && e.State != EntityState.Deleted
                && MatchesForeignKey(e, dependentInfo.ForeignKey, principalKeyValues)
            )
            .ToList();

        // Set the marked property to null on each dependent
        foreach (var dependentEntry in trackedDependents)
        {
            var currentValue = dependentInfo.PropertyToNull.GetValue(dependentEntry.Entity);
            if (currentValue is not null)
            {
                dependentInfo.PropertyToNull.SetValue(dependentEntry.Entity, null);
                dependentEntry.State = EntityState.Modified;
            }
        }

        // Also query the database for untracked dependents
        // This ensures we don't miss any dependents that weren't loaded
        LoadAndUpdateUntrackedDependents(context, deletedEntry, dependentInfo, principalKeyValues);
    }

    /// <summary>
    /// Gets the principal key values that the FK references.
    /// </summary>
    private static Dictionary<string, object?> GetPrincipalKeyValues(
        EntityEntry principalEntry,
        IForeignKey foreignKey
    )
    {
        var result = new Dictionary<string, object?>();
        var principalKey = foreignKey.PrincipalKey;

        foreach (var property in principalKey.Properties)
        {
            var value = principalEntry.Property(property.Name).CurrentValue;
            result[property.Name] = value;
        }

        return result;
    }

    /// <summary>
    /// Checks if a dependent entry's FK values match the principal key values.
    /// </summary>
    private static bool MatchesForeignKey(
        EntityEntry dependentEntry,
        IForeignKey foreignKey,
        Dictionary<string, object?> principalKeyValues
    )
    {
        var fkProperties = foreignKey.Properties;
        var pkProperties = foreignKey.PrincipalKey.Properties;

        for (var i = 0; i < fkProperties.Count; i++)
        {
            var fkValue = dependentEntry.Property(fkProperties[i].Name).CurrentValue;
            var pkPropertyName = pkProperties[i].Name;

            if (!principalKeyValues.TryGetValue(pkPropertyName, out var pkValue))
                return false;

            if (!Equals(fkValue, pkValue))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Queries the database for untracked dependent entities and updates them.
    /// </summary>
    private static void LoadAndUpdateUntrackedDependents(
        DbContext context,
        EntityEntry deletedEntry,
        DependentEntityInfo dependentInfo,
        Dictionary<string, object?> principalKeyValues
    )
    {
        // Build a query using the DbContext's Set method dynamically
        var dbSetMethod = typeof(DbContext)
            .GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!
            .MakeGenericMethod(dependentInfo.DependentClrType);

        var dbSet = dbSetMethod.Invoke(context, null);
        if (dbSet is null)
            return;

        // We need to query for dependents that match the FK values
        // Use reflection to call the appropriate query methods
        var queryable = dbSet as IQueryable<object>;
        if (queryable is null)
            return;

        // Build filter expression for FK match
        var fkProperties = dependentInfo.ForeignKey.Properties;
        var pkProperties = dependentInfo.ForeignKey.PrincipalKey.Properties;

        // Load dependents that match the FK - we'll filter in memory for simplicity
        // In a production system, you'd want to build a proper expression tree
        var allDependents = queryable.ToList();

        foreach (var dependent in allDependents)
        {
            var entry = context.Entry(dependent);

            // Skip if already tracked and processed
            if (entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
                continue;

            // Check if this dependent matches the FK
            var matches = true;
            for (var i = 0; i < fkProperties.Count; i++)
            {
                var fkValue = entry.Property(fkProperties[i].Name).CurrentValue;
                var pkPropertyName = pkProperties[i].Name;

                if (
                    !principalKeyValues.TryGetValue(pkPropertyName, out var pkValue)
                    || !Equals(fkValue, pkValue)
                )
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
            {
                var currentValue = dependentInfo.PropertyToNull.GetValue(dependent);
                if (currentValue is not null)
                {
                    dependentInfo.PropertyToNull.SetValue(dependent, null);
                    entry.State = EntityState.Modified;
                }
            }
        }
    }

    /// <summary>
    /// Contains information about a dependent entity type that has a
    /// <see cref="CitusSetNullOnDeleteAttribute"/> marked property.
    /// </summary>
    private sealed class DependentEntityInfo
    {
        public required IEntityType DependentEntityType { get; init; }
        public required Type DependentClrType { get; init; }
        public required PropertyInfo PropertyToNull { get; init; }
        public required IForeignKey ForeignKey { get; init; }
        public required INavigation Navigation { get; init; }
    }
}

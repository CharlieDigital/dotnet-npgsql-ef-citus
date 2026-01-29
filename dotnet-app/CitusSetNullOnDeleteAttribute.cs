namespace Microsoft.EntityFrameworkCore;

/// <summary>
/// Marks a foreign key property for "virtual SET NULL" behavior in Citus distributed tables.
/// When the referenced principal entity is deleted, only this property is set to null,
/// preserving the distribution key portion of the composite FK.
/// </summary>
/// <remarks>
/// This attribute is used by <see cref="SetNullInterceptor"/> to detect which properties
/// should be nulled when a principal entity is deleted. The distribution key (typically
/// the tenant ID) remains intact, maintaining proper data isolation.
/// </remarks>
/// <remarks>
/// ⚠️ This approach may not be scalable because it requires reading the entities in
/// to mark with null.  This is probably a big footgun.
/// <example>
/// <code>
/// public class PartsOrder
/// {
///     public Guid DealershipId { get; set; }  // Distribution key - preserved
///
///     [CitusSetNullOnDelete(nameof(Vehicle))]
///     public Guid? VehicleId { get; set; }    // Will be set to null on Vehicle delete
///
///     public Vehicle? Vehicle { get; set; }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property)]
public class CitusSetNullOnDeleteAttribute : Attribute
{
    /// <summary>
    /// The name of the navigation property that points to the principal entity.
    /// </summary>
    public string NavigationName { get; }

    /// <summary>
    /// Creates a new instance of <see cref="CitusSetNullOnDeleteAttribute"/>.
    /// </summary>
    /// <param name="navigationName">
    /// The name of the navigation property pointing to the principal entity
    /// whose deletion should trigger setting this property to null.
    /// </param>
    public CitusSetNullOnDeleteAttribute(string navigationName)
    {
        NavigationName = navigationName;
    }
}

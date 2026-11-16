using System;
using Robust.Shared.GameObjects;

namespace Content.Server._Aspis.Organizations;

/// <summary>
/// Server-owned identity of a round-local construction yard or contract hull.
/// Never grants ship ownership, station membership or permission to sell a grid.
/// </summary>
[RegisterComponent]
public sealed partial class OrganizationConstructionYardComponent : Component
{
    public Guid OrganizationId;
    public Guid AssignmentId;
    public string QuotaCode = string.Empty;

    /// <summary>
    /// A yard platform is not the ship being assembled or repaired.
    /// Contract settlement must verify the actual target grid separately.
    /// </summary>
    public bool IsWorkPlatform;
    public EntityUid ContractGrid = EntityUid.Invalid;
}

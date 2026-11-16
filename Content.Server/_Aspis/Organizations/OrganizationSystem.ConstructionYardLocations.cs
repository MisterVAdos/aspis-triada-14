using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Server.Station.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._Aspis.Organizations;

/// <summary>
/// Q2.2A: construction yards belong to the sector's ordinary space map.
/// This locator deliberately does not create maps, FTL destinations or ships.
/// Callers must load a trusted yard/grid template, validate its footprint,
/// place it at the returned world position and reserve that position only
/// after the entire deployment has succeeded.
/// </summary>
public sealed partial class OrganizationSystem
{
    private const float ConstructionYardFirstRing = 900f;
    private const float ConstructionYardRingStep = 350f;
    private const int ConstructionYardRingCount = 9;
    private const int ConstructionYardSpokesPerRing = 16;
    private const float ConstructionYardSafetyBuffer = 175f;

    private readonly List<ConstructionYardReservation> _constructionYardReservations = new();

    private readonly record struct ConstructionYardReservation(
        MapId MapId,
        Vector2 WorldPosition,
        float ClearanceRadius);

    /// <summary>
    /// Use the SAME station and map selection as corporate outposts.
    /// In particular, a yard must not be loaded onto an isolated expedition
    /// destination merely because that map happens to contain the requester.
    /// </summary>
    private bool TryGetConstructionSector(
        out EntityUid sectorMap,
        out MapId mapId,
        out Vector2 sectorOrigin)
    {
        sectorMap = EntityUid.Invalid;
        mapId = default;
        sectorOrigin = default;

        var principal = _organizationStationSystem.GetStations()
            .Where(station => !HasComp<OrganizationOutpostStationComponent>(station))
            .Where(station => TryComp<StationDataComponent>(station, out var data) &&
                              data.Grids.Count > 0)
            .OrderByDescending(station => Name(station).Contains(
                "Venmar Station", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(station =>
                _organizationStationSystem.GetTileCount(Comp<StationDataComponent>(station)))
            .FirstOrDefault();

        if (!principal.Valid)
            return false;

        var anchor = _organizationStationSystem.GetLargestGrid(
            Comp<StationDataComponent>(principal));
        if (anchor is not { } stationGrid ||
            Transform(stationGrid).MapUid is not { } sharedMap)
            return false;

        sectorMap = sharedMap;
        mapId = Transform(stationGrid).MapID;
        sectorOrigin = Transform(stationGrid).WorldPosition;
        return true;
    }

    /// <summary>
    /// Find an unoccupied world-space site that a normal shuttle can reach.
    /// The footprint radius must cover BOTH the work platform and the hull,
    /// including sufficient extra room for docking and maneuvering.
    /// </summary>
    private bool TryFindConstructionYardPosition(
        MapId mapId,
        Vector2 sectorOrigin,
        float footprintRadius,
        out Vector2 position)
    {
        position = default;
        var clearance = MathF.Max(75f, footprintRadius) + ConstructionYardSafetyBuffer;
        var intersecting = new List<Entity<MapGridComponent>>();

        for (var ring = 0; ring < ConstructionYardRingCount; ring++)
        {
            var distance = ConstructionYardFirstRing + ring * ConstructionYardRingStep;

            for (var spoke = 0; spoke < ConstructionYardSpokesPerRing; spoke++)
            {
                // Alternating spokes prevent long radial rows of construction
                // sites, while retaining deterministic placement per round.
                var angle = (spoke + (ring % 2 == 0 ? 0f : 0.5f)) *
                    (MathF.PI * 2f / ConstructionYardSpokesPerRing);
                var candidate = sectorOrigin +
                    new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;

                if (_constructionYardReservations.Any(reservation =>
                    reservation.MapId == mapId &&
                    Vector2.DistanceSquared(candidate, reservation.WorldPosition) <
                    MathF.Pow(clearance + reservation.ClearanceRadius, 2f)))
                    continue;

                intersecting.Clear();
                _organizationMapSystem.FindGridsIntersecting(
                    mapId,
                    Box2.CenteredAround(candidate, new Vector2(clearance * 2f)),
                    ref intersecting,
                    false,
                    false);

                if (intersecting.Count != 0)
                    continue;

                position = candidate;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Reserve ONLY after successful deployment; never on a rejected quota.
    /// Round cleanup will clear this cache once live yard handling is wired.
    /// </summary>
    private void ReserveConstructionYardPosition(
        MapId mapId,
        Vector2 position,
        float footprintRadius)
    {
        _constructionYardReservations.Add(new ConstructionYardReservation(
            mapId, position, MathF.Max(75f, footprintRadius) + ConstructionYardSafetyBuffer));
    }
}

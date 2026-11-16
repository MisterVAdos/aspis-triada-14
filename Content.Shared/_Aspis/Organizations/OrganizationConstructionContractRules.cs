using System;

namespace Content.Shared._Aspis.Organizations;

/// <summary>
/// Server/client-neutral contract categories for Q2.2. A construction contract
/// is not an IndustrialExport cargo order. These rules do not unlock or pay
/// a contract without the server's physical ship-inspection proof.
/// </summary>
public enum OrganizationConstructionContractKind : byte
{
    MinorHullRepair,
    CompartmentReconstruction,
    CapitalShipRepair,
    NewShipConstruction
}

public static class OrganizationConstructionContractRules
{
    public const int MaximumYardsPerRound = 2;

    public static OrganizationConstructionContractKind KindForSlot(int slot) =>
        (OrganizationConstructionContractKind)Math.Clamp(slot, 0, 3);

    public static string Title(int slot) => KindForSlot(slot) switch
    {
        OrganizationConstructionContractKind.MinorHullRepair => "Поточний ремонт шатла",
        OrganizationConstructionContractKind.CompartmentReconstruction => "Реконструкція відсіків",
        OrganizationConstructionContractKind.CapitalShipRepair => "Капітальне відновлення корабля",
        _ => "Суднобудівний контракт"
    };

    // These are contract deadlines, not artificial build timers.
    public static int DeadlineMinutes(int slot) => KindForSlot(slot) switch
    {
        OrganizationConstructionContractKind.MinorHullRepair => 60,
        OrganizationConstructionContractKind.CompartmentReconstruction => 100,
        OrganizationConstructionContractKind.CapitalShipRepair => 165,
        _ => 240
    };

    // 100 basis points = 1 percentage point of sector influence.
    // These are proposed balancing coefficients, NOT live grant logic.
    public static int ProposedSectorInfluenceBasisPoints(int slot) => KindForSlot(slot) switch
    {
        OrganizationConstructionContractKind.MinorHullRepair => 8,
        OrganizationConstructionContractKind.CompartmentReconstruction => 25,
        OrganizationConstructionContractKind.CapitalShipRepair => 100,
        _ => 150
    };
}

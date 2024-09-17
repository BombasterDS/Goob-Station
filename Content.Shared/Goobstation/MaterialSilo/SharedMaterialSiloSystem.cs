using Content.Shared.Goobstation.MaterialSilo.Components;
using Content.Shared.Materials;
using Content.Shared.Power.EntitySystems;

namespace Content.Shared.Goobstation.MaterialSilo;

public abstract partial class SharedMaterialSiloSystem : EntitySystem
{
    [Dependency] private readonly SharedMaterialStorageSystem _materialStorage = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiverSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MaterialSiloComponent, MaterialAmountChangedEvent>(OnMaterialsChange);
    }

    public int GetSiloMaterialAmount(EntityUid uid, string material)
    {
        var silo = GetConnectedAndActiveSilo(uid);

        if (silo == null)
            return 0;

        var amount = _materialStorage.GetMaterialAmount(silo.Value, material);

        return amount;
    }

    /// <summary>
    /// Used for lathes to change materials in silo first and then use materials in lathe storage. Return the amount of material that
    /// will be used from lathe storage.
    /// </summary>
    public int ChangeMaterialInSilo(EntityUid uid, string material, int amount)
    {
        var silo = GetConnectedAndActiveSilo(uid);

        if (silo == null)
            return amount;

        var amountInSilo = _materialStorage.GetMaterialAmount(silo.Value, material);

        if (amountInSilo >= amount)
        {
            _materialStorage.TryChangeMaterialAmount(silo.Value, material, -amount);

            return 0;
        }

        var leftAmount = amount - amountInSilo;
        _materialStorage.TryChangeMaterialAmount(silo.Value, material, -amountInSilo);

        return leftAmount;
    }

    /// <summary>
    /// Returns silo if it's powered and active
    /// </summary>
    private EntityUid? GetConnectedAndActiveSilo(EntityUid uid)
    {
        if (!TryComp<MaterialSiloUtilizerComponent>(uid, out var utilizer) || utilizer.SiloUid == null)
            return null;

        if (!HasComp<MaterialStorageComponent>(utilizer.SiloUid))
            return null;

        if (!_powerReceiverSystem.IsPowered(uid))
            return null;

        return utilizer.SiloUid;
    }

    private void OnMaterialsChange(Entity<MaterialSiloComponent> silo, ref MaterialAmountChangedEvent args)
    {
        // Need to update UI on lathes if someone uses it to prevent bugs
        foreach (var utilizer in silo.Comp.UtilizersUids)
        {
            RaiseLocalEvent(utilizer, args);
        }
    }
}

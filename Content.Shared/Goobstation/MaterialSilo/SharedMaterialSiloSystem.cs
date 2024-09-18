using Content.Shared.DeviceLinking.Events;
using Content.Shared.Goobstation.MaterialSilo.Components;
using Content.Shared.Materials;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Timing;

namespace Content.Shared.Goobstation.MaterialSilo;

public abstract partial class SharedMaterialSiloSystem : EntitySystem
{
    [Dependency] private readonly SharedMaterialStorageSystem _materialStorage = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiver = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MaterialSiloComponent, NewLinkEvent>(OnSiloLink);
        SubscribeLocalEvent<MaterialSiloComponent, MaterialAmountChangedEvent>(OnMaterialsChange);

        SubscribeLocalEvent<MaterialSiloUtilizerComponent, NewLinkEvent>(OnUtilizerLink);
        SubscribeLocalEvent<MaterialSiloUtilizerComponent, PortDisconnectedEvent>(OnDisconnected);
        SubscribeLocalEvent<MaterialSiloUtilizerComponent, MaterialEntityBeforeInsertEvent>(OnMaterialInsert);
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
    /// Connects silo to utilizer and adds utilizer uid to list
    /// </summary>
    private void OnSiloLink(Entity<MaterialSiloComponent> silo, ref NewLinkEvent args)
    {
        var comp = silo.Comp;
        var utilizer = args.Sink;

        if (!HasComp<MaterialSiloUtilizerComponent>(utilizer))
            return;

        if (!comp.UtilizersUids.Contains(utilizer))
            comp.UtilizersUids.Add(utilizer);

        Dirty(silo);
    }

    /// <summary>
    /// Connects utilizer to silo and move all materials to silo storage
    /// </summary>
    private void OnUtilizerLink(Entity<MaterialSiloUtilizerComponent> utilizer, ref NewLinkEvent args)
    {
        var comp = utilizer.Comp;
        var silo = args.Source;

        if (!TryComp<MaterialStorageComponent>(utilizer, out var storage) || !TryComp<MaterialStorageComponent>(silo, out var siloStorage))
            return;

        // Prevent stack overflow it someone will try to put silo and utilizer on same entity
        if (utilizer.Owner == silo)
            return;

        comp.SiloUid = silo;
        Dirty(utilizer);

        foreach (var material in storage.Storage.Keys)
        {
            var materialAmount = _materialStorage.GetMaterialAmount(utilizer, material, storage);

            if (_materialStorage.TryChangeMaterialAmount(silo, material, materialAmount, siloStorage))
                _materialStorage.TryChangeMaterialAmount(utilizer, material, -materialAmount, storage);
        }
    }

    /// <summary>
    /// Remove connection between silo and utilizer
    /// </summary>
    private void OnDisconnected(Entity<MaterialSiloUtilizerComponent> utilizer, ref PortDisconnectedEvent args)
    {
        var silo = utilizer.Comp.SiloUid;

        if (TryComp<MaterialSiloComponent>(silo, out var siloComp) && siloComp.UtilizersUids.Contains(utilizer.Owner))
            siloComp.UtilizersUids.Remove(utilizer.Owner);

        utilizer.Comp.SiloUid = null;
        Dirty(utilizer);
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

        if (!_powerReceiver.IsPowered(utilizer.SiloUid.Value))
            return null;

        return utilizer.SiloUid;
    }

    /// <summary>
    /// Checks if have powered silo
    /// </summary>
    public bool HasPoweredSilo(EntityUid uid)
    {
        var silo = GetConnectedAndActiveSilo(uid);

        if (silo == null)
            return false;

        return true;
    }

    /// <summary>
    /// Transfer materials from uzilier to silo if it connected
    /// </summary>
    private void OnMaterialInsert(Entity<MaterialSiloUtilizerComponent> utilizer, ref MaterialEntityBeforeInsertEvent args)
    {
        var comp = utilizer.Comp;
        var silo = comp.SiloUid;

        if (silo == null)
            return;

        if (!_powerReceiver.IsPowered(silo.Value))
            return;

        if (_materialStorage.TryInsertMaterialEntity(args.User, args.ToInsert, comp.SiloUid!.Value, silent: true))
            args.Handled = true;
    }

    private void OnMaterialsChange(Entity<MaterialSiloComponent> silo, ref MaterialAmountChangedEvent args)
    {
        // Need to update UI on lathes if someone uses it to prevent bugs
        foreach (var utilizer in silo.Comp.UtilizersUids)
        {
            RaiseLocalEvent(utilizer, ref args);
        }
    }
}


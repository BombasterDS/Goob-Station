using Content.Server.Materials;
using Content.Server.Power.EntitySystems;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Goobstation.MaterialSilo;
using Content.Shared.Goobstation.MaterialSilo.Components;
using Content.Shared.Materials;
using System.Linq;

namespace Content.Server.Goobstation.MaterialSilo;

public sealed partial class MaterialSiloSystem : SharedMaterialSiloSystem
{
    [Dependency] private readonly MaterialStorageSystem _materialStorage = default!;
    [Dependency] private readonly PowerReceiverSystem _powerReceiver = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MaterialSiloUtilizerComponent, MaterialEntityBeforeInsertEvent>(OnMaterialInsert);
        SubscribeLocalEvent<MaterialSiloUtilizerComponent, NewLinkEvent>(OnUtilizerLink);
        SubscribeLocalEvent<MaterialSiloUtilizerComponent, PortDisconnectedEvent>(OnUtilizerDisconnected);
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

        foreach (var material in storage.Storage.Keys.ToArray())
        {
            var materialAmount = _materialStorage.GetMaterialAmount(utilizer, material, storage);

            if (_materialStorage.TryChangeMaterialAmount(silo, material, materialAmount, siloStorage))
                _materialStorage.TryChangeMaterialAmount(utilizer, material, -materialAmount, storage);
        }
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

    /// <summary>
    /// Remove connection between silo and utilizer 
    /// </summary>
    private void OnUtilizerDisconnected(Entity<MaterialSiloUtilizerComponent> utilizer, ref PortDisconnectedEvent args)
    {
        utilizer.Comp.SiloUid = null;
    }
}

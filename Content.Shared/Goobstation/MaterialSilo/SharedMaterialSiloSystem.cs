using Content.Shared.DeviceLinking.Events;
using Content.Shared.Goobstation.MaterialSilo.Components;
using Content.Shared.Materials;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Microsoft.VisualBasic;

namespace Content.Shared.Goobstation.MaterialSilo;

public sealed partial class SharedMaterialSiloSystem : EntitySystem
{
    [Dependency] private readonly SharedMaterialStorageSystem _materialStorageSystem = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiverSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MaterialSiloUtilizerComponent, MaterialEntityBeforeInsertEvent>(OnMaterialInsert);
        SubscribeLocalEvent<MaterialSiloUtilizerComponent, NewLinkEvent>(OnUtilizerLink);
    }

    // Connects lathe to silo and move all materials to silo storage
    private void OnUtilizerLink(Entity<MaterialSiloUtilizerComponent> utilizer, ref NewLinkEvent args)
    {
        var comp = utilizer.Comp;

        if (!TryComp<MaterialStorageComponent>(utilizer, out var storage))
            return;

        comp.SiloUid = args.Source;
    }

    // Transfer materials from lathe to silo if it connected
    private void OnMaterialInsert(Entity<MaterialSiloUtilizerComponent> entity, ref MaterialEntityBeforeInsertEvent args)
    {
        var comp = entity.Comp;
        var silo = comp.SiloUid;

        if (silo == null)
            return;

        if (!_powerReceiverSystem.IsPowered(silo.Value))
            return;

        if (_materialStorageSystem.TryInsertMaterialEntity(args.User, args.ToInsert, comp.SiloUid!.Value, silent: true))
            args.Handled = true;
    }
}

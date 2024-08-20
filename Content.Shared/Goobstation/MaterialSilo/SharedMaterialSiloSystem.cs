using Content.Shared.Goobstation.MaterialSilo.Components;
using Content.Shared.Materials;

namespace Content.Shared.Goobstation.MaterialSilo;

public sealed partial class SharedMaterialSiloSystem : EntitySystem
{
    [Dependency] private readonly SharedMaterialStorageSystem _materialStorageSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MaterialSiloUtilizerComponent, MaterialEntityBeforeInsertEvent>(OnMaterialInsert);
        //SubscribeLocalEvent<MaterialSiloUtilizerComponent, >(OnMaterialInsert);
    }

    private void OnMaterialInsert(Entity<MaterialSiloUtilizerComponent> entity, ref MaterialEntityBeforeInsertEvent args)
    {
        if (!TryComp<MaterialStorageComponent>(entity, out var storage))
            return;

        var comp = entity.Comp;

        if (comp.MaterialSilo == null)
            return;

        args.Handled = true;

        _materialStorageSystem.TryInsertMaterialEntity(args.User, args.ToInsert, comp.MaterialSilo!.Value);
    }
}

using Robust.Shared.GameStates;

namespace Content.Shared.Goobstation.MaterialSilo.Components;

[RegisterComponent]
[NetworkedComponent]
[Access(typeof(SharedMaterialSiloSystem))]
public sealed partial class MaterialSiloUtilizerComponent : Component
{
    // Connected material silo
    [DataField]
    public EntityUid? MaterialSilo;
}

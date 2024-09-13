using Robust.Shared.GameStates;

namespace Content.Shared.Goobstation.MaterialSilo.Components;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedMaterialSiloSystem))]
public sealed partial class MaterialSiloUtilizerComponent : Component
{
    // Connected material silo uid
    [DataField, AutoNetworkedField]
    public EntityUid? SiloUid;
}

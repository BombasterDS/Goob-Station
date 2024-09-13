using Robust.Shared.GameStates;

namespace Content.Shared.Goobstation.MaterialSilo.Components;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedMaterialSiloSystem))]
public sealed partial class MaterialSiloComponent : Component
{
    /// <summary>
    /// Uids of all utilizers connected to this silo
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntityUid> UtilizersUids = new();
}

using Sandbox.Entity;

namespace Entity.Interactable.Door
{
    public sealed class Door : Component
    {
        [Property] public string DoorName { get; set; }
    }
}

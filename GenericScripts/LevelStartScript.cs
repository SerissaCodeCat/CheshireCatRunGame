using Godot;


public partial class LevelStartScript : Node
{
    public override void _Ready()
    {
        MessageManager.instance.SetCameraStartPosition();
        int x = MessageManager.instance.enquireCurrentHealthofPlayer();
    }
}

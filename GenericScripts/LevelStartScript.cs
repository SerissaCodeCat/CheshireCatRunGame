using Godot;


public partial class LevelStartScript : Node
{
    public override void _Ready()
    {
        MessageManager.instance.setPlayerSpawnPosition(new Vector2(20, 190));
        MessageManager.instance.SetCameraStartPosition();
    }
}

using Godot;


public partial class LevelStartScript : Node
{
    public override void _Ready()
    {
        MessageManager.instance.SetPlayerSpawnPosition(new Vector2(20, 190));
        MessageManager.instance.SetCameraStartPosition();
        MessageManager.instance.SetCameraTarget(); //set camera to player by default
        SoundManager.instance.playMusicByName("testMusic");
        
        //allow physics processes to begin!
        GetTree().Paused = false;
    }
}

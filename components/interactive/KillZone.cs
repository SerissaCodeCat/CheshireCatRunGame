using Godot;

public partial class KillZone : Node2D
{
	private Area2D detectionBox;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		detectionBox = GetNode<Area2D>($"ActivationDetectionBox");
        detectionBox.BodyEntered += (body) => PlayerEnteredDetectionBox(body);
	}
	private void PlayerEnteredDetectionBox(Node2D body)
    {
		GD.Print("Player entered DEATH TRAP detection box");
		MessageManager.instance.KillPlayer();
    }
}

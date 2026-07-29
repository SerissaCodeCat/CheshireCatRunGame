using Godot;
using System;

public partial class StealthItemDefault : Node2D
{
	[Export]
	private Area2D detectionBox;
	public override void _Ready()
	{
		detectionBox.BodyEntered += (body) => PlayerEnteredDetectionBox(body);
		detectionBox.BodyExited += (body) => PlayerLeftDetectionBox(body);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	private void PlayerEnteredDetectionBox(Node2D body)
	{
		if (body.Name.ToString() == "Player")
		{
			MessageManager.instance.increasePlayerStealthLayerCount();
		}
	}
	private void PlayerLeftDetectionBox(Node2D body)
    {
		if (body.Name.ToString() == "Player")
		{
			MessageManager.instance.decreasePlayerStealthLayerCount();
		}
    }
}

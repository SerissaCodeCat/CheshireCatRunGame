using Godot;
using System;

public partial class SpikeTrap : Node2D
{
	private Area2D detectionBox;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
    {
		detectionBox = GetNode<Area2D>($"ActivationDetectionBox");
        detectionBox.BodyEntered += (body) => PlayerEnteredDetectionBox(body);
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void PlayerEnteredDetectionBox(Node2D body)
    {
		GD.Print("Player entered Spike Trap detection box");
		MessageManager.instance.DamagePlayer(this.Position);		
    }
}


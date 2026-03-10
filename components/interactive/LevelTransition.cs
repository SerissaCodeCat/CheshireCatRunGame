using Godot;
using System;

public partial class LevelTransition : Node2D
{
	private Area2D detectionBox;
	[Export]
	public String LevelToTransitionTo {get; set;} = null;
	public override void _Ready()
	{
		detectionBox = GetNode<Area2D>($"ActivationDetectionBox");
        detectionBox.BodyEntered += (body) => PlayerEnteredDetectionBox(body);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	private void PlayerEnteredDetectionBox(Node2D body)
    {
		GD.Print("Player entered LevelTransition detection box");
        MessageManager.instance.LoadLevelWithPath(LevelToTransitionTo);
    }
}

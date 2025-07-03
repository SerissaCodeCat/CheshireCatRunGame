using Godot;
using System;

public partial class Door : Node2D, iButtonActivation
{

	private Sprite2D doorSprite;
	private StaticBody2D doorCollision; 
	public override void _Ready()
	{
		doorSprite = GetNode<Sprite2D>($"Sprite2D");
		doorCollision = GetNode<StaticBody2D>($"StaticBody2D"); 

		//GD.Print("door collision is: " + doorCollision);
	}
	public bool Activate(Node2D body)
	{
		//GD.Print("Door Detected: " + body.Name);
		
		doorSprite.Visible = !doorSprite.Visible;
		if (doorCollision.CollisionLayer == 1)
			doorCollision.CollisionLayer = 0;
		else
			doorCollision.CollisionLayer = 1;
		return true;
	}

	public int feedback()
	{
		return 0;
	}
}

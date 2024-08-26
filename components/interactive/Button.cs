using Godot;
using System;

public partial class Button : Node2D 
{
	[Export]
	public Door buttonLinksTo { get; set; }
	[Export]
	public double reactivationDelay { get; set; } = 5.0d;
	private Sprite2D buttonSprite;
	private Area2D buttonAreaDetection;
	private bool canBeActivated = true;
	private double reactivationTimer = 0;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		buttonAreaDetection = GetNode<Area2D>($"ButtonArea2D");
		buttonAreaDetection.BodyEntered += (body) => SendActivation(body);
		buttonSprite = GetNode<Sprite2D>($"buttonDefault");
		
    }
    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public bool SendActivation(Node2D body)
	{
		if(canBeActivated)
		{
			GD.Print("Button Detected: " + body.Name);
			buttonSprite.Visible = false;
			canBeActivated = false;
			reactivationTimer = reactivationDelay;
			if (buttonLinksTo != null)
			{
				buttonLinksTo.Activate(body);
			}
		}
		return true;
	}
    public override void _Process(double delta)
    {
        base._Process(delta);
		if(!canBeActivated)
		{
			reactivationTimer -= delta;
			if (reactivationTimer <= 0.0d)
			{
				buttonSprite.Visible = true;
				canBeActivated = true;
			}
		}
    }
    public int feedback()
	{
		return 0;
	}

}

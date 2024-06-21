using Godot;
using System;

public partial class Bullet : CharacterBody2D
{
	float direction;
	Vector2 currentVelocity = new Vector2 (0.0f, 0.0f);
	const float speed = 650.0f; 
	const float deceleration = 4.0f; 
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
	public Vector2 spawn = new Vector2 (63.0f, 529.0f);
	public bool goRight = true;
	double lifespan = 5.0d;
	short maxBounces = 3;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		base._Ready();
		GlobalPosition = spawn;
		currentVelocity.X = goRight == true ? speed : 0.0f - speed;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		//GD.Print("CoOrd X = " + GlobalPosition.X );
		//GD.Print("CoOrd Y = " + GlobalPosition.Y );
		//currentVelocity.Y += gravity * (float)delta;
		currentVelocity.X = Mathf.MoveToward(currentVelocity.X, 0, deceleration);
		Velocity = currentVelocity;
		MoveAndSlide();

        if ((lifespan -= delta) <= 0.0d || GetSlideCollisionCount() > maxBounces)
		{
			QueueFree();
		}
	}
	
}

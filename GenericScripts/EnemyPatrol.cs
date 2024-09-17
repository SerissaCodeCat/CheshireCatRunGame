using Godot;
using System;
using System.Numerics;

public partial class EnemyPatrol : CharacterBody2D
{

	public const float Speed = 350.0f;
	public const float acceleration = 15.0f;
	private bool wallToRight = false;
	private Godot.Vector2 finalVelocity;
	private bool direction = true;
	private Godot.Vector2 Stop = new Godot.Vector2(0, 0);
	private CollisionShape2D CollisionBody;
	private Area2D AreaDetection;
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
	private AnimatedSprite2D sprite_2d;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		sprite_2d = GetNode<AnimatedSprite2D>($"Sprite2D");
		CollisionBody = GetNode<CollisionShape2D>($"CollisionShapeStanding");
		AreaDetection = GetNode<Area2D>($"DetectionArea");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		finalVelocity = Velocity;
		
		Move(ref finalVelocity, delta);
		
		Velocity = finalVelocity;
		GD.Print("final velocity: " + Velocity);
		MoveAndSlide();
		if (IsOnWall())
		{
			direction = !direction;
		}
	}

	private void Move(ref Godot.Vector2 incomingVelocity, double incomingDelta)
	{
		
		incomingVelocity.Y += gravity * (float)incomingDelta;
		if (IsOnFloor())
		{
			incomingVelocity.X = Mathf.MoveToward(incomingVelocity.X, (direction? 1.0f: -1.0f) * (Speed / 2), acceleration);
			sprite_2d.FlipH = !direction;		
		}

	}
}

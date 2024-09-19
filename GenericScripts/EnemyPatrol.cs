using Godot;
using System;
using System.ComponentModel;
using System.Numerics;

public partial class EnemyPatrol : CharacterBody2D, IPatrolOnGround
{

	public const float Speed = 350.0f;
	public const float acceleration = 15.0f;
	private bool wallToRight = false;
	private Godot.Vector2 finalVelocity;
	private bool direction = true;
	private bool detected = false;
	private double idleTimer = 60.0d;
	private double breakTimer = 0.0d;
	private bool onBreak = false;
	private Godot.Vector2 Stop = new Godot.Vector2(0, 0);
	private CollisionShape2D CollisionBody;
	private Area2D AreaDetectionRight;
	private Area2D AreaDetectionLeft;

	private Shape2D detectionShapeRight;
	private Shape2D detectionShapeLeft;
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
	private Random rnd = new Random();
	private AnimatedSprite2D sprite_2d;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		AreaDetectionRight = GetNode<Area2D>($"DetectionAreaRight");
		AreaDetectionLeft = GetNode<Area2D>($"DetectionAreaLeft");

		AreaDetectionRight.BodyEntered += (body) => DetectPlayerRight(body);
		AreaDetectionLeft.BodyEntered += (body) => DetectPlayerLeft(body);
		sprite_2d = GetNode<AnimatedSprite2D>($"Sprite2D");
		CollisionBody = GetNode<CollisionShape2D>($"CollisionShapeStanding");
		setDetectionDirrection();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		finalVelocity = Velocity;
		
		if(!detected && !onBreak) 
		{
			Move(ref finalVelocity, delta);
		}
		else if (!onBreak)
		{ 
			DetectedMove(ref finalVelocity, direction, delta);
		}
		else
		{
			takeBreak(ref finalVelocity, delta);
		}
		
		Velocity = finalVelocity;
		MoveAndSlide();
		if (IsOnWall())
		{
			direction = !direction;
			Velocity = Godot.Vector2.Zero;
			setDetectionDirrection();
		}
	}

    private void takeBreak(ref Godot.Vector2 incomingVelocity, double incomingDelta)
    {
		incomingVelocity = Godot.Vector2.Zero;
		breakTimer -= incomingDelta;
		if(breakTimer <= 0.0d)
		{
			idleTimer = rnd.Next(20, 120);
			onBreak = false;
		}
	}

    public void Move(ref Godot.Vector2 incomingVelocity, double incomingDelta)
	{
		
		incomingVelocity.Y += gravity * (float)incomingDelta;
		if (IsOnFloor())
		{
			if(idleTimer > 0.0d)
			{
				incomingVelocity.X = Mathf.MoveToward(incomingVelocity.X, (direction? 1.0f: -1.0f) * (Speed / 2), acceleration);
				sprite_2d.FlipH = !direction;
				idleTimer -= incomingDelta;
				//GD.Print("Time Till Break = " +idleTimer);
			}
			else
			{
				onBreak = true;
				breakTimer = rnd.Next(5, 35);
			}		
		}

	}

    public void DetectedMove(ref Godot.Vector2 incomingVelocity, bool detectionDirrection, double incomingDelta)
    {
		incomingVelocity.Y += gravity * (float)incomingDelta;
		if (IsOnFloor())
		{
			incomingVelocity.X = Mathf.MoveToward(incomingVelocity.X, (detectionDirrection ? 1.0f : -1.0f) * Speed, acceleration);
			sprite_2d.FlipH = !direction;
		}
	}

	public bool DetectPlayerRight(Node2D body)
	{
		GD.Print("ping from right" + body.Name);
		//if(body.Name == "Player")
		//{
		//}
		return false;
	}
	public bool DetectPlayerLeft(Node2D body)
	{
		GD.Print("ping from Left " + body.Name);
		//if(body.Name == "Player")
		//{
		//}
		return false;
	}
	private void setDetectionDirrection()
	{
		AreaDetectionRight.Monitoring = direction;
		GD.Print("Setting area Right to" + direction);
		AreaDetectionLeft.Monitoring = !direction;
		GD.Print("Setting area left to " + !direction);

	}

}

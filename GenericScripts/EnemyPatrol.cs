using Godot;
using System;
using System.ComponentModel;
using System.Numerics;

public partial class EnemyPatrol : CharacterBody2D, IPatrolOnGround
{

	public const float Speed = 350.0f;
	public const float acceleration = 15.0f;
	private bool wallToRight = false;
	private bool playerDetected = false;
	private Godot.Vector2 finalVelocity;
	private bool direction = true;
	private bool detected = false;
	private double idleTimer = 60.0d;
	private double breakTimer = 0.0d;
	private bool onBreak = false;
	private Godot.Vector2 Stop = new Godot.Vector2(0, 0);
	private CollisionShape2D CollisionBody;
	private Area2D AreaDetectionRight;

	private Shape2D detectionShapeRight;
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
	private Random rnd = new Random();
	private AnimatedSprite2D sprite_2d;
	private Node2D enemyNode;

	private Godot.Vector2 aboutFace;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		AreaDetectionRight = GetNode<Area2D>($"DetectionAreaRight");
		AreaDetectionRight.BodyEntered += (body) => DetectPlayer(body);
		AreaDetectionRight.BodyExited += (body) => DetectPlayerLeaving(body);
		sprite_2d = GetNode<AnimatedSprite2D>($"Sprite2D");
		enemyNode = this;
		aboutFace = new(-1, enemyNode.Scale.Y);
		CollisionBody = GetNode<CollisionShape2D>($"CollisionShapeStanding");
		//setDetectionDirrection();
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
			//setDetectionDirrection();
			FlipEntity();
			
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
				if(playerDetected)
				{
					incomingVelocity.X = Mathf.MoveToward(incomingVelocity.X, (direction ? 1.0f : -1.0f) * Speed, acceleration);
				}
				else
				{
					incomingVelocity.X = Mathf.MoveToward(incomingVelocity.X, (direction? 1.0f: -1.0f) * (Speed / 2), acceleration);
					idleTimer -= incomingDelta;
				}
				//sprite_2d.FlipH = !direction;
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
			//sprite_2d.FlipH = !direction;
		}
	}

	public void DetectPlayer(Node2D body)
	{
		if (body.Name.ToString() == "Player")
		{
			playerDetected = LineOfSightCheck(body);
		}
	}
	public void DetectPlayerLeaving(Node2D body)
	{
		if (body.Name.ToString() == "Player")
		{
			//GD.Print("player Escaped");
			playerDetected = false;
		}
	}
	private void setDetectionDirrection()
	{
		AreaDetectionRight.Monitoring = direction;
	}
	private bool LineOfSightCheck(Node2D target)
	{
		PhysicsDirectSpaceState2D worldState = GetWorld2D().DirectSpaceState;
		var query = PhysicsRayQueryParameters2D.Create(Position, target.Position);
		query.HitFromInside = false;
		var sightCheck = worldState.IntersectRay(query);
		if (sightCheck.Count != 0)
		{
			if((ulong)sightCheck["collider_id"] == target.GetInstanceId())
			{
				//GD.Print("Target Aquired");

				return true;
			}
		}
		return false;
	}

	private void FlipEntity()
	{
		if (enemyNode.Scale.X == -1)
		{
			enemyNode.Scale *= aboutFace;
		}
		else
		{
			enemyNode.Scale *= aboutFace;
		}
	}

}

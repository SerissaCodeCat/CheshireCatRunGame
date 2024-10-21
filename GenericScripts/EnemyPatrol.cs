using Godot;
using System;
using System.ComponentModel;
using System.Numerics;

public partial class EnemyPatrol : CharacterBody2D
{

	public const float Speed = 350.0f;
	public const float acceleration = 15.0f;
	private bool wallToRight = false;
	private bool playerDetected = false;
	private Godot.Vector2 finalVelocity;
	private bool direction = true;
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

	private Node2D Player = null;

	private double attackTimer = 0.0d;
	private double attackOn;
	private bool attacking;
	private float attackRange = 70;
	private bool stunned = false;
	private double stunTimer = 0.0d;
	private double flashTimer = 0.0d;
	private const double stunTime = 3.0d;
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
		attacking = false;
		attackOn = 2.0d; //this will set the timer so that the enemy attacks after 2 seconds of being within range and detecting the player
		MessageManager.instance.addToEnemyDictionary(this);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		finalVelocity = Velocity;
		if (stunned)
		{
			Velocity = Godot.Vector2.Zero;
			if(stunTimer <= 0.0d)
			{
				stunned = false;
				sprite_2d.Visible = true;
				flashTimer = 0.0d;
			}
			else
			{
				stunTimer -= delta;
				flashTimer -= delta;
				if(flashTimer <= 0.0d)
				{
					flashTimer = stunTimer/8;
					sprite_2d.Visible = !sprite_2d.Visible;
				}
				MoveAndSlide();
			}
		}
		else if(!attacking)
		{
			if(!onBreak) 
			{
				Move(ref finalVelocity, delta);
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
				FlipEntity();
			}
		}
		else
		{
			Attack(ref finalVelocity, direction, delta);
		}
	}

	private bool rangeCheck()
	{
		if(Player.Position.X - Position.X < attackRange && Player.Position.X - Position.X > -attackRange)
		{
			attacking = true;
			return true;
		}
		return false;
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

    private void Move(ref Godot.Vector2 incomingVelocity, double incomingDelta)
	{
		
		incomingVelocity.Y += gravity * (float)incomingDelta;
		if (IsOnFloor())
		{
			if(idleTimer > 0.0d)
			{
				if(playerDetected)
				{
					DetectedMove(ref incomingVelocity, direction, incomingDelta);
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

    private void DetectedMove(ref Godot.Vector2 incomingVelocity, bool detection, double incomingDelta)
    {
		incomingVelocity.Y += gravity * (float)incomingDelta;
		if (IsOnFloor())
		{
			incomingVelocity.X = Mathf.MoveToward(incomingVelocity.X, (direction ? 1.0f : -1.0f) * Speed, acceleration);
			//sprite_2d.FlipH = !direction;
			rangeCheck();
		}
	}

	private void DetectPlayer(Node2D body)
	{
		if (body.Name.ToString() == "Player")
		{
			Player = body;
			playerDetected = LineOfSightCheck(Player);
		}
	}
	private void DetectPlayerLeaving(Node2D body)
	{
		if (body == Player)
		{
			//GD.Print("player Escaped");
			playerDetected = false;
		}
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
				return true;
			}
		}
		return false;
	}

	private void Attack(ref Godot.Vector2 incomingVelocity, bool detectionDirrection, double incomingDelta)
	{
		incomingVelocity = Godot.Vector2.Zero;
		incomingVelocity.Y += gravity * (float)incomingDelta;
		attacking = true;
		attackTimer += incomingDelta;
		if(attackTimer >= attackOn)
		{
			GD.Print("bonk"); //this is where the damage function wound go.... IF I HAD ONE!
			attackTimer = 0.0d;
			attacking = rangeCheck();
		}
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

	public void BeStunned()
	{
		stunned = true;
		stunTimer = stunTime;
	}
}

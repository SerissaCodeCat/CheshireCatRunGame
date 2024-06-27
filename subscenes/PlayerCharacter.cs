using Godot;
using System;

public partial class PlayerCharacter : CharacterBody2D
{
	public const float Speed = 450.0f;
	public const float Deceleration = 15.0f;
	public const float AirDeceleration = 3.3f;
	public const float JumpVelocity = -460.0f;
	private const double CyoteTime = 0.1d;
	private const double teleportTimerReset = 0.3d;
	private const double clingTimerReset = 1.0d;
	private float dashSpeed = 1000.0f;
	private double cyoteTimer = CyoteTime;
	private double teleportTimer = teleportTimerReset;
	private double clingTimer = clingTimerReset;
	private bool doubleJumpAvailiable = true;
	private bool teleportAvailiable = true;
	private bool wallToRight = false;
	private Vector2 shotOffset = new Vector2 (60.0f, 0.0f); 	
	public PackedScene bullet { get; set; }
	private double bulletTimer = 0.0d;
 

	public enum playerStates
	{
		grounded,
		airborn,
		clinging,
		teleporting,
		crouching
	}
	public playerStates PlayerState = playerStates.grounded;
	private Vector2 finalVelocity;
    private Vector2 direction;
	private Vector2 Stop = new Vector2(0,0);

	private AnimatedSprite2D sprite_2d;
	private Node2D aimingLynchpin;
	private Node2D aimingDirrection;
	private CollisionShape2D StandingCollision;
	private CollisionShape2D CrouchingCollision;
	private ShapeCast2D ShapeCast;


	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	public override void _Ready()
	{
		base._Ready();
		sprite_2d = GetNode<AnimatedSprite2D>($"Sprite2D");
		StandingCollision = GetNode<CollisionShape2D>($"CollisionShapeStanding");
		CrouchingCollision = GetNode<CollisionShape2D>($"CollisionShapeCrouching");
		ShapeCast = GetNode<ShapeCast2D>("ShapeCast2D");
		bullet = GD.Load<PackedScene>("res://subscenes/Bullet.tscn");
		aimingLynchpin = GetNode<Node2D>($"aimingLynchpin");
		aimingDirrection = GetNode<Node2D>($"aimingLynchpin/aimingDirection");
		GD.Print("test the : " + bullet);
 	}
	public override void _PhysicsProcess(double delta)
	{
		finalVelocity = Velocity;
		
		switch(PlayerState)
		{
			case playerStates.grounded:
				doGroundedPhysics(ref finalVelocity, delta);
			break;
			case playerStates.airborn:
				doAirbornPhysics(ref finalVelocity, delta);
			break;
			case playerStates.clinging:
				doClingingPhysics(ref finalVelocity, delta);
			break;
			case playerStates.teleporting:
				doTeleportingPhysics(ref finalVelocity, delta);
			break;
			case playerStates.crouching:
				doCrouchingPhysics(ref finalVelocity, delta);
			break;
			default:
			break;
		}
		Velocity = finalVelocity;
		bulletTimer -= delta;
		//fireBullet();	
		
		MoveAndSlide();
	}
	private void doGroundedPhysics(ref Vector2 incomingVelocity, double incomingDelta)
	{
		//add gravity
		incomingVelocity.Y += gravity * (float)incomingDelta;

		//if touching ground, refresh cyote timer, if not, decrease it
		cyoteTimer = IsOnFloor() ? CyoteTime : -incomingDelta; 
		direction = Input.GetVector("left", "right", "up", "down");				
		if(cyoteTimer == 0.0d)
		{
			teleportAvailiable = true;
			doubleJumpAvailiable = true;
			PlayerState = playerStates.airborn;
			GD.Print("entering Airborn State");
			return;
		}

		if (Input.IsActionJustPressed("jump"))
		{
			doubleJumpAvailiable = true;
			teleportAvailiable = true;
			PlayerState = playerStates.airborn;
			incomingVelocity.Y = JumpVelocity;
			cyoteTimer = 0.0d;
			GD.Print("entering Airborn State");
			return;

		}

		if (Input.IsActionJustPressed("teleport"))
		{
			teleportAvailiable = false;
			doubleJumpAvailiable = true;
			PlayerState = playerStates.teleporting;
			teleportTimer = teleportTimerReset;
			GD.Print("entering Teleporting State");
			return;
		}
		if (Input.IsActionPressed("fire"))
		{
			direction = Vector2.Zero;
			//do aimingDirrection rotation thingy here
			//aimingDirrection.Position = sprite_2d.FlipH == true? new Vector2(-65.0f,0) : new Vector2(65.0f,0);
		}
		if (Input.IsActionJustReleased("fire"))
		{
			fireBullet();
		}

		// Get the input direction and handle the movement/deceleration.
		if (direction != Vector2.Zero)
		{
			//add the currently input dirrection to our  velocity
			incomingVelocity.X = Mathf.MoveToward(Velocity.X, direction.X * Speed, Deceleration);
			//turn the sprite to face the current inputted dirrection
			sprite_2d.FlipH = direction.X < 0;
			if(direction.X < 0 && !Input.IsActionPressed("fire"))
			{
				if(aimingLynchpin.RotationDegrees != 180.0f)
				{
					aimingLynchpin.Rotate((float)Mathf.DegToRad(180.0d));
					GD.Print("Aiming right" + aimingDirrection.Position);
					GD.Print(aimingLynchpin.RotationDegrees);
				}
			}
			else if(direction.X > 0 && !Input.IsActionPressed("fire"))
			{
				if(aimingLynchpin.RotationDegrees != 0.0f)
				{
					aimingLynchpin.Rotate((float)Mathf.DegToRad(-180.0d));
					GD.Print("Aiming left" + aimingDirrection.Position);
					GD.Print(aimingLynchpin.RotationDegrees);
				}
			} 
		}
		else
		{
			incomingVelocity.X = Mathf.MoveToward(Velocity.X, 0, Deceleration);
		}

		if(Input.IsActionJustPressed("crouch"))
		{

			GD.Print("entering CROUCHED State");
			StandingCollision.Disabled = true;
			CrouchingCollision.Disabled = false;
			PlayerState = playerStates.crouching;	
		}
		if(incomingVelocity.X != 0.0f)
			sprite_2d.Animation = "running";
		else
			sprite_2d.Animation = "default";
			
	}
	private void doCrouchingPhysics(ref Vector2 incomingVelocity, double incomingDelta)
	{
		//add gravity
		incomingVelocity.Y += gravity * (float)incomingDelta;
		//if touching ground, refresh cyote timer, if not, decrease it
		cyoteTimer = IsOnFloor() ? CyoteTime : -incomingDelta; 
				
		if(cyoteTimer == 0.0d)
		{
			teleportAvailiable = true;
			doubleJumpAvailiable = true;
			StandingCollision.Disabled = false;
			CrouchingCollision.Disabled = true;
			PlayerState = playerStates.airborn;
			GD.Print("entering Airborn State");
			return;
		}

		if (Input.IsActionJustPressed("jump"))
		{
			doubleJumpAvailiable = true;
			teleportAvailiable = true;
			StandingCollision.Disabled = false;
			CrouchingCollision.Disabled = true;
			PlayerState = playerStates.airborn;
			incomingVelocity.Y = JumpVelocity*1.5f;
			cyoteTimer = 0.0d;
			GD.Print("SUPER JUMP");
			return;

		}

		if (Input.IsActionJustPressed("teleport"))
		{
			teleportAvailiable = false;
			doubleJumpAvailiable = true;

			StandingCollision.Disabled = false;
			CrouchingCollision.Disabled = true;

			PlayerState = playerStates.teleporting;
			teleportTimer = teleportTimerReset;
			GD.Print("entering Teleporting State");
			return;
		}

		// Get the input direction and handle the movement/deceleration.
		direction = Input.GetVector("left", "right", "up", "down");
		if (direction != Vector2.Zero)
		{
			//add the currently input dirrection to our  velocity
			incomingVelocity.X = Mathf.MoveToward(Velocity.X, direction.X * (Speed / 2.0f) , Deceleration / 3.0F);
			//turn the sprite to face the current inputted dirrection
			sprite_2d.FlipH = direction.X < 0;
		}
		else
		{
			incomingVelocity.X = Mathf.MoveToward(Velocity.X, 0, Deceleration);
		}

		if(!Input.IsActionPressed("crouch"))
		{
			if(ShapeCast.IsColliding())
			{
				GD.Print("Collision Detected");
			}
			else
			{
				CrouchingCollision.Disabled = true;
				StandingCollision.Disabled = false;
				PlayerState = playerStates.grounded;
			}	
		}

		sprite_2d.Animation = "crouching";
	}

	private void doAirbornPhysics(ref Vector2 incomingVelocity, double incomingDelta)
	{
		//add gravity
		incomingVelocity.Y += gravity * (float)incomingDelta;
		direction = Input.GetVector("left", "right", "up", "down");

		if(IsOnFloor())
		{
			PlayerState = playerStates.grounded;
			doubleJumpAvailiable = true;
			teleportAvailiable = true;
			GD.Print("entering Grounded State");
			return;
		}
		if (!Input.IsActionPressed("crouch"))
		{
			if(IsOnWall())
			{
				determineDirrectionOfWall();
				incomingVelocity = Stop;
				clingTimer = clingTimerReset;
				//if they are on the wall they are clinging 
				PlayerState = playerStates.clinging;
				return;
			}
		}

		if (Input.IsActionJustPressed("teleport"))
		{
			if(teleportAvailiable)
			{
				PlayerState = playerStates.teleporting;
				teleportAvailiable = false;
				teleportTimer = teleportTimerReset;
				GD.Print("entering Teleporting State");
				return;
			}
		}

		if (Input.IsActionJustPressed("jump"))
		{		
			// if jump button is pressed, perform the jump function and set the result as the current velocity
			if (doubleJumpAvailiable)
			{
				incomingVelocity.Y = (JumpVelocity * 0.9f);
				doubleJumpAvailiable = false;
			}
		}

		// Get the input direction and handle the movement/deceleration.
		if (direction != Vector2.Zero)
		{
			//add the currently input dirrection to our  velocity
			incomingVelocity.X = Mathf.MoveToward(Velocity.X, direction.X * Speed, Deceleration);
			//turn the sprite to face the current inputted dirrection
			sprite_2d.FlipH = direction.X < 0;
		}
		else
		{
			finalVelocity.X = Mathf.MoveToward(Velocity.X, 0, AirDeceleration);
		}

		if(doubleJumpAvailiable)
			sprite_2d.Animation = "jumping";
		else
			sprite_2d.Animation = "doubleJump";
	}

	private void doClingingPhysics(ref Vector2 incomingVelocity, double incomingDelta)
	{

		
		if(IsOnFloor())
		{
			PlayerState = playerStates.grounded;
			doubleJumpAvailiable = true;
			teleportAvailiable = true;
			GD.Print("entering Grounded State");
			return;
		}
		//add gravity at 1/3 the normal value due to cat claws stuck in the wall we are clinging to
		if(clingTimer <= 0)
			incomingVelocity.Y += gravity / 3 * (float)incomingDelta;
		else
			clingTimer -= incomingDelta;

		if (Input.IsActionJustPressed("jump"))
		{
			if (wallToRight)
			{
				incomingVelocity.Y = JumpVelocity;
				incomingVelocity.X = -Speed;
				PlayerState = playerStates.airborn;
				if(!sprite_2d.FlipH)
					sprite_2d.FlipH = true;
			}
			else
			{
				incomingVelocity.Y = JumpVelocity;
				incomingVelocity.X = Speed;
				PlayerState = playerStates.airborn;
				if(sprite_2d.FlipH)
					sprite_2d.FlipH = false;
			}
			//push away from wall and add jump power
		}
		direction = Input.GetVector("left", "right", "up", "down");

		//push away from the wall slightly and become airborn
		if (Input.IsActionJustPressed("crouch"))
		{
			if (wallToRight)
			{
				incomingVelocity.X = -Speed/10;
				PlayerState = playerStates.airborn;
			}
			else
			{
				incomingVelocity.X = Speed/10;
				PlayerState = playerStates.airborn;
			}
		}

	}

	private void doTeleportingPhysics(ref Vector2 incomingVelocity, double incomingDelta)
	{
		// do not add gravity. gravity does not apply to teleportation

		if(!sprite_2d.FlipH)
		{
			wallToRight = true;
			finalVelocity.X = dashSpeed;
			finalVelocity.Y = 0.0f;
		}
		else
		{
			wallToRight = false;
			finalVelocity.X = -dashSpeed;
			finalVelocity.Y = 0.0f;
		}

		if((teleportTimer -= incomingDelta)<=0.0d)
		{
			finalVelocity = Stop;
			if(IsOnFloor())
			{
				PlayerState = playerStates.grounded;
				GD.Print("entering Grounded State");
			}
			else if (IsOnWall())
			{	
				PlayerState = playerStates.clinging;
				GD.Print("entering Clinging State");
			}
			else
			{	
				PlayerState = playerStates.airborn;
				GD.Print("entering Airborn State");
			}
		}
	}

	private void determineDirrectionOfWall()
	{
		for (int i = 0;  i < GetSlideCollisionCount(); i++)
		{
			if (GetSlideCollision(i).GetCollider().GetType().FullName == "Godot.TileMap")
			{
				wallToRight = GetSlideCollision(i).GetNormal().X > 0 ?  false : true;
			}
		}
	}
	private void fireBullet()
	{
		if(bulletTimer <= 0.0d)
		{	
			Bullet shot = bullet.Instantiate<Bullet>();
			shot.setup(sprite_2d.FlipH == true ? GlobalPosition - shotOffset : GlobalPosition + shotOffset, 
				aimingDirrection.GlobalPosition - aimingLynchpin.GlobalPosition, Bullet.bulletTypes.basic);
			//shot.goRight = sprite_2d.FlipH == true ? false : true;
			Owner.AddChild(shot);
			bulletTimer = 1.0d;
			GD.Print("FIRE");
		}
	}
}

using Godot;

public partial class PlayerCharacter : CharacterBody2D
{
    /// <State Machine enumerator>
    /// The enum that contains all the different states in which the player may be put.
    /// </State Machine enumerator>
    public enum playerStates
    {
        grounded,
        airborn,
        clinging,
        teleporting,
        crouching,
        damaged
    }
    public playerStates PlayerState = playerStates.grounded;

    /// <External Class refferances>
    ///  //////////////////////////////////////////////////////////////////////////////////////////////////////
    /// </External Class refferances>
    private Godot.Vector2 shotOffset = new Godot.Vector2(60.0f, 0.0f);
    public PackedScene bullet { get; set; }
    private Godot.Color semiTransparent = new Godot.Color(1, 1, 1, 0.5f);
    private Godot.Color solid = new Godot.Color(1, 1, 1, 1f);
    private Godot.Vector2 finalVelocity;
    private Godot.Vector2 direction;
    private Godot.Vector2 Stop = new Godot.Vector2(0, 0);

    private AnimatedSprite2D sprite_2d;
    private Sprite2D aimingSprite;
    private Node2D aimingLynchpin;
    private Node2D aimingDirrection;
    private bool aimingRise = true;
    private CollisionShape2D StandingCollision;
    private CollisionShape2D CrouchingCollision;
    private ShapeCast2D ShapeCast;

    /// <Floats>
    /// ////////////////////////////////////////////////////////////////////////////////
    /// </Floats>
    [Export]
    public float Speed = 450.0f;
    [Export]
    public float Acceleration = 1.0f;
    [Export]
    public float Deceleration = 15.0f;
    [Export]
    public float AirDeceleration = 3.3f;
    [Export]
    public float JumpVelocity = -600.0f;
    [Export]
    private float dashSpeed = 1000.0f;
    [Export]
    private float aimingRotationSpeed = 120.0f;
    [Export]
    private float DamageRecoveryReset = 2.0f;
    [Export]
    private float DamageReboundForce = 3.0f;
    public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
    /// <Doubles>
    /// /////////////////////////////////////////////////////////////////////////////////
    /// </Doubles>
    [Export]
    private double CyoteTime = 0.05d;
    [Export]
    private double teleportTimerReset = 0.3d;
    [Export]
    private double clingTimerReset = 1.0d;
    [Export]
    private double BulletResetTime = 2.5d;
    private double FirstClingReset = 0.5d;
    private double damageTimer = 0.0d;
    private double flashTimer = 0.0d;
    private double cyoteTimer;
    private double teleportTimer;
    private double clingTimer;
    private double firstClingTimer;
    private double bulletTimer = 0.0d;
    /// <Intergers>
    /// ////////////////////////////////////////////////////////////////////////////////
    /// </Intergers>
    public int Health;
    public const int startingHealth = 3;
    public int MaxHealth = 3;
    /// <Bools>
    /// ////////////////////////////////////////////////////////////////////////////////
    /// </Bools>
    private bool doubleJumpAvailiable = true;
    private bool teleportAvailiable = true;
    private bool wallToRight = false;
    private bool damagable;
    public override void _Ready()
    {
        base._Ready();
        sprite_2d = GetNode<AnimatedSprite2D>($"Sprite2D");
        StandingCollision = GetNode<CollisionShape2D>($"CollisionShapeStanding");
        CrouchingCollision = GetNode<CollisionShape2D>($"CollisionShapeCrouching");
        ShapeCast = GetNode<ShapeCast2D>("ShapeCast2D");
        bullet = GD.Load<PackedScene>("res://components/entities/player/Bullet.tscn");
        aimingLynchpin = GetNode<Node2D>($"aimingLynchpin");
        aimingDirrection = GetNode<Node2D>($"aimingLynchpin/aimingDirection");
        aimingSprite = GetNode<Sprite2D>($"aimingLynchpin/aimingSprite");
        aimingSprite.Visible = false;
        MessageManager.instance.addPlayerToMessageManager(this);
        damagable = true;
        cyoteTimer = CyoteTime;
        teleportTimer = teleportTimerReset;
        clingTimer = clingTimerReset;
    }
    public override void _PhysicsProcess(double delta)
    {
        finalVelocity = Velocity;
        //the damage flash is done outside of the physics loop, this is because the flashing and damage recovery needs to span across two states
        if (!damagable)
        {
            if (damageTimer <= 0.0d)
            {
                damagable = true;
                //sprite_2d.Visible = true;
                sprite_2d.Modulate = solid;
                flashTimer = 0.0d;
                if (IsOnFloor())
                {
                    PlayerState = playerStates.grounded;
                }
                else
                {
                    PlayerState = playerStates.airborn;
                }
            }
            else
            {
                damageTimer -= delta;
                flashTimer -= delta;
                if (flashTimer <= 0.0d)
                {
                    flashTimer = damageTimer / 8;
                    flashPlayer();
                }
            }
        }

        switch (PlayerState)
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
            case playerStates.damaged:
                doDamagedPhysics(ref finalVelocity, delta);
                break;
            default:
                PlayerState = playerStates.grounded;
                break;
        }
        Velocity = finalVelocity;
        if (bulletTimer > 0.0d)
        {
            bulletTimer -= delta;
        }
        else
        {
            bulletTimer = 0.0d;
        }
        MessageManager.instance.sendEnegyPercentageTotalToUI(GetbulletTimePercentageDecimal());
        //fireBullet();

        MoveAndSlide();
    }
    private void doGroundedPhysics(ref Godot.Vector2 incomingVelocity, double incomingDelta)
    {
        //add gravity
        incomingVelocity.Y += gravity * (float)incomingDelta;

        //if touching ground, refresh cyote timer, if not, decrease it
        cyoteTimer = IsOnFloor() ? CyoteTime : -incomingDelta;
        direction = Input.GetVector("left", "right", "up", "down");
        if (cyoteTimer <= 0.00d)
        {
            //GD.Print("falling");
            teleportAvailiable = true;
            doubleJumpAvailiable = true;
            clingTimer = clingTimerReset;

            PlayerState = playerStates.airborn;
            return;
        }

        else if (Input.IsActionJustPressed("jump"))
        {
            doubleJumpAvailiable = true;
            teleportAvailiable = true;
            clingTimer = clingTimerReset;

            if (!Input.IsActionPressed("down"))
            {
                incomingVelocity.Y = JumpVelocity;
                PlayerState = playerStates.airborn;
            }
            else
            {
                GD.Print("downhop");
                Godot.Vector2 tmp = new Vector2(this.Position.X, this.Position.Y + 10);
                this.Position = tmp;
            }
            cyoteTimer = 0.0d;
            return;

        }

        else if (Input.IsActionJustPressed("teleport"))
        {
            teleportAvailiable = false;
            doubleJumpAvailiable = true;
            clingTimer = clingTimerReset;

            PlayerState = playerStates.teleporting;
            teleportTimer = teleportTimerReset;
            return;
        }
        else if (Input.IsActionPressed("fire"))
        {
            if (bulletTimer <= 0.0f)
            {
                direction = Godot.Vector2.Zero;
                aimingSprite.Visible = true;
                //do aimingDirrection rotation thingy here
                if (!sprite_2d.FlipH)
                {
                    if (aimingRise)
                    {
                        if (aimingLynchpin.RotationDegrees >= -80.0f)
                        {
                            aimingLynchpin.Rotate(-(float)Mathf.DegToRad(aimingRotationSpeed * incomingDelta));
                        }
                        else
                        {
                            aimingLynchpin.RotationDegrees = -80.0f;
                            aimingRise = !aimingRise;
                        }
                    }
                    else
                    {
                        if (aimingLynchpin.RotationDegrees < 0.0f)
                        {
                            aimingLynchpin.Rotate((float)Mathf.DegToRad(aimingRotationSpeed * incomingDelta));
                        }
                        else
                        {
                            aimingLynchpin.RotationDegrees = 0.0f;
                            aimingRise = !aimingRise;
                        }
                    }
                }
                else
                {
                    if (aimingRise)
                    {
                        if (aimingLynchpin.RotationDegrees <= 260.0f)
                        {
                            aimingLynchpin.Rotate((float)Mathf.DegToRad(aimingRotationSpeed * incomingDelta));
                        }
                        else
                        {
                            aimingLynchpin.RotationDegrees = 260.0f;
                            aimingRise = !aimingRise;
                        }
                    }
                    else
                    {
                        if (aimingLynchpin.RotationDegrees >= 180.0f)
                        {
                            aimingLynchpin.Rotate(-(float)Mathf.DegToRad(aimingRotationSpeed * incomingDelta));
                        }
                        else
                        {
                            aimingLynchpin.RotationDegrees = 180.0f;
                            aimingRise = !aimingRise;
                        }
                    }
                }
            }
        }
        else if (Input.IsActionJustReleased("fire"))
        {
            fireBullet();
        }
        else if (Input.IsActionJustPressed("crouch"))
        {

            //GD.Print("entering CROUCHED State");
            StandingCollision.Disabled = true;
            CrouchingCollision.Disabled = false;
            PlayerState = playerStates.crouching;
        }
        // Get the input direction and handle the movement/deceleration.
        if (direction != Godot.Vector2.Zero)
        {
            //add the currently input dirrection to our  velocity
            incomingVelocity.X = Mathf.MoveToward(Velocity.X, direction.X * Speed, Acceleration);
            //turn the sprite to face the current inputted dirrection
            sprite_2d.FlipH = direction.X < 0;
            if (direction.X < 0 && !Input.IsActionPressed("fire"))
            {
                if (aimingLynchpin.RotationDegrees != 180.0f)
                {
                    aimingLynchpin.Rotate((float)Mathf.DegToRad(180.0d));
                }
            }
            else if (direction.X > 0 && !Input.IsActionPressed("fire"))
            {
                if (aimingLynchpin.RotationDegrees != 0.0f)
                {
                    aimingLynchpin.Rotate((float)Mathf.DegToRad(-180.0d));
                }
            }
        }
        else
        {
            incomingVelocity.X = Mathf.MoveToward(Velocity.X, 0, Deceleration);
        }
        if (incomingVelocity.X != 0.0f)
            sprite_2d.Animation = "running";
        else
            sprite_2d.Animation = "default";

    }
    private void doCrouchingPhysics(ref Godot.Vector2 incomingVelocity, double incomingDelta)
    {
        //add gravity
        incomingVelocity.Y += gravity * (float)incomingDelta;
        //if touching ground, refresh cyote timer, if not, decrease it
        cyoteTimer = IsOnFloor() ? CyoteTime : -incomingDelta;

        if (cyoteTimer == 0.0d)
        {
            teleportAvailiable = true;
            doubleJumpAvailiable = true;
            clingTimer = clingTimerReset;
            StandingCollision.Disabled = false;
            CrouchingCollision.Disabled = true;
            PlayerState = playerStates.airborn;
            return;
        }

        if (Input.IsActionJustPressed("jump"))
        {
            doubleJumpAvailiable = true;
            teleportAvailiable = true;
            clingTimer = clingTimerReset;
            StandingCollision.Disabled = false;
            CrouchingCollision.Disabled = true;
            PlayerState = playerStates.airborn;
            incomingVelocity.Y = JumpVelocity * 1.5f;
            cyoteTimer = 0.0d;
            return;

        }

        if (Input.IsActionJustPressed("teleport"))
        {
            teleportAvailiable = false;
            doubleJumpAvailiable = true;
            clingTimer = clingTimerReset;

            StandingCollision.Disabled = false;
            CrouchingCollision.Disabled = true;

            PlayerState = playerStates.teleporting;
            teleportTimer = teleportTimerReset;
            return;
        }

        // Get the input direction and handle the movement/deceleration.
        direction = Input.GetVector("left", "right", "up", "down");
        if (direction != Godot.Vector2.Zero)
        {
            //add the currently input dirrection to our  velocity
            incomingVelocity.X = Mathf.MoveToward(Velocity.X, direction.X * (Speed / 2.0f), Deceleration / 3.0F);
            //turn the sprite to face the current inputted dirrection
            sprite_2d.FlipH = direction.X < 0;
        }
        else
        {
            incomingVelocity.X = Mathf.MoveToward(Velocity.X, 0, Deceleration);
        }

        if (!Input.IsActionPressed("crouch"))
        {
            if (ShapeCast.IsColliding())
            {
                //GD.Print("Collision Detected");
            }
            else
            {
                enterGroundedState();
            }
        }

        sprite_2d.Animation = "crouching";
    }

    private void doAirbornPhysics(ref Godot.Vector2 incomingVelocity, double incomingDelta)
    {
        //add gravity
        incomingVelocity.Y += gravity * (float)incomingDelta;
        direction = Input.GetVector("left", "right", "up", "down");
        firstClingTimer -= incomingDelta;
        if (IsOnFloor())
        {
            enterGroundedState();
            return;
        }
        if (IsOnWall())
        {
            if (firstClingTimer <= 0.0d)
            {
                if (!Input.IsActionPressed("down"))
                {
                    enterClingingState();
                    return;
                }
                else
                {

                }
            }
        }
        if (!Input.IsActionPressed("jump"))
        {
            if (incomingVelocity.Y < 0.0f)
            {
                incomingVelocity.Y += gravity * 4 * (float)incomingDelta;
            }
        }

        if (Input.IsActionJustPressed("teleport"))
        {
            if (teleportAvailiable)
            {
                PlayerState = playerStates.teleporting;
                teleportAvailiable = false;
                teleportTimer = teleportTimerReset;
                //GD.Print("entering Teleporting State");
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
        if (direction != Godot.Vector2.Zero)
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

        if (doubleJumpAvailiable)
            sprite_2d.Animation = "jumping";
        else
            sprite_2d.Animation = "doubleJump";
    }

    private void doClingingPhysics(ref Godot.Vector2 incomingVelocity, double incomingDelta)
    {


        if (IsOnFloor())
        {
            enterGroundedState();
            return;
        }
        //add gravity at 1/3 the normal value due to cat claws stuck in the wall we are clinging to
        if (clingTimer <= 0)
            incomingVelocity.Y += gravity / 3 * (float)incomingDelta;
        else
            clingTimer -= incomingDelta;

        if (Input.IsActionJustPressed("jump"))
        {
            direction = Input.GetVector("left", "right", "up", "down");
            //push away from the wall slightly and become airborn
            if (Input.IsActionPressed("down"))
            {
                PlayerState = playerStates.airborn;
            }
            else if (Input.IsActionPressed("left"))
            {
                incomingVelocity.Y = JumpVelocity;
                if (wallToRight == false)
                {
                    incomingVelocity.X = (Speed / 2);
                }
                else
                {
                    incomingVelocity.X = -Speed;
                }
                PlayerState = playerStates.airborn;
            }
            else if (Input.IsActionPressed("right"))
            {
                incomingVelocity.Y = JumpVelocity;
                if (wallToRight == true)
                {
                    incomingVelocity.X = -(Speed / 2);
                }
                else
                {
                    incomingVelocity.X = Speed;
                }
                PlayerState = playerStates.airborn;
            }
        }
    }

    private void doTeleportingPhysics(ref Godot.Vector2 incomingVelocity, double incomingDelta)
    {
        // do not add gravity. gravity does not apply to teleportation

        if (!sprite_2d.FlipH)
        {
            finalVelocity.X = dashSpeed;
            finalVelocity.Y = 0.0f;
        }
        else
        {
            finalVelocity.X = -dashSpeed;
            finalVelocity.Y = 0.0f;
        }

        if ((teleportTimer -= incomingDelta) <= 0.0d)
        {
            finalVelocity = Stop;
            if (IsOnFloor())
            {
                enterGroundedState();
            }
            else if (IsOnWall())
            {
                enterClingingState();
            }
            else
            {
                enterAirbornState();
            }
        }
    }
    private void doDamagedPhysics(ref Godot.Vector2 incomingVelocity, double incomingDelta)
    {
        incomingVelocity.Y += gravity * (float)incomingDelta;
    }

    private void enterGroundedState()
    {
        cyoteTimer = CyoteTime;
        clingTimer = clingTimerReset;
        firstClingTimer = FirstClingReset;
        doubleJumpAvailiable = true;
        teleportAvailiable = true;
        CrouchingCollision.Disabled = true;
        StandingCollision.Disabled = false;
        PlayerState = playerStates.grounded;
    }
    private void enterClingingState()
    {
        determineDirrectionOfWall();
        finalVelocity = Stop;
        PlayerState = playerStates.clinging;
    }
    private void enterAirbornState()
    {
        PlayerState = playerStates.airborn;
    }
    public bool setValues(int incomingHealth = startingHealth)
    {
        GD.Print("Setting Values");
        Health = incomingHealth;
        //MessageManager.instance.sendNewHealthTotalToUI(Health);
        return true;
    }
    public void DamagePLayer(float DamageOriginX = 0.0f, float DamageOriginY = 0.0f)
    {
        if (damagable)
        {
            //GD.Print("DAMAGED!");
            Health--;
            MessageManager.instance.sendNewHealthTotalToUI(Health);
            damagable = false;
            damageTimer = DamageRecoveryReset;
            if (Health <= 0)
            {
                GD.Print("DEATH!");
                Health = MaxHealth;
                GD.Print("Life restored!");

            }
            else
            {
                PlayerState = playerStates.damaged;

                //work out the inverse dirrection that the damage is coming from, as a normalized Vector
                var tmpVelocity = new Godot.Vector2((this.Position.X - DamageOriginX), (this.Position.Y - DamageOriginY)).Normalized();
                tmpVelocity.X *= DamageReboundForce;
                tmpVelocity.Y *= DamageReboundForce;
                GD.Print("bounce velocity = " + tmpVelocity);
                if (IsOnFloor())
                {
                    if (tmpVelocity.Y >= 0.0f)
                    {
                        //divide by twice damage rebound force as the sudden Acceleration to terminal velocity upwards is hillarious but not fun to play
                        tmpVelocity.Y = -(gravity / (DamageReboundForce * 4));
                        GD.Print("Gravity = " + gravity + "end bounce velocity = " + tmpVelocity);
                    }
                }
                Velocity = tmpVelocity * DamageReboundForce;
                //GD.Print("output of damage Escape Vector X: " + Velocity.X + "   Y: " + Velocity.Y);
            }
        }
    }
    public void HealPlayer(int amount = 1)
    {
        Health += amount;
        if (Health > MaxHealth)
        {
            Health = MaxHealth;
        }
    }
    private void flashPlayer()
    {
        if (sprite_2d.Modulate.A == 0.5f)
        {
            sprite_2d.Modulate = solid;
        }
        else
        {
            sprite_2d.Modulate = semiTransparent;
        }
    }
    private void determineDirrectionOfWall()
    {
        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            if (GetSlideCollision(i).GetCollider().GetType().FullName == "Godot.TileMapLayer")
            {
                GD.Print("detected TileMapLayer");
                wallToRight = GetSlideCollision(i).GetNormal().X > 0 ? false : true;
                GD.Print("wall is to the right = " + wallToRight);
            }
        }
    }
    private void fireBullet()
    {
        if (bulletTimer <= 0.0d)
        {
            Bullet shot = bullet.Instantiate<Bullet>();
            shot.setup(lerp(aimingLynchpin.GlobalPosition, aimingDirrection.GlobalPosition, 0.5f),
                aimingDirrection.GlobalPosition - aimingLynchpin.GlobalPosition, Bullet.bulletTypes.basic);
            Owner.AddChild(shot);
            bulletTimer = BulletResetTime;
            aimingLynchpin.RotationDegrees = sprite_2d.FlipH ? 180.0f : 0.0f;
            aimingSprite.Visible = false;
        }
    }
    public double GetbulletTimePercentageDecimal()
    {
        if (bulletTimer > 0.0d)
        {
            return (bulletTimer / BulletResetTime);
        }
        else
        {
            return 0.0d;
        }
    }
    public int enquireCurrentHealth()
    {
        MessageManager.instance.sendNewHealthTotalToUI(Health);
        return Health;
    }
    private float lerp(float firstPoint, float secondPoint, float percentageBetweenTheTwoPoints = 0.5f)
    {
        return firstPoint * (1 - percentageBetweenTheTwoPoints) + secondPoint * percentageBetweenTheTwoPoints;
    }
    private Godot.Vector2 lerp(Godot.Vector2 firstPoint, Godot.Vector2 secondPoint, float percentageBetweenTheTwoPoints = 0.5f)
    {
        Godot.Vector2 result = new Godot.Vector2(lerp(firstPoint.X, secondPoint.X, percentageBetweenTheTwoPoints),
            lerp(firstPoint.Y, secondPoint.Y, percentageBetweenTheTwoPoints));
        return result;
    }
}

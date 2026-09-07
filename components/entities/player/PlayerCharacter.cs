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
    private Godot.Vector2 shotOffset = new (60.0f, 0.0f);
    public PackedScene Bullet { get; set; }
    private Godot.Color semiTransparent = new (1, 1, 1, 0.5f);
    private Godot.Color solid = new (1, 1, 1, 1f);
    private Godot.Vector2 finalVelocity;
    private Godot.Vector2 direction;
    private Godot.Vector2 Stop = new (0, 0);

    private AnimatedSprite2D sprite_2d;
    private Sprite2D aimingSprite;
    private Node2D aimingLynchpin;
    private Node2D aimingDirrection;
    private bool aimingRise = true;
    private CollisionShape2D StandingCollision;
    private CollisionShape2D CrouchingCollision;
    private ShapeCast2D ShapeCastCeilingCheck;
    private ShapeCast2D ShapeCastWallCheck;
    private byte StealthLayerCount = 0;


    /// <Floats>
    /// ////////////////////////////////////////////////////////////////////////////////
    /// </Floats>
    [Export]
    public float Speed = 450.0f;
    [Export(PropertyHint.Range, "0,5.0")]
    public float Acceleration = 1.0f;
    [Export(PropertyHint.Range, "0,20.0")]
    public float Deceleration = 15.0f;
    [Export(PropertyHint.Range, "0,10.0")]
    public float AirDeceleration = 3.3f;
    [Export(PropertyHint.Range, "0,-1000.0")]
    public float JumpVelocity = -600.0f;
    [Export(PropertyHint.Range, "0,10.0")]
    public float JumpAscentionTimer = 3.0f;
    [Export(PropertyHint.Range, "0,5.0")]
    public float JumpHangingTime = 01.0f;
    [Export(PropertyHint.Range, "0,5.0")]
    public float PlayerGravityMultiplier = 1.0f;
    private float JumpHangingTimeTimer = 0.0f;

    [Export]
    private float dashSpeed = 1000.0f;
    [Export(PropertyHint.Range, "0,300.0")]
    private float aimingRotationSpeed = 120.0f;
    [Export(PropertyHint.Range, "0,5.0")]
    private float DamageRecoveryReset = 2.0f;
    private const int HorizontalDamageReboundForce = 150;
    private const int VerticalDamageReboundForce = 300;
    public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
    /// <Doubles>
    /// /////////////////////////////////////////////////////////////////////////////////
    /// </Doubles>
    [Export (PropertyHint.Range, "0.0,1.0")]
    private double CyoteTime = 0.05d;
    [Export(PropertyHint.Range, "0,1.0")]
    private double teleportTimerReset = 0.3d;
    [Export(PropertyHint.Range, "0,10.0")]
    private double clingTimerReset = 1.0d;
    [Export(PropertyHint.Range, "0,10.0")]
    private double BulletResetTime = 2.5d;
    [Export (PropertyHint.Range, "0,1.0")]
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

    private const int NPCCollisionLayer = 7;
    private const int NPCCollisionenabled = 8|16|64|128;
    private const int NPCCollisiondisabled = 8|16|128;
    private const int PlayerLayer = 32;
    private const int RemoveAllLayers = 0;
    /// <Bools>
    /// ////////////////////////////////////////////////////////////////////////////////
    /// </Bools>
    private bool doubleJumpAvailiable = true;
    private bool teleportAvailiable = true;
    private bool wallToRight = false;
    private bool damagable;

    private Vector2 spawnPosition = new(0, 0);
    private double previousPercentage;

    public override void _Ready()
    {
        base._Ready();
        sprite_2d = GetNode<AnimatedSprite2D>($"Sprite2D");
        StandingCollision = GetNode<CollisionShape2D>($"CollisionShapeStanding");
        CrouchingCollision = GetNode<CollisionShape2D>($"CollisionShapeCrouching");
        ShapeCastCeilingCheck = GetNode<ShapeCast2D>("ShapeCast2DCeilingCheck");
        ShapeCastWallCheck = GetNode<ShapeCast2D>("ShapeCast2DWallCheck");
        Bullet = GD.Load<PackedScene>("res://components/entities/player/Bullet.tscn");
        aimingLynchpin = GetNode<Node2D>($"aimingLynchpin");
        aimingDirrection = GetNode<Node2D>($"aimingLynchpin/aimingDirection");
        aimingSprite = GetNode<Sprite2D>($"aimingLynchpin/aimingSprite");
        aimingSprite.Visible = false;
        damagable = true;
        cyoteTimer = CyoteTime;
        teleportTimer = teleportTimerReset;
        clingTimer = clingTimerReset;
        MessageManager.instance.AddPlayerToMessageManager(this);
    }
    public override void _ExitTree()
    {
        base._ExitTree();
        GD.Print("player with ID of " + this.GetInstanceId() + " has been removed");
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
                    FlashPlayer();
                }
            }
        }

        switch (PlayerState)
        {
            case playerStates.grounded:
                DoGroundedPhysics(ref finalVelocity, delta);
                break;
            case playerStates.airborn:
                DoAirbornPhysics(ref finalVelocity, delta);
                break;
            case playerStates.clinging:
                DoClingingPhysics(ref finalVelocity, delta);
                break;
            case playerStates.teleporting:
                DoTeleportingPhysics(delta);
                break;
            case playerStates.crouching:
                DoCrouchingPhysics(ref finalVelocity, delta);
                break;
            case playerStates.damaged:
                DoDamagedPhysics(ref finalVelocity, delta);
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
        double percentage = GetbulletTimePercentageDecimal();
        if (percentage != previousPercentage)
        {
            previousPercentage = percentage;
            MessageManager.instance.SendEnegyPercentageTotalToUI(percentage);
        }
        MoveAndSlide();
    }
    private void DoGroundedPhysics(ref Godot.Vector2 incomingVelocity, double incomingDelta)
    {
        //add gravity
        //incomingVelocity.Y += gravity * (float)incomingDelta;

        //if touching ground, refresh cyote timer, if not, decrease it
        cyoteTimer = IsOnFloor() ? CyoteTime : -incomingDelta;
        if (cyoteTimer <= 0.00d)
        {
            teleportAvailiable = true;
            doubleJumpAvailiable = true;
            clingTimer = clingTimerReset;

            PlayerState = playerStates.airborn;
            return;
        }
        // Get the input direction and handle the movement/deceleration.
        direction = Input.GetVector("left", "right", "up", "down");
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

        if (Input.IsActionJustPressed("jump"))
        {
            EnterAirbornState();
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
                Godot.Vector2 tmp = new(this.Position.X, this.Position.Y + 10);
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
            FireBullet();
        }
        else if (Input.IsActionJustPressed("crouch"))
        {

            StandingCollision.Disabled = true;
            CrouchingCollision.Disabled = false;
            PlayerState = playerStates.crouching;
        }

        if (incomingVelocity.X != 0.0f)
            sprite_2d.Animation = "running";
        else
            sprite_2d.Animation = "default";

    }
    private void DoCrouchingPhysics(ref Godot.Vector2 incomingVelocity, double incomingDelta)
    {
        //add gravity
        incomingVelocity.Y += gravity * PlayerGravityMultiplier *  (float)incomingDelta;
        //if touching ground, refresh cyote timer, if not, decrease it
        cyoteTimer = IsOnFloor() ? CyoteTime : -incomingDelta;

        if (cyoteTimer <= 0.0d)
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
            EnterAirbornState();
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
            if (ShapeCastCeilingCheck.IsColliding())
            {
            }
            else
            {
                EnterGroundedState();
            }
        }

        sprite_2d.Animation = "crouching";
    }

    private void DoAirbornPhysics(ref Godot.Vector2 incomingVelocity, double incomingDelta)
    {
        //add gravity
        //incomingVelocity.Y += gravity * (float)incomingDelta;
        direction = Input.GetVector("left", "right", "up", "down");
        firstClingTimer -= incomingDelta;
        if (IsOnFloor())
        {
            EnterGroundedState();
            return;
        }
        if (IsOnWall())
        {
            if (firstClingTimer <= 0.0d)
            {
                if (!Input.IsActionPressed("down"))
                {
                    EnterClingingState();
                    return;
                }
                else
                {

                }
            }
        }
        if(JumpHangingTimeTimer > 0.0f && incomingVelocity.Y > 0.0f)
        {
            JumpHangingTimeTimer -= (float)incomingDelta;
            incomingVelocity.Y = 0.0f;
        }
        else
        {
            incomingVelocity.Y += gravity * PlayerGravityMultiplier *  (float)incomingDelta;
        }
        if (Input.IsActionJustReleased("jump") && incomingVelocity.Y < 0.0f)
        {
            incomingVelocity.Y = 0.0f;
        }

        if (Input.IsActionJustPressed("teleport"))
        {
            if (teleportAvailiable)
            {
                PlayerState = playerStates.teleporting;
                teleportAvailiable = false;
                teleportTimer = teleportTimerReset;
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

    private void DoClingingPhysics(ref Godot.Vector2 incomingVelocity, double incomingDelta)
    {


        if (IsOnFloor())
        {
            EnterGroundedState();
            return;
        }
        if (!ShapeCastWallCheck.IsColliding())
        {
            EnterAirbornState();
            return;
        }
        //add gravity at 1/3 the normal value due to cat claws stuck in the wall we are clinging to
        if (clingTimer <= 0)
            incomingVelocity.Y += gravity / 3 * (float)incomingDelta;
        else
            clingTimer -= incomingDelta;

        if (Input.IsActionJustPressed("jump"))
        {
            EnterAirbornState();
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

    private void DoTeleportingPhysics(double incomingDelta)
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
                EnterGroundedState();
            }
            else if (IsOnWall())
            {
                EnterClingingState();
            }
            else
            {
                EnterAirbornState();
            }
        }
    }
    private void DoDamagedPhysics(ref Godot.Vector2 incomingVelocity, double incomingDelta)
    {
        incomingVelocity.Y += gravity * (float)incomingDelta;
    }

    private void EnterGroundedState()
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
    private void EnterCrouchingState()
    {
        CrouchingCollision.Disabled = false;
        StandingCollision.Disabled = true;
        PlayerState = playerStates.crouching;
        if (StealthLayerCount > 0)
        {
            MessageManager.instance.SendStealthStatusToSystem(true);
            this.SetCollisionMaskValue(NPCCollisionLayer, false);
            this.CollisionMask = NPCCollisiondisabled;
            this.CollisionLayer = RemoveAllLayers;
        }
    }
    public void IncreaseStealthLayerCount()
    {
        StealthLayerCount++;
        if (StealthLayerCount > 0 && PlayerState == playerStates.crouching)
        {
            MessageManager.instance.SendStealthStatusToSystem(true);
        }
    }
    public void DecreaseStealthLayerCount()
    {
        StealthLayerCount--;
        if (StealthLayerCount <= 0)
        {
            MessageManager.instance.SendStealthStatusToSystem(false);
            this.SetCollisionMaskValue(NPCCollisionLayer, true);
            this.CollisionMask = NPCCollisionenabled;
            this.CollisionLayer = PlayerLayer;
            StealthLayerCount = 0;
        }
    }
    private void EnterClingingState()
    {
        DetermineDirrectionOfWall();
        finalVelocity = Stop;
        PlayerState = playerStates.clinging;
    }
    private void EnterAirbornState()
    {
        JumpHangingTimeTimer = JumpHangingTime;    
        PlayerState = playerStates.airborn;
    }
    public void DamagePLayer(float DamageOriginX = 0.0f, float DamageOriginY = 0.0f, int damage = 1)
    {
        if (damagable)
        {
            //Health -= damage;
            MessageManager.instance.DecreaseUICurrentHealthBy(damage);
            damagable = false;
            damageTimer = DamageRecoveryReset;
            PlayerState = playerStates.damaged;

            //work out the dirrection that the damage is coming from, 
            //if the damage comes from the positive X direction, tmpX is the negative dirrection, and visa versa
            int tmpx = this.Position.X > DamageOriginX ? 1 : -1;
            //Same for the Y
            int tmpy = this.Position.Y > DamageOriginY ? 1 : -1;
            //if player is on floor always bounce upwards
            if(IsOnFloor())
            {
                tmpy = -1; //Y -1 is UP
            }
            //send player recoiling to the opposite quater to the damage's origin.
            Godot.Vector2 tmpVelocity = new (tmpx * HorizontalDamageReboundForce, tmpy * VerticalDamageReboundForce);             
            Velocity = tmpVelocity;
        }
    }
    private void FlashPlayer()
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
    private void DetermineDirrectionOfWall()
    {
        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            if (GetSlideCollision(i).GetCollider() is TileMapLayer)
            {
                wallToRight = GetSlideCollision(i).GetNormal().X <= 0;
                //cast the wall detection ray out to the side that the wall is on
                if (wallToRight)
                {
                    ShapeCastWallCheck.TargetPosition = new Vector2(15.0f, 0.0f);
                }
                else           
                {
                    ShapeCastWallCheck.TargetPosition = new Vector2(-15.0f, 0.0f); 
                }
            }
        }
    }
    private void FireBullet()
    {
        if (bulletTimer <= 0.0d)
        {
            Bullet shot = Bullet.Instantiate<Bullet>();
            shot.setup(Lerp(aimingLynchpin.GlobalPosition, aimingDirrection.GlobalPosition, 0.5f),
                aimingDirrection.GlobalPosition - aimingLynchpin.GlobalPosition, global::Bullet.bulletTypes.basic);
            Owner.AddChild(shot);
            bulletTimer = BulletResetTime;
            aimingLynchpin.RotationDegrees = sprite_2d.FlipH ? 180.0f : 0.0f;
            aimingSprite.Visible = false;
        }
    }
    public void ResetPlayerToSpawnPosition()
    {
        this.Position = spawnPosition;
        Velocity = Stop;
    }
    public void SetSpawnPosition(Vector2 incomingPosition)
    {
        spawnPosition = incomingPosition;
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
    private float Lerp(float firstPoint, float secondPoint, float percentageBetweenTheTwoPoints = 0.5f)
    {
        return firstPoint * (1 - percentageBetweenTheTwoPoints) + secondPoint * percentageBetweenTheTwoPoints;
    }
    private Godot.Vector2 Lerp(Godot.Vector2 firstPoint, Godot.Vector2 secondPoint, float percentageBetweenTheTwoPoints = 0.5f)
    {
        Godot.Vector2 result = new (Lerp(firstPoint.X, secondPoint.X, percentageBetweenTheTwoPoints),
            Lerp(firstPoint.Y, secondPoint.Y, percentageBetweenTheTwoPoints));
        return result;
    }
}

using Godot;
using System;
using System.Runtime.CompilerServices;
public enum CurrentState
{
    patroling,
    charging,
    jumping,
    takingBreak,
    stunned
}

public partial class EnemyPatrol : CharacterBody2D
{
    [Export]
    //generic walking speed
    public float Speed = 350.0f;
    [Export]
    //acceleration towards top speed from less than top speed
    public float acceleration = 15.0f;
    [Export]
    //initial timer before guard will "take a break"
    private double idleTimer = 60.0d;
    [Export]
    // time enemy will remain stunned when conditions Met
    private double stunTime = 3.0d;
    [Export]
    private bool canHurtPlayer = true;
    [Export]
    private bool canBeStunned = true;
    [Export]
    private bool canDetectPlayer = true;
    [Export]
    private bool canCharge = true;
    [Export]
    private bool canInstantKill = false;
    [Export]
    private bool canJump = false;

    private const double changeDirectionTimer = 0.5d;
    private double changeDirectionTimerCurrent;

    private bool wallToRight = false;
    private Godot.Vector2 finalVelocity;
    private bool direction = true;
    private double breakTimer = 0.0d;
    private bool onBreak = false;
    private Godot.Vector2 Stop = new Godot.Vector2(0, 0);
    private CollisionShape2D CollisionBody;
    private ShapeCast2D EdgeDetectionCast;
    private ShapeCast2D WallDetectionCast;
    private ShapeCast2D JumpDetectionCast1;
    private ShapeCast2D JumpDetectionCast2;
    private Area2D AreaDetectionRight;
    private Area2D HurtBox;

    private Shape2D detectionShapeRight;
    public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
    private Random rnd = new Random();
    private AnimatedSprite2D sprite_2d;
    private Node2D enemyNode;

    private Node2D Player = null;

    private double attackTimer = 0.0d;
    private double attackOn;
    private bool attacking;
    private float attackRange = 80;
    private bool stunned = false;
    private double stunTimer = 0.0d;
    private double flashTimer = 0.0d;
    private Godot.Vector2 aboutFace;

    private bool PlayerInHurtbox;

    public CurrentState CurrentState;


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {

        AreaDetectionRight = GetNode<Area2D>($"DetectionAreaRight");
        AreaDetectionRight.BodyEntered += (body) => DetectPlayer(body);
        HurtBox = GetNode<Area2D>($"HurtBox2D");
        HurtBox.BodyEntered += (body) => PlayerEnteredHurtbox(body);
        PlayerInHurtbox = false;
        sprite_2d = GetNode<AnimatedSprite2D>($"Sprite2D");
        enemyNode = this;
        aboutFace = new(-1, enemyNode.Scale.Y);
        CollisionBody = GetNode<CollisionShape2D>($"CollisionShapeStanding");
        EdgeDetectionCast = GetNode<ShapeCast2D>($"EdgeDetectionShapeCast2D");
        WallDetectionCast = GetNode<ShapeCast2D>($"WallDetectionShapeCast2D");
        JumpDetectionCast1 = GetNode<ShapeCast2D>($"JumpablePlatformShapeCast2D");
        JumpDetectionCast2 = GetNode<ShapeCast2D>($"JumpablePlatformShapeCast2D2");
        attacking = false;
        attackOn = 2.0d; //this will set the timer so that the enemy attacks after 2 seconds of being within range and detecting the player
        changeDirectionTimerCurrent = changeDirectionTimer;
        CurrentState = CurrentState.patroling;
        MessageManager.instance.addToEnemyDictionary(this);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _PhysicsProcess(double delta)
    {
        finalVelocity = Velocity;
        switch (CurrentState)
        {
            case CurrentState.patroling:
                Patrol(delta, ref finalVelocity);
                break;
            case CurrentState.charging:
                Charging(delta, ref finalVelocity);
                break;
            case CurrentState.jumping:
                Jumping(delta, ref finalVelocity);
                break;
            case CurrentState.takingBreak:
                TakingBreak(delta, ref finalVelocity);
                break;
            case CurrentState.stunned:
                Stunned(delta, ref finalVelocity);
                break;
        }
        Velocity = finalVelocity;
        changeDirectionTimerCurrent -= delta;
        MoveAndSlide();
    }
    private void Patrol(double incomingDelta, ref Godot.Vector2 incomingVelocity)
    {
        incomingVelocity.Y += gravity * (float)incomingDelta;
        incomingVelocity.X = Mathf.MoveToward(incomingVelocity.X, (direction ? 1.0f : -1.0f) * (Speed / 2), acceleration);
        idleTimer -= incomingDelta;
        if (WallDetectionCast.IsColliding())
        {
            //Velocity = Godot.Vector2.Zero;
            if (canJump)
            {
                if (JumpDetectionCast1.IsColliding())
                {
                    GD.Print("jump DetectionCast 1 Colliding");
                    if (!JumpDetectionCast2.IsColliding())
                    {
                        GD.Print("could jump");
                        //switch State HERE!
                    }
                    else
                    {
                        GD.Print("Cant Jump");
                    }
                    
                }
            }
            direction = !direction;
            Velocity = Godot.Vector2.Zero;
            FlipEntity();
        }
        if(!EdgeDetectionCast.IsColliding())
        {
            direction = !direction;
            Velocity = Godot.Vector2.Zero;
            FlipEntity();
        }
    }
    private void Charging(double incomingDelta, ref Godot.Vector2 incomingVelocity)
    {
        incomingVelocity.Y += gravity * (float)incomingDelta;
        if (IsOnFloor())
        {
            incomingVelocity.X = Mathf.MoveToward(incomingVelocity.X, (direction ? 1.0f : -1.0f) * Speed, acceleration);
        }
        if(IsOnWall())
        {
            SwitchToStunState();
        }
    }
    private void Jumping(double incomingDelta, ref Godot.Vector2 incomingVelocity)
    {
        
    }
    private void TakingBreak(double incomingDelta, ref Godot.Vector2 incomingVelocity)
    {
        incomingVelocity = Godot.Vector2.Zero;
        breakTimer -= incomingDelta;
        if (breakTimer <= 0.0d)
        {
            idleTimer = rnd.Next(20, 120);
            onBreak = false;
        }
    }
    private void Stunned(double incomingDelta, ref Godot.Vector2 incomingVelocity)
    {
        stunTimer -= incomingDelta;
        if (stunTimer <= 0.0d)
        {
            SwitchToPatrolState();
        }
        incomingVelocity = Godot.Vector2.Zero;
    }


    private void DetectPlayer(Node2D body)
    {
        if (body.Name.ToString() == "Player")
        {
            Player = body;
            onBreak = false;
            idleTimer = rnd.Next(20, 120);
            if (canCharge)
            {
                if(LineOfSightCheck(Player))
                {
                    SwitchToChargeState();
                }
            }
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
            if ((ulong)sightCheck["collider_id"] == target.GetInstanceId())
            {
                return true;
            }
        }
        return false;
    }

    private void PlayerEnteredHurtbox(Node2D body)
    {
        if (body.Name.ToString() == "Player")
        if(canHurtPlayer)
        {
            if (canInstantKill)
            {
                MessageManager.instance.KillPlayer();
            }
            else
            {
                MessageManager.instance.DamagePlayer(this.Position);
            }
        }
        
    }

    private void FlipEntity()
    {
        if (changeDirectionTimerCurrent > 0.0d)
        {
            GD.Print("Tried to flip but timer not ready");
            return;
        }
        GD.Print("Flipping");
        enemyNode.Scale *= aboutFace;
    }

    private void SwitchToPatrolState()
    {
        GD.Print("Entering Patrol State");
        CurrentState = CurrentState.patroling;
    }
    private void SwitchToChargeState()
    {
        GD.Print("Entering Charge State");
        CurrentState = CurrentState.charging;
    }
    private void SwitchToJumpState()
    {
        GD.Print("Entering Jump State");
        CurrentState = CurrentState.jumping;
    }
    private void SwitchToBreakState()
    {
        GD.Print("Entering Break State");
        CurrentState = CurrentState.takingBreak;
    }
    public void SwitchToStunState()
    {
        GD.Print("Entering Stunned State");
        stunTimer = stunTime;
        CurrentState = CurrentState.stunned;
    }
}
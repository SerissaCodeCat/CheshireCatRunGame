using Godot;
using System;
using System.Runtime.CompilerServices;

/// <summary>
/// The behavioural states an <see cref="EnemyPatrol"/> can be in at any given time.
/// </summary>
public enum CurrentState
{
    patroling,
    charging,
    jumping,
    takingBreak,
    stunned
}

/// <summary>
/// Controls a patrolling enemy that walks back and forth, detects the player by line of sight,
/// charges and attacks when the player is spotted, can be stunned, and can optionally take
/// idle breaks or jump across gaps.
/// </summary>
public partial class EnemyPatrol : CharacterBody2D
{
    /// <summary>Base walking speed, in pixels per second.</summary>
    [Export]
    public float Speed = 350.0f;

    /// <summary>Rate at which the enemy accelerates toward its target speed.</summary>
    [Export]
    public float acceleration = 15.0f;

    /// <summary>
    /// Initial delay, in seconds, before the enemy will enter <see cref="CurrentState.takingBreak"/>.
    /// This value is overwritten with a random duration at runtime; see remarks in code review.
    /// </summary>
    [Export]
    private double idleTimer;

    /// <summary>How long, in seconds, the enemy remains stunned once <see cref="SwitchToStunState"/> is called.</summary>
    [Export]
    private double stunTime = 3.0d;

    /// <summary>If true, this enemy can damage the player on contact.</summary>
    [Export]
    private bool canHurtPlayer = true;

    /// <summary>If true, this enemy is able to enter <see cref="CurrentState.stunned"/>.</summary>
    [Export]
    private bool canBeStunned = true;

    /// <summary>If true, this enemy will actively look for the player via its detection areas.</summary>
    [Export]
    private bool canDetectPlayer = true;

    /// <summary>If true, this enemy will charge toward the player once spotted.</summary>
    [Export]
    private bool canCharge = true;

    /// <summary>If true, contact with the player results in an instant kill rather than normal damage.</summary>
    [Export]
    private bool canInstantKill = false;

    /// <summary>If true, this enemy will periodically stop and idle while patrolling.</summary>
    [Export]
    private bool canTakeBreaks = true;

    /// <summary>If true, this enemy is permitted to jump across small gaps while patrolling.</summary>
    [Export]
    private bool canJump = false;

    /// <summary>Upward impulse applied, in pixels per second, when this enemy jumps.</summary>
    [Export]
    private float jumpForce = 250.0f;

    /// <summary>If true, the enemy faces left immediately on spawning instead of the default right-facing orientation.</summary>
    [Export]
    private bool faceLeftAtStart = false;

    /// <summary>Minimum time, in seconds, that must pass between direction changes, to prevent rapid flip-flopping.</summary>
    private const double changeDirectionTimer = 0.5d;

    /// <summary>Countdown timer tracking time remaining until another direction change is permitted.</summary>
    private double changeDirectionTimerCurrent;

    /// <summary>Unused at present. Intended to flag whether a wall is present to the enemy's right.</summary>
    private bool wallToRight = false;

    /// <summary>Velocity accumulated and applied during the current physics tick.</summary>
    private Godot.Vector2 finalVelocity;

    /// <summary>Current facing/movement direction. True represents facing/moving right, false represents left.</summary>
    private bool direction = true;

    /// <summary>Countdown timer tracking remaining time in <see cref="CurrentState.takingBreak"/>.</summary>
    private double breakTimer = 0.0d;

    /// <summary>Unused at present.</summary>
    private bool onBreak = false;

    /// <summary>Unused constant zero vector.</summary>
    private Godot.Vector2 Stop = new Godot.Vector2(0, 0);

    /// <summary>The enemy's standing collision shape.</summary>
    private CollisionShape2D CollisionBody;

    /// <summary>Detects the floor edge ahead, on the right side, so the enemy does not walk off ledges.</summary>
    private ShapeCast2D EdgeDetectionCastRight;

    /// <summary>Detects walls ahead, on the right side, so the enemy can turn around or jump.</summary>
    private ShapeCast2D WallDetectionCastRight;

    /// <summary>First of two shape casts used to determine whether a gap ahead, on the right side, can be jumped.</summary>
    private ShapeCast2D JumpDetectionCast1Right;

    /// <summary>Second of two shape casts used to determine whether a gap ahead, on the right side, can be jumped.</summary>
    private ShapeCast2D JumpDetectionCast2Right;

    /// <summary>Detects the floor edge ahead, on the left side, so the enemy does not walk off ledges.</summary>
    private ShapeCast2D EdgeDetectionCastLeft;

    /// <summary>Detects walls ahead, on the left side, so the enemy can turn around or jump.</summary>
    private ShapeCast2D WallDetectionCastLeft;

    /// <summary>First of two shape casts used to determine whether a gap ahead, on the left side, can be jumped.</summary>
    private ShapeCast2D JumpDetectionCast1Left;

    /// <summary>Second of two shape casts used to determine whether a gap ahead, on the left side, can be jumped.</summary>
    private ShapeCast2D JumpDetectionCast2Left;

    /// <summary>Detection area covering the enemy's right side, used to spot the player.</summary>
    private Area2D AreaDetectionRight;

    /// <summary>Detection area covering the enemy's left side, used to spot the player.</summary>
    private Area2D AreaDetectionLeft;

    /// <summary>Area used to detect when the player is close enough to take damage from this enemy.</summary>
    private Area2D HurtBox;

    /// <summary>Gravity applied to this enemy every physics tick, read from the project's physics settings.</summary>
    public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

    /// <summary>Random number generator used for idle timer durations.</summary>
    private Random rnd = new Random();

    /// <summary>The enemy's animated sprite, used for facing direction and the stun flash effect.</summary>
    private AnimatedSprite2D sprite_2d;

    /// <summary>Cached reference to this node, used when registering with <see cref="MessageManager"/>.</summary>
    private Node2D enemyNode;

    /// <summary>Reference to the player node once detected. Null until the player has been spotted at least once.</summary>
    private Node2D Player = null;

    /// <summary>Unused at present. Intended to track time spent in an attack.</summary>
    private double attackTimer = 0.0d;

    /// <summary>Unused at present. Intended to represent the delay, in seconds, before an attack triggers.</summary>
    private double attackOn;

    /// <summary>Unused at present. Intended to flag whether an attack is currently in progress.</summary>
    private bool attacking;

    /// <summary>Unused at present. Intended to represent the distance within which this enemy can attack.</summary>
    private float attackRange = 80;

    /// <summary>Unused at present.</summary>
    private bool stunned = false;

    /// <summary>Countdown timer tracking time remaining in <see cref="CurrentState.stunned"/>.</summary>
    private double stunTimer = 0.0d;

    /// <summary>Countdown timer controlling the sprite's visibility flicker while stunned.</summary>
    private double flashTimer = 0.0d;

    /// <summary>True while the player is currently standing inside this enemy's <see cref="HurtBox"/>.</summary>
    private bool PlayerInHurtbox;

    /// <summary>The enemy's current behavioural state.</summary>
    public CurrentState CurrentState;

    /// <summary>
    /// Godot lifecycle callback. Caches node references, wires up detection signals, sets the
    /// initial facing direction, and registers this enemy with <see cref="MessageManager"/>.
    /// </summary>
    /// <example>
    /// Called automatically by the Godot engine when this node enters the scene tree.
    /// It is not intended to be called directly from other scripts.
    /// </example>
    public override void _Ready()
    {
        // initial set up to face right and monitor for players on the right.
        AreaDetectionRight = GetNode<Area2D>($"DetectionAreaRight");
        AreaDetectionRight.BodyEntered += (body) => DetectPlayer(body);
        AreaDetectionLeft = GetNode<Area2D>($"DetectionAreaLeft");
        AreaDetectionLeft.BodyEntered += (body) => DetectPlayer(body);
        AreaDetectionLeft.Monitoring = false;
        AreaDetectionRight.Monitoring = true;

        HurtBox = GetNode<Area2D>($"HurtBox2D");
        HurtBox.BodyEntered += (body) => PlayerEnteredHurtbox(body);
        PlayerInHurtbox = false;
        sprite_2d = GetNode<AnimatedSprite2D>($"Sprite2D");
        enemyNode = this;
        CollisionBody = GetNode<CollisionShape2D>($"CollisionShapeStanding");
        // set up detection shapes for right and left sides
        EdgeDetectionCastRight = GetNode<ShapeCast2D>($"EdgeDetectionShapeCast2DRight");
        WallDetectionCastRight = GetNode<ShapeCast2D>($"WallDetectionShapeCast2DRight");
        JumpDetectionCast1Right = GetNode<ShapeCast2D>($"JumpablePlatformShapeCast2DRight");
        JumpDetectionCast2Right = GetNode<ShapeCast2D>($"JumpablePlatformShapeCast2D2Right");
        EdgeDetectionCastLeft = GetNode<ShapeCast2D>($"EdgeDetectionShapeCast2DLeft");
        WallDetectionCastLeft = GetNode<ShapeCast2D>($"WallDetectionShapeCast2DLeft");
        JumpDetectionCast1Left = GetNode<ShapeCast2D>($"JumpablePlatformShapeCast2DLeft");
        JumpDetectionCast2Left = GetNode<ShapeCast2D>($"JumpablePlatformShapeCast2D2Left");
        // enable detection shapes, Right Side only at start.
        EdgeDetectionCastRight.Enabled = true;
        WallDetectionCastRight.Enabled = true;
        JumpDetectionCast1Right.Enabled = true;
        JumpDetectionCast2Right.Enabled = true;
        EdgeDetectionCastLeft.Enabled = false;
        WallDetectionCastLeft.Enabled = false;
        JumpDetectionCast1Left.Enabled = false;
        JumpDetectionCast2Left.Enabled = false;

        attacking = false;
        attackOn = 2.0d; // this will set the timer so that the enemy attacks after 2 seconds of being within range and detecting the player
        changeDirectionTimerCurrent = changeDirectionTimer;
        CurrentState = CurrentState.patroling;
        MessageManager.instance.AddToEnemyDictionary(this);
        idleTimer = rnd.Next(45, 120);
        if (faceLeftAtStart)
        {
            FlipEntity();
        }
    }

    /// <summary>
    /// Godot lifecycle callback. Runs the behaviour for the current <see cref="CurrentState"/>,
    /// applies the resulting velocity, and advances the direction-change cooldown.
    /// </summary>
    /// <param name="delta">Elapsed time, in seconds, since the previous physics tick.</param>
    /// <example>
    /// Called automatically by the Godot engine every physics tick.
    /// It is not intended to be called directly from other scripts.
    /// </example>
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

    /// <summary>
    /// Moves the enemy back and forth at half speed, checks for walls, ledges, and the idle
    /// timer, and switches state accordingly.
    /// </summary>
    /// <param name="incomingDelta">Elapsed time, in seconds, since the previous physics tick.</param>
    /// <param name="incomingVelocity">The enemy's velocity for this tick, modified in place.</param>
    private void Patrol(double incomingDelta, ref Godot.Vector2 incomingVelocity)
    {
        incomingVelocity.Y += gravity * (float)incomingDelta;
        incomingVelocity.X = Mathf.MoveToward(incomingVelocity.X, (direction ? 1.0f : -1.0f) * (Speed / 2), acceleration);
        if (canTakeBreaks)
        {
            idleTimer -= incomingDelta;
            if (idleTimer <= 0.0d)
            {
                SwitchToBreakState();
            }
        }
        if ((WallDetectionCastRight.IsColliding() && WallDetectionCastRight.Enabled == true)
        || (WallDetectionCastLeft.IsColliding() && WallDetectionCastLeft.Enabled == true))
        {
            if (canJump)
            {
                if (SwitchToJumpState(incomingDelta, ref incomingVelocity))
                {
                    return;
                }
            }
            Velocity = Godot.Vector2.Zero;
            FlipEntity();
        }
        if ((!EdgeDetectionCastRight.IsColliding() && EdgeDetectionCastRight.Enabled == true)
        || (!EdgeDetectionCastLeft.IsColliding() && EdgeDetectionCastLeft.Enabled == true))
        {
            Velocity = Godot.Vector2.Zero;
            FlipEntity();
        }
    }

    /// <summary>
    /// Accelerates the enemy toward full speed while charging at the player, and switches to
    /// the stunned state if a wall is struck.
    /// </summary>
    /// <param name="incomingDelta">Elapsed time, in seconds, since the previous physics tick.</param>
    /// <param name="incomingVelocity">The enemy's velocity for this tick, modified in place.</param>
    private void Charging(double incomingDelta, ref Godot.Vector2 incomingVelocity)
    {
        incomingVelocity.Y += gravity * (float)incomingDelta;
        if (IsOnFloor())
        {
            incomingVelocity.X = Mathf.MoveToward(incomingVelocity.X, (direction ? 1.0f : -1.0f) * Speed, acceleration);
        }
        if (IsOnWall())
        {
            SwitchToStunState();
        }
    }

    /// <summary>
    /// Applies gravity and horizontal movement while airborne after a jump, and returns the
    /// enemy to patrolling once it lands.
    /// </summary>
    /// <param name="incomingDelta">Elapsed time, in seconds, since the previous physics tick.</param>
    /// <param name="incomingVelocity">The enemy's velocity for this tick, modified in place.</param>
    private void Jumping(double incomingDelta, ref Godot.Vector2 incomingVelocity)
    {
        incomingVelocity.Y += gravity * (float)incomingDelta;
        incomingVelocity.X = Mathf.MoveToward(incomingVelocity.X, (direction ? 1.0f : -1.0f) * (Speed / 2), acceleration);
        if (IsOnFloor())
        {
            SwitchToPatrolState();
        }
    }

    /// <summary>
    /// Holds the enemy stationary until the break timer elapses, then resets the idle timer
    /// and returns to patrolling.
    /// </summary>
    /// <param name="incomingDelta">Elapsed time, in seconds, since the previous physics tick.</param>
    /// <param name="incomingVelocity">The enemy's velocity for this tick, set to zero for the duration of the break.</param>
    private void TakingBreak(double incomingDelta, ref Godot.Vector2 incomingVelocity)
    {
        incomingVelocity = Godot.Vector2.Zero;
        breakTimer -= incomingDelta;
        if (breakTimer <= 0.0d)
        {
            idleTimer = rnd.Next(10, 30);
            SwitchToPatrolState();
        }
    }

    /// <summary>
    /// Holds the enemy stationary and flickers its sprite for the duration of the stun, then
    /// returns to patrolling once the stun timer elapses.
    /// </summary>
    /// <param name="incomingDelta">Elapsed time, in seconds, since the previous physics tick.</param>
    /// <param name="incomingVelocity">The enemy's velocity for this tick, set to zero for the duration of the stun.</param>
    private void Stunned(double incomingDelta, ref Godot.Vector2 incomingVelocity)
    {
        stunTimer -= incomingDelta;
        flashTimer -= incomingDelta;
        if (flashTimer <= 0.0d)
        {
            flashTimer = stunTimer / 8;
            sprite_2d.Visible = !sprite_2d.Visible;
        }

        if (stunTimer <= 0.0d)
        {
            sprite_2d.Visible = true;
            SwitchToPatrolState();
        }
        incomingVelocity = Godot.Vector2.Zero;
    }

    /// <summary>
    /// Signal handler invoked when a body enters one of this enemy's detection areas. If the
    /// body is the player, resets the idle timer and begins charging if the player is in line of sight.
    /// </summary>
    /// <param name="body">The body that entered the detection area.</param>
    private void DetectPlayer(Node2D body)
    {
        GD.Print("DetectPlayer: ", body.Name.ToString());
        if (body.Name.ToString() == "Player")
        {
            Player = body;
            idleTimer = rnd.Next(20, 120);
            if (canCharge)
            {
                if (LineOfSightCheck(Player))
                {
                    SwitchToChargeState();
                }
            }
        }
    }

    /// <summary>
    /// Casts a ray from this enemy toward the given target to determine whether anything blocks
    /// the view between them.
    /// </summary>
    /// <param name="target">The node to check visibility toward.</param>
    /// <returns>True if the ray reaches <paramref name="target"/> without being blocked, false otherwise.</returns>
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

    /// <summary>
    /// Signal handler invoked when a body enters this enemy's <see cref="HurtBox"/>. If the body
    /// is the player, either kills or damages the player depending on <see cref="canInstantKill"/>.
    /// </summary>
    /// <param name="body">The body that entered the hurtbox.</param>
    private void PlayerEnteredHurtbox(Node2D body)
    {
        if (body.Name.ToString() == "Player")

            if (canInstantKill)
            {
                MessageManager.instance.KillPlayer();
            }
            else
            {
                MessageManager.instance.DamagePlayer(this.Position);
            }
    }

    /// <summary>
    /// Flips the enemy's facing direction, swaps which side's detection areas and shape casts
    /// are active, and resets the direction-change cooldown. Does nothing if the cooldown has
    /// not yet elapsed.
    /// </summary>
    private void FlipEntity()
    {
        if (changeDirectionTimerCurrent > 0.0d)
        {
            return;
        }
        // flip enemy sprite and change detection areas to monitor the other side.
        sprite_2d.FlipH = !sprite_2d.FlipH;
        direction = !direction;
        AreaDetectionRight.Monitoring = !AreaDetectionRight.Monitoring;
        AreaDetectionLeft.Monitoring = !AreaDetectionLeft.Monitoring;
        changeDirectionTimerCurrent = changeDirectionTimer;

        // swap navigation detection shapes.
        EdgeDetectionCastRight.Enabled = !EdgeDetectionCastRight.Enabled;
        WallDetectionCastRight.Enabled = !WallDetectionCastRight.Enabled;
        JumpDetectionCast1Right.Enabled = !JumpDetectionCast1Right.Enabled;
        JumpDetectionCast2Right.Enabled = !JumpDetectionCast2Right.Enabled;
        EdgeDetectionCastLeft.Enabled = !EdgeDetectionCastLeft.Enabled;
        WallDetectionCastLeft.Enabled = !WallDetectionCastLeft.Enabled;
        JumpDetectionCast1Left.Enabled = !JumpDetectionCast1Left.Enabled;
        JumpDetectionCast2Left.Enabled = !JumpDetectionCast2Left.Enabled;
    }

    /// <summary>Switches the enemy into <see cref="CurrentState.patroling"/>.</summary>
    private void SwitchToPatrolState()
    {
        CurrentState = CurrentState.patroling;
    }

    /// <summary>Switches the enemy into <see cref="CurrentState.charging"/>.</summary>
    private void SwitchToChargeState()
    {
        CurrentState = CurrentState.charging;
    }

    /// <summary>
    /// Attempts to switch the enemy into <see cref="CurrentState.jumping"/> if a jumpable gap is
    /// detected ahead, applying an upward impulse to <paramref name="incomingVelocity"/> on success.
    /// </summary>
    /// <param name="incomingDelta">Elapsed time, in seconds, since the previous physics tick. Currently unused.</param>
    /// <param name="incomingVelocity">The enemy's velocity for this tick, set to the jump impulse on success.</param>
    /// <returns>True if the enemy entered the jumping state, false otherwise.</returns>
    private bool SwitchToJumpState(double incomingDelta, ref Godot.Vector2 incomingVelocity)
    {
        if ((JumpDetectionCast1Right.IsColliding() && JumpDetectionCast1Right.Enabled == true)
        || (JumpDetectionCast1Left.IsColliding() && JumpDetectionCast1Left.Enabled == true))
        {
            //GD.Print("jump DetectionCast 1 Colliding");
            if ((!JumpDetectionCast2Right.IsColliding() && JumpDetectionCast2Right.Enabled == true)
            || (!JumpDetectionCast2Left.IsColliding() && JumpDetectionCast2Left.Enabled == true))
            {
                //GD.Print("Entering Jump State");
                CurrentState = CurrentState.jumping;
                incomingVelocity = new Godot.Vector2(0, -jumpForce);
                return true;
            }
            else
            {
                return false;
            }
        }
        return false;
    }

    /// <summary>Switches the enemy into <see cref="CurrentState.takingBreak"/>.</summary>
    private void SwitchToBreakState()
    {
        CurrentState = CurrentState.takingBreak;
    }

    /// <summary>
    /// Switches the enemy into <see cref="CurrentState.stunned"/>, resetting the stun and flash
    /// timers and hiding the sprite as the first step of the flicker effect.
    /// </summary>
    /// <example>
    /// <code>
    /// // Called from a bullet or hazard script when it strikes this enemy:
    /// enemy.SwitchToStunState();
    /// </code>
    /// </example>
    public void SwitchToStunState()
    {
        stunTimer = stunTime;
        flashTimer = stunTimer / 8;
        sprite_2d.Visible = false;
        CurrentState = CurrentState.stunned;
    }
}
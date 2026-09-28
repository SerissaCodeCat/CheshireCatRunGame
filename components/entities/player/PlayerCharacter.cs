using System;
using Godot;
/// <summary>
/// Top-level controller for the player avatar. Owns a small state machine and runs the
/// matching movement physics each frame, plus shooting, stealth visibility, and
/// save/load persistence.
/// </summary>
public partial class PlayerCharacter : CharacterBody2D
{
    /// <summary>
    /// Every discrete behaviour the player can be in. The current value selects which
    /// physics routine runs inside <see cref="_PhysicsProcess"/>.
    /// </summary>
    public enum PlayerStates
    {
        grounded,
        airborn,
        clinging,
        teleporting,
        crouching,
        damaged
    }
    /// <summary>The player's current state machine value. Other systems may read this to react to what the player is doing.</summary>
    public PlayerStates PlayerState = PlayerStates.grounded;
    // ---------- scene / resource references ----------
    /// <summary>Scene instantiated whenever the player fires. Loaded in <see cref="_Ready"/>.</summary>
    public PackedScene Bullet { get; set; }
    private readonly Godot.Color semiTransparent = new(1, 1, 1, 0.5f);
    private readonly Godot.Color solid = new(1, 1, 1, 1f);
    private Godot.Vector2 finalVelocity;
    private Godot.Vector2 direction;
    private AnimatedSprite2D sprite_2d;
    private Sprite2D aimingSprite;
    private Node2D aimingLynchpin;
    private Node2D aimingDirrection;
    private TrajectoryPreview trajectory; //Experimental replacement for aiming

    private bool aimingRise = true;
    private CollisionShape2D StandingCollision;
    private CollisionShape2D CrouchingCollision;
    private ShapeCast2D ShapeCastCeilingCheck;
    private ShapeCast2D ShapeCastWallCheck;
    private int StealthLayerCount = 0;
    // ---------- movement tuning ----------
    [Export] public float Speed = 450.0f;
    [Export(PropertyHint.Range, "0,5.0")] public float Acceleration = 1.0f;
    [Export(PropertyHint.Range, "0,20.0")] public float Deceleration = 15.0f;
    [Export(PropertyHint.Range, "0,10.0")] public float AirDeceleration = 3.3f;
    [Export(PropertyHint.Range, "0,-1000.0")] public float JumpVelocity = -600.0f;
    [Export(PropertyHint.Range, "0,10.0")] public float JumpAscentionTimer = 3.0f;
    [Export(PropertyHint.Range, "0,5.0")] public float JumpHangingTime = 1.0f;
    [Export(PropertyHint.Range, "0,5.0")] public float PlayerGravityMultiplier = 1.0f;
    private float JumpHangingTimeTimer = 0.0f;
    [Export] private float dashSpeed = 1000.0f;
    [Export(PropertyHint.Range, "50.0,150.0")] private float aimingRotationSpeed = 120.0f;
    [Export(PropertyHint.Range, "0,5.0")] private float DamageRecoveryReset = 2.0f;
    private const int HorizontalDamageReboundForce = 150;
    private const int VerticalDamageReboundForce = 300;
    /// <summary>Default world gravity, read once from the project settings.</summary>
    public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
    // ---------- timers (seconds) ----------
    [Export(PropertyHint.Range, "0.0,1.0")] private double CyoteTime = 0.05d;
    [Export(PropertyHint.Range, "0,1.0")] private double teleportTimerReset = 0.3d;
    [Export(PropertyHint.Range, "0,10.0")] private double clingTimerReset = 1.0d;
    [Export(PropertyHint.Range, "0,10.0")] private double BulletResetTime = 2.5d;
    [Export(PropertyHint.Range, "0,1.0")] private double FirstClingReset = 0.5d;
    [Export (PropertyHint.Range, "200.0f, 400.0f")] public float BulletSpeed = 200.0f;
    [Export (PropertyHint.Range, "0.4f, 1.0f")] public float BulletBounceDamping = 0.4f;

    private double damageTimer = 0.0d;
    private double flashTimer = 0.0d;
    private double cyoteTimer;
    private double teleportTimer;
    private double clingTimer;
    private double firstClingTimer;
    private double bulletTimer = 0.0d;
    // ---------- collision layers ----------
    private const int PlayerStealthDetectionLayer = 9; // bit value 256
    // ---------- ability / state flags ----------
    private bool doubleJumpAvailiable = true;
    private bool teleportAvailiable = true;
    private bool wallToRight = false;
    private bool damagable;
    private Vector2 spawnPosition = new(0, 0);
    private double previousPercentage;
    // ---------- save / load ----------
    /// <summary>Identifies this node type to the save system so it can be matched on load.</summary>
    public string SaveId => "player";
    // Increment SaveVersion whenever the Save() payload changes so a migration path can be added.
    private const int SaveVersion = 1;
    private static class SaveKeys
    {
        public const string Id = "saveId";
        public const string Version = "saveVersion";
        public const string ScenePath = "scenePath";
        public const string NodeName = "nodeName";      // debugging only, never used for lookup
        public const string PosX = "posX";
        public const string PosY = "posY";
        public const string VelX = "velX";
        public const string VelY = "velY";
        public const string State = "state";
        public const string FacingLeft = "facingLeft";
        public const string DoubleJump = "doubleJumpAvailable";
        public const string Teleport = "teleportAvailable";
        public const string SpawnX = "spawnX";
        public const string SpawnY = "spawnY";
        public const string BulletTimer = "bulletTimer";
    }
    private Godot.Collections.Dictionary<string, Variant> pendingLoadData;
    /// <summary>
    /// Godot lifecycle hook. Caches child node references, loads the bullet scene,
    /// registers with the message and save systems, and applies any save data that
    /// arrived before this node entered the scene tree.
    /// </summary>
    /// <example>Invoked automatically by the engine when the node enters the tree; never call it directly.</example>
    public override void _Ready()
    {
        base._Ready();
        sprite_2d = GetNode<AnimatedSprite2D>("Sprite2D");
        StandingCollision = GetNode<CollisionShape2D>("CollisionShapeStanding");
        CrouchingCollision = GetNode<CollisionShape2D>("CollisionShapeCrouching");
        ShapeCastCeilingCheck = GetNode<ShapeCast2D>("ShapeCast2DCeilingCheck");
        ShapeCastWallCheck = GetNode<ShapeCast2D>("ShapeCast2DWallCheck");
        Bullet = GD.Load<PackedScene>("res://components/entities/player/Bullet.tscn");
        aimingLynchpin = GetNode<Node2D>("aimingLynchpin");
        aimingDirrection = GetNode<Node2D>("aimingLynchpin/aimingDirection");
        aimingSprite = GetNode<Sprite2D>("aimingLynchpin/aimingSprite");
        trajectory = GetNode<TrajectoryPreview>("TrajectoryPreview");
        trajectory.Visible = false;
        aimingSprite.Visible = false;
        damagable = true;
        cyoteTimer = CyoteTime;
        teleportTimer = teleportTimerReset;
        clingTimer = clingTimerReset;
        MessageManager.instance.AddPlayerToMessageManager(this);
        AddToGroup("PersistThroughSave");   // lets SaveManager find saveables without knowing names
        if (pendingLoadData != null)
        {
            ApplyLoad(pendingLoadData);
            pendingLoadData = null;
        }
    }
    /// <summary>
    /// Collects everything needed to restore this player later (transform, velocity,
    /// abilities, spawn point, and bullet cooldown) into a serialisable dictionary.
    /// </summary>
    /// <returns>A dictionary keyed by <see cref="SaveKeys"/> values describing the player's current persistent state.</returns>
    /// <example>saveFile["player"] = player.Save();</example>
    public Godot.Collections.Dictionary<string, Variant> Save()
    {
        return new Godot.Collections.Dictionary<string, Variant>
        {
            { SaveKeys.Id, SaveId },
            { SaveKeys.Version, SaveVersion },
            { SaveKeys.ScenePath, SceneFilePath },
            { SaveKeys.NodeName, Name.ToString() },
            { SaveKeys.PosX, GlobalPosition.X },
            { SaveKeys.PosY, GlobalPosition.Y },
            { SaveKeys.VelX, Velocity.X },
            { SaveKeys.VelY, Velocity.Y },
            { SaveKeys.State, PlayerState.ToString() },
            { SaveKeys.FacingLeft, sprite_2d.FlipH },
            { SaveKeys.DoubleJump, doubleJumpAvailiable },
            { SaveKeys.Teleport, teleportAvailiable },
            { SaveKeys.SpawnX, spawnPosition.X },
            { SaveKeys.SpawnY, spawnPosition.Y },
            { SaveKeys.BulletTimer, bulletTimer },
        };
    }
    /// <summary>
    /// Validates a saved data dictionary and applies it to this player. If the node's
    /// children do not yet exist the data is cached and applied from <see cref="_Ready"/>.
    /// </summary>
    /// <param name="data">A dictionary previously produced by <see cref="Save"/>. May be null or hold data for a different object.</param>
    /// <returns>True if the data was for a player and has been applied or queued; false if it was missing, foreign, or from a newer save version.</returns>
    /// <example>if (!player.Load(section)) GD.PushWarning("could not restore player");</example>
    public bool Load(Godot.Collections.Dictionary<string, Variant> data)
    {
        if (data == null
            || !data.TryGetValue(SaveKeys.Id, out Variant id)
            || id.VariantType != Variant.Type.String
            || id.AsString() != SaveId)
        {
            GD.PushWarning("PlayerCharacter.Load: data is missing or not player data.");
            return false;
        }
        int version = (int)ReadDouble(data, SaveKeys.Version, 0);
        if (version > SaveVersion)
        {
            GD.PushWarning($"PlayerCharacter.Load: save version {version} is newer than supported {SaveVersion}.");
            return false;
        }
        // if (version < SaveVersion) { migrate here }
        // Child node references don't exist before _Ready. If the SaveManager calls
        // us before AddChild, defer applying the data until _Ready runs.
        if (!IsNodeReady())
        {
            pendingLoadData = data;
            return true;
        }
        ApplyLoad(data);
        return true;
    }
    /// <summary>Writes a validated save dictionary onto this player's live state.</summary>
    private void ApplyLoad(Godot.Collections.Dictionary<string, Variant> data)
    {
        // --- transform ---
        GlobalPosition = new Vector2(
            ReadFloat(data, SaveKeys.PosX, GlobalPosition.X),
            ReadFloat(data, SaveKeys.PosY, GlobalPosition.Y));
        Velocity = new Vector2(
            ReadFloat(data, SaveKeys.VelX, 0f),
            ReadFloat(data, SaveKeys.VelY, 0f));
        spawnPosition = new Vector2(
            ReadFloat(data, SaveKeys.SpawnX, spawnPosition.X),
            ReadFloat(data, SaveKeys.SpawnY, spawnPosition.Y));
        // --- facing / aiming ---
        sprite_2d.FlipH = ReadBool(data, SaveKeys.FacingLeft, false);
        aimingLynchpin.RotationDegrees = sprite_2d.FlipH ? 180.0f : 0.0f;
        aimingSprite.Visible = false;
        aimingRise = true;
        // --- abilities / persisted timers ---
        doubleJumpAvailiable = ReadBool(data, SaveKeys.DoubleJump, true);
        teleportAvailiable = ReadBool(data, SaveKeys.Teleport, true);
        bulletTimer = Mathf.Clamp(ReadDouble(data, SaveKeys.BulletTimer, 0.0d), 0.0d, BulletResetTime);
        previousPercentage = -1.0d;          // forces the UI to be re-sent next physics frame
        // --- transient state is always reset, never restored ---
        damagable = true;
        damageTimer = 0.0d;
        flashTimer = 0.0d;
        cyoteTimer = CyoteTime;
        clingTimer = clingTimerReset;
        teleportTimer = teleportTimerReset;
        firstClingTimer = 0.0d;
        JumpHangingTimeTimer = 0.0f;         // don't hover for a second after loading
        // Only crouching is restored literally. Standing the player up under a low
        // ceiling would embed them in it, so every other saved state becomes airborn
        // and the state machine settles within a frame or two.
        if (ReadSavedPlayerState(data) == PlayerStates.crouching)
        {
            EnterCrouchingState();
        }
        else
        {
            CrouchingCollision.Disabled = true;
            StandingCollision.Disabled = false;
            PlayerState = PlayerStates.airborn;
            RefreshStealth();
        }
        ResetPhysicsInterpolation();         // harmless if interpolation is off; prevents a visible "slide" if on
    }
    /// <summary>Reads the saved player-state name, falling back to airborn for unknown or malformed values.</summary>
    private static PlayerStates ReadSavedPlayerState(Godot.Collections.Dictionary<string, Variant> data)
    {
        if (data.TryGetValue(SaveKeys.State, out Variant s)
            && s.VariantType == Variant.Type.String
            && Enum.TryParse(s.AsString(), out PlayerStates parsed)
            && Enum.IsDefined(typeof(PlayerStates), parsed))
        {
            return parsed;
        }
        return PlayerStates.airborn;
    }
    /// <summary>
    /// Godot lifecycle hook. Logs that this player instance has been removed from the scene tree.
    /// </summary>
    /// <example>Invoked automatically by the engine when the node leaves the tree; never call it directly.</example>
    public override void _ExitTree()
    {
        base._ExitTree();
        GD.Print("player with ID of " + GetInstanceId() + " has been removed");
    }
    /// <summary>
    /// Godot physics tick. Advances damage-recovery flashing, runs the physics routine
    /// for the current <see cref="PlayerState"/>, ticks the bullet cooldown, reports the
    /// energy percentage to the UI, and finally moves the body.
    /// </summary>
    /// <param name="delta">Seconds elapsed since the previous physics frame, supplied by the engine.</param>
    /// <example>Invoked automatically by the engine every physics frame; never call it directly.</example>
    public override void _PhysicsProcess(double delta)
    {
        finalVelocity = Velocity;
        // Damage recovery spans two states, so it runs before the state switch.
        UpdateDamageRecovery(delta);
        switch (PlayerState)
        {
            case PlayerStates.grounded:    DoGroundedPhysics(ref finalVelocity, delta);  break;
            case PlayerStates.airborn:     DoAirbornPhysics(ref finalVelocity, delta);   break;
            case PlayerStates.clinging:    DoClingingPhysics(ref finalVelocity, delta);  break;
            case PlayerStates.teleporting: DoTeleportingPhysics(delta);                  break;
            case PlayerStates.crouching:   DoCrouchingPhysics(ref finalVelocity, delta); break;
            case PlayerStates.damaged:     DoDamagedPhysics(ref finalVelocity, delta);   break;
            default:                       PlayerState = PlayerStates.grounded;          break;
        }
        Velocity = finalVelocity;
        UpdateBulletCooldown(delta);
        MoveAndSlide();
    }
    /// <summary>
    /// While the player is recovering from a hit, counts the invulnerability timer down
    /// and flashes the sprite. When it expires, control is returned and the player is put
    /// back into the grounded or airborn state.
    /// </summary>
    private void UpdateDamageRecovery(double delta)
    {
        if (damagable)
            return;
        if (damageTimer <= 0.0d)
        {
            damagable = true;
            sprite_2d.Modulate = solid;
            flashTimer = 0.0d;
            PlayerState = IsOnFloor() ? PlayerStates.grounded : PlayerStates.airborn;
            return;
        }
        damageTimer -= delta;
        flashTimer -= delta;
        if (flashTimer <= 0.0d)
        {
            flashTimer = damageTimer / 8;
            FlashPlayer();
        }
    }
    /// <summary>Drains the bullet cooldown and pushes the energy percentage to the UI when it changes.</summary>
    private void UpdateBulletCooldown(double delta)
    {
        bulletTimer = bulletTimer > 0.0d ? bulletTimer - delta : 0.0d;
        double percentage = GetbulletTimePercentageDecimal();
        if (percentage != previousPercentage)
        {
            previousPercentage = percentage;
            MessageManager.instance.SendEnegyPercentageTotalToUI(percentage);
        }
    }
    /// <summary>Runs one physics frame of grounded movement: walking, turning, jumping, teleporting, shooting, and crouching.</summary>
    private void DoGroundedPhysics(ref Godot.Vector2 incomingVelocity, double incomingDelta)
    {
        // Refresh coyote time while on the floor, otherwise drain it. When it runs out
        // the player has walked off a ledge, so restore air abilities and fall.
        cyoteTimer = IsOnFloor() ? CyoteTime : cyoteTimer - incomingDelta;
        if (cyoteTimer <= 0.0d)
        {
            RefreshAirAbilities();
            PlayerState = PlayerStates.airborn;
            return;
        }
        direction = Input.GetVector("left", "right", "up", "down");
        if (direction != Godot.Vector2.Zero)
        {
            incomingVelocity.X = Mathf.MoveToward(Velocity.X, direction.X * Speed, Acceleration);
            FaceHorizontalInput(direction.X);
        }
        else
        {
            incomingVelocity.X = Mathf.MoveToward(Velocity.X, 0, Deceleration);
        }
        if (Input.IsActionJustPressed("jump"))
        {
            EnterAirbornState();
            RefreshAirAbilities();
            if (!Input.IsActionPressed("down"))
            {
                incomingVelocity.Y = JumpVelocity;
                PlayerState = PlayerStates.airborn;
            }
            else
            {
                // Nudge down a few pixels to drop through a one-way platform.
                Position = new Godot.Vector2(Position.X, Position.Y + 10);
            }
            cyoteTimer = 0.0d;
            return;
        }
        if (Input.IsActionJustPressed("teleport"))
        {
            teleportAvailiable = false;
            doubleJumpAvailiable = true;
            clingTimer = clingTimerReset;
            PlayerState = PlayerStates.teleporting;
            teleportTimer = teleportTimerReset;
            return;
        }
        if (Input.IsActionPressed("fire"))
        {
            if (bulletTimer <= 0.0d)
            {
                direction = Godot.Vector2.Zero;
                //aimingSprite.Visible = true;      // optional: remove if the dots replace the arrow
                UpdateAimSweep(incomingDelta);
                UpdateTrajectory();               // new
            }
        }
        else if (Input.IsActionJustReleased("fire"))
        {
            FireBullet();
        }
        else if (Input.IsActionJustPressed("crouch"))
        {
            EnterCrouchingState();
        }
        sprite_2d.Animation = incomingVelocity.X != 0.0f ? "running" : "default";
    }
    /// <summary>Runs one physics frame of crouching movement: reduced-speed walking, standing up, jumping, and teleporting.</summary>
    private void DoCrouchingPhysics(ref Godot.Vector2 incomingVelocity, double incomingDelta)
    {
        incomingVelocity.Y += gravity * PlayerGravityMultiplier * (float)incomingDelta;
        // Refresh coyote time while on the floor, otherwise drain it and fall once empty.
        cyoteTimer = IsOnFloor() ? CyoteTime : cyoteTimer - incomingDelta;
        if (cyoteTimer <= 0.0d)
        {
            RefreshAirAbilities();
            RestoreStandingCollision();
            PlayerState = PlayerStates.airborn;
            return;
        }
        if (Input.IsActionJustPressed("jump"))
        {
            EnterAirbornState();
            RefreshAirAbilities();
            RestoreStandingCollision();
            PlayerState = PlayerStates.airborn;
            incomingVelocity.Y = JumpVelocity * 1.5f;
            cyoteTimer = 0.0d;
            return;
        }
        if (Input.IsActionJustPressed("teleport"))
        {
            teleportAvailiable = false;
            doubleJumpAvailiable = true;
            clingTimer = clingTimerReset;
            RestoreStandingCollision();
            PlayerState = PlayerStates.teleporting;
            teleportTimer = teleportTimerReset;
            return;
        }
        direction = Input.GetVector("left", "right", "up", "down");
        if (direction != Godot.Vector2.Zero)
        {
            incomingVelocity.X = Mathf.MoveToward(Velocity.X, direction.X * (Speed / 2.0f), Deceleration / 3.0f);
            sprite_2d.FlipH = direction.X < 0;
        }
        else
        {
            incomingVelocity.X = Mathf.MoveToward(Velocity.X, 0, Deceleration);
        }
        // Only stand back up when the crouch input is released and there is headroom.
        if (!Input.IsActionPressed("crouch") && !ShapeCastCeilingCheck.IsColliding())
        {
            EnterGroundedState();
        }
        sprite_2d.Animation = "crouching";
    }
    /// <summary>
    /// Runs one physics frame of airborne movement: gravity, apex hang, variable jump
    /// height, double jump, teleport, and wall-cling detection.
    /// </summary>
    private void DoAirbornPhysics(ref Godot.Vector2 incomingVelocity, double incomingDelta)
    {
        direction = Input.GetVector("left", "right", "up", "down");
        firstClingTimer -= incomingDelta;
        if (IsOnFloor())
        {
            EnterGroundedState();
            return;
        }
        // Grab onto a wall, unless the player is holding "down" to slide past it.
        if (IsOnWall() && firstClingTimer <= 0.0d && !Input.IsActionPressed("down"))
        {
            EnterClingingState();
            return;
        }
        // While the apex-hang timer is running and the player has started falling,
        // hold them in place; otherwise apply gravity as normal.
        if (JumpHangingTimeTimer > 0.0f && incomingVelocity.Y > 0.0f)
        {
            JumpHangingTimeTimer -= (float)incomingDelta;
            incomingVelocity.Y = 0.0f;
        }
        else
        {
            incomingVelocity.Y += gravity * PlayerGravityMultiplier * (float)incomingDelta;
        }
        // Releasing jump while still rising cuts the jump short for variable height.
        if (Input.IsActionJustReleased("jump") && incomingVelocity.Y < 0.0f)
        {
            incomingVelocity.Y = 0.0f;
        }
        if (Input.IsActionJustPressed("teleport") && teleportAvailiable)
        {
            PlayerState = PlayerStates.teleporting;
            teleportAvailiable = false;
            teleportTimer = teleportTimerReset;
            return;
        }
        if (Input.IsActionJustPressed("jump") && doubleJumpAvailiable)
        {
            incomingVelocity.Y = JumpVelocity * 0.9f;
            doubleJumpAvailiable = false;
        }
        if (direction != Godot.Vector2.Zero)
        {
            incomingVelocity.X = Mathf.MoveToward(Velocity.X, direction.X * Speed, Deceleration);
            sprite_2d.FlipH = direction.X < 0;
        }
        else
        {
            incomingVelocity.X = Mathf.MoveToward(Velocity.X, 0, AirDeceleration);
        }
        sprite_2d.Animation = doubleJumpAvailiable ? "jumping" : "doubleJump";
    }
    /// <summary>Runs one physics frame of wall clinging: a slow slide after the grace period and a directional wall jump.</summary>
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
        // Hold still for the grace period, then slide at a third of normal gravity.
        if (clingTimer <= 0)
            incomingVelocity.Y += gravity / 3 * (float)incomingDelta;
        else
            clingTimer -= incomingDelta;
        if (!Input.IsActionJustPressed("jump"))
            return;
        EnterAirbornState();
        direction = Input.GetVector("left", "right", "up", "down");
        if (Input.IsActionPressed("left"))
        {
            incomingVelocity.Y = JumpVelocity;
            incomingVelocity.X = wallToRight ? -Speed : Speed / 2;
        }
        else if (Input.IsActionPressed("right"))
        {
            incomingVelocity.Y = JumpVelocity;
            incomingVelocity.X = wallToRight ? -(Speed / 2) : Speed;
        }
        // Holding "down" (or no horizontal input) just drops off the wall.
    }
    /// <summary>Runs one physics frame of the teleport dash: a fixed horizontal burst that ignores gravity until the timer expires.</summary>
    private void DoTeleportingPhysics(double incomingDelta)
    {
        // Gravity does not apply while teleporting.
        finalVelocity = new Godot.Vector2(sprite_2d.FlipH ? -dashSpeed : dashSpeed, 0.0f);
        teleportTimer -= incomingDelta;
        if (teleportTimer > 0.0d)
            return;
        finalVelocity = Godot.Vector2.Zero;
        if (IsOnFloor())
            EnterGroundedState();
        else if (IsOnWall())
            EnterClingingState();
        else
            EnterAirbornState();
    }
    /// <summary>Runs one physics frame of the damaged state: only gravity is applied; the player has no control until recovery finishes.</summary>
    private void DoDamagedPhysics(ref Godot.Vector2 incomingVelocity, double incomingDelta)
    {
        incomingVelocity.Y += gravity * (float)incomingDelta;
    }
    /// <summary>Resets timers and abilities for standing on the ground and enters the grounded state.</summary>
    private void EnterGroundedState()
    {
        cyoteTimer = CyoteTime;
        clingTimer = clingTimerReset;
        firstClingTimer = FirstClingReset;
        doubleJumpAvailiable = true;
        teleportAvailiable = true;
        RestoreStandingCollision();
        PlayerState = PlayerStates.grounded;
        RefreshStealth();
    }
    /// <summary>Swaps to the crouching collision shape and enters the crouching state.</summary>
    private void EnterCrouchingState()
    {
        CrouchingCollision.Disabled = false;
        StandingCollision.Disabled = true;
        PlayerState = PlayerStates.crouching;
        RefreshStealth();
    }
    /// <summary>Works out which side the wall is on, stops the player, and enters the clinging state.</summary>
    private void EnterClingingState()
    {
        DetermineDirrectionOfWall();
        finalVelocity = Godot.Vector2.Zero;
        PlayerState = PlayerStates.clinging;
        RefreshStealth();
    }
    /// <summary>Starts the apex-hang timer and enters the airborn state.</summary>
    private void EnterAirbornState()
    {
        JumpHangingTimeTimer = JumpHangingTime;
        PlayerState = PlayerStates.airborn;
        RefreshStealth();
    }
    /// <summary>Restores the abilities that become available again once the player is in the air.</summary>
    private void RefreshAirAbilities()
    {
        doubleJumpAvailiable = true;
        teleportAvailiable = true;
        clingTimer = clingTimerReset;
    }
    /// <summary>Swaps the crouching collision shape back out for the standing one.</summary>
    private void RestoreStandingCollision()
    {
        StandingCollision.Disabled = false;
        CrouchingCollision.Disabled = true;
    }
    /// <summary>
    /// Recomputes whether the player should currently be hidden (crouched inside a stealth
    /// area), updates the sprite transparency, and toggles the stealth-detection layer.
    /// </summary>
    private void RefreshStealth()
    {
        bool shouldBeHidden = PlayerState == PlayerStates.crouching && StealthLayerCount > 0;
        sprite_2d.Modulate = shouldBeHidden ? semiTransparent : solid;
        SetCollisionLayerValue(PlayerStealthDetectionLayer, !shouldBeHidden);
    }
    /// <summary>
    /// Records that the player has entered another overlapping stealth area and refreshes
    /// whether they should currently be hidden.
    /// </summary>
    /// <example>Call from a stealth zone's body_entered handler: player.IncreaseStealthLayerCount();</example>
    public void IncreaseStealthLayerCount()
    {
        StealthLayerCount++;
        RefreshStealth();
    }
    /// <summary>
    /// Records that the player has left a stealth area (never dropping below zero) and
    /// refreshes whether they should currently be hidden.
    /// </summary>
    /// <example>Call from a stealth zone's body_exited handler: player.DecreaseStealthLayerCount();</example>
    public void DecreaseStealthLayerCount()
    {
        StealthLayerCount--;
        if (StealthLayerCount < 0)
            StealthLayerCount = 0;
        RefreshStealth();
    }
    /// <summary>
    /// Applies a hit to the player: begins the invulnerability and flash window, knocks the
    /// player away from the damage source, and asks the UI to reduce the health bar. Does
    /// nothing while the player is already invulnerable.
    /// </summary>
    /// <param name="DamageOriginX">World-space X the damage came from; selects the horizontal knock-back direction.</param>
    /// <param name="DamageOriginY">World-space Y the damage came from; selects the vertical knock-back direction (ignored while on the floor).</param>
    /// <param name="damage">How many points to subtract from the UI health bar. Defaults to 1.</param>
    /// <returns>Nothing. The player's velocity, invulnerability state, and the UI health bar are updated as side effects.</returns>
    /// <example>spikeTrap calls player.DamagePLayer(GlobalPosition.X, GlobalPosition.Y, 2);</example>
    public void DamagePLayer(float DamageOriginX = 0.0f, float DamageOriginY = 0.0f, int damage = 1)
    {
        if (!damagable)
            return;
        damagable = false;
        damageTimer = DamageRecoveryReset;
        PlayerState = PlayerStates.damaged;
        // Recoil to the opposite quadrant from the damage source; X +1 is right, Y -1 is up.
        int reboundX = GlobalPosition.X > DamageOriginX ? 1 : -1;
        int reboundY = GlobalPosition.Y > DamageOriginY ? 1 : -1;
        if (IsOnFloor())
            reboundY = -1;   // always bounce upward when standing on the ground
        Velocity = new Godot.Vector2(reboundX * HorizontalDamageReboundForce, reboundY * VerticalDamageReboundForce);
        MessageManager.instance.DecreaseUICurrentHealthBy(damage);
    }
    /// <summary>Toggles the sprite between fully opaque and semi-transparent for the damage-flash effect.</summary>
    private void FlashPlayer()
    {
        // Modulate.A is the alpha channel of the RGBA colour.
        sprite_2d.Modulate = sprite_2d.Modulate.A == semiTransparent.A ? solid : semiTransparent;
    }
    /// <summary>
    /// Inspects this frame's slide collisions to find a tilemap wall, records which side
    /// it is on, and aims the wall-check shape cast at that side.
    /// </summary>
    private void DetermineDirrectionOfWall()
    {
        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            KinematicCollision2D collision = GetSlideCollision(i);
            if (collision.GetCollider() is not TileMapLayer)
                continue;
            wallToRight = collision.GetNormal().X <= 0;
            // Point the wall-detection cast at whichever side the wall is on.
            ShapeCastWallCheck.TargetPosition = new Vector2(wallToRight ? 15.0f : -15.0f, 0.0f);
        }
    }
    /// <summary>
    /// Spawns a bullet at the aim reticle if the bullet cooldown has elapsed, restarts the
    /// cooldown, and hides the aiming sprite.
    /// </summary>
    private void FireBullet()
    {
        if (bulletTimer > 0.0d)
            return;
        Bullet shot = Bullet.Instantiate<Bullet>();
        Godot.Vector2 spawnPoint = aimingLynchpin.GlobalPosition.Lerp(aimingDirrection.GlobalPosition, 0.5f);
        Godot.Vector2 aimVector = aimingDirrection.GlobalPosition - aimingLynchpin.GlobalPosition;
        shot.Setup(spawnPoint, aimVector, BulletSpeed, BulletBounceDamping);
        Owner.AddChild(shot);
        bulletTimer = BulletResetTime;
        aimingLynchpin.RotationDegrees = sprite_2d.FlipH ? 180.0f : 0.0f;
        aimingSprite.Visible = false;
        trajectory.Visible = false;
    }
    /// <summary>
    /// Returns the player to the stored spawn position, clears damage state and velocity,
    /// and forces the grounded state.
    /// </summary>
    /// <example>Call when the player falls out of the level: player.ResetPlayerToSpawnPosition();</example>
    public void ResetPlayerToSpawnPosition()
    {
        GlobalPosition = spawnPosition;
        damageTimer = 0.0d; // not damaged
        PlayerState = PlayerStates.grounded;
        Velocity = Godot.Vector2.Zero;
        RefreshStealth();
    }
    /// <summary>Stores the world position the player will return to when <see cref="ResetPlayerToSpawnPosition"/> is called.</summary>
    /// <param name="incomingPosition">The world-space position to use as the new spawn point.</param>
    /// <example>Call from a checkpoint: player.SetSpawnPosition(checkpointMarker.GlobalPosition);</example>
    public void SetSpawnPosition(Vector2 incomingPosition)
    {
        spawnPosition = incomingPosition;
    }
    /// <summary>Reports how much of the bullet cooldown is still remaining, as a fraction.</summary>
    /// <returns>A value from 0.0 (ready to fire) up to 1.0 (just fired). Never negative.</returns>
    /// <example>energyBar.Value = player.GetbulletTimePercentageDecimal() * 100.0;</example>
    public double GetbulletTimePercentageDecimal()
    {
        return bulletTimer > 0.0d ? bulletTimer / BulletResetTime : 0.0d;
    }
    /// <summary>
    /// Flips the sprite to match horizontal input and, unless the player is firing, snaps
    /// the aim reticle to point the same way.
    /// </summary>
    /// <param name="directionX">The horizontal component of the movement input; negative faces left, positive faces right.</param>
    private void FaceHorizontalInput(float directionX)
    {
        if (Input.IsActionPressed("fire"))
            return;
        sprite_2d.FlipH = directionX < 0;
        if (directionX < 0 && aimingLynchpin.RotationDegrees != 180.0f)
            aimingLynchpin.Rotate((float)Mathf.DegToRad(180.0d));
        else if (directionX > 0 && aimingLynchpin.RotationDegrees != 0.0f)
            aimingLynchpin.Rotate((float)Mathf.DegToRad(-180.0d));
    }
    /// <summary>
    /// Advances the aiming reticle's up-and-down sweep for this frame. The arc depends on
    /// whether the player faces left or right; <c>aimingRise</c> tracks the sweep direction.
    /// </summary>
    /// <param name="delta">Seconds elapsed this frame; scales how far the reticle rotates.</param>
    private void UpdateAimSweep(double delta)
    {
        float step = (float)Mathf.DegToRad(aimingRotationSpeed * delta);
        if (!sprite_2d.FlipH)
            SweepAimFacingRight(step);
        else
            SweepAimFacingLeft(step);
    }
    /// <summary>Moves the reticle through its arc while the player faces right (0 degrees down to -80 degrees up).</summary>
    private void SweepAimFacingRight(float step)
    {
        if (aimingRise)
        {
            if (aimingLynchpin.RotationDegrees >= -80.0f)
            {
                aimingLynchpin.Rotate(-step);
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
                aimingLynchpin.Rotate(step);
            }
            else
            {
                aimingLynchpin.RotationDegrees = 0.0f;
                aimingRise = !aimingRise;
            }
        }
    }
    /// <summary>Moves the reticle through its arc while the player faces left (180 degrees down to 260 degrees up).</summary>
    private void SweepAimFacingLeft(float step)
    {
        if (aimingRise)
        {
            if (aimingLynchpin.RotationDegrees <= 260.0f)
            {
                aimingLynchpin.Rotate(step);
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
                aimingLynchpin.Rotate(-step);
            }
            else
            {
                aimingLynchpin.RotationDegrees = 180.0f;
                aimingRise = !aimingRise;
            }
        }
    }
    /// <summary>Computes the launch velocity the bullet will use and redraws the predicted arc.</summary>
    private void UpdateTrajectory()
    {
        Vector2 spawn = aimingLynchpin.GlobalPosition.Lerp(aimingDirrection.GlobalPosition, 0.5f);
        Vector2 aimDir = (aimingDirrection.GlobalPosition - aimingLynchpin.GlobalPosition).Normalized();
        Vector2 launchVelocity = aimDir * BulletSpeed;
        trajectory.Visible = true;
        trajectory.UpdateArc(spawn, launchVelocity, gravity);
    }

    // ---------- save-data readers ----------
    // JSON returns every number as a double and a hand-edited or corrupt file can contain
    // anything, so never index the dictionary directly - always go through these.
    /// <summary>Safely reads a numeric value from a save dictionary, returning a fallback when it is absent or not finite.</summary>
    private static double ReadDouble(Godot.Collections.Dictionary<string, Variant> d, string key, double fallback)
    {
        if (d.TryGetValue(key, out Variant v)
            && (v.VariantType == Variant.Type.Float || v.VariantType == Variant.Type.Int))
        {
            double value = v.AsDouble();
            if (double.IsFinite(value))
                return value;
        }
        return fallback;
    }
    /// <summary>Safely reads a numeric value from a save dictionary as a float.</summary>
    private static float ReadFloat(Godot.Collections.Dictionary<string, Variant> d, string key, float fallback)
        => (float)ReadDouble(d, key, fallback);
    /// <summary>Safely reads a boolean value from a save dictionary, returning a fallback when it is absent or the wrong type.</summary>
    private static bool ReadBool(Godot.Collections.Dictionary<string, Variant> d, string key, bool fallback)
        => d.TryGetValue(key, out Variant v) && v.VariantType == Variant.Type.Bool ? v.AsBool() : fallback;
}
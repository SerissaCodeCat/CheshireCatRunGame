using Godot;

public partial class PixelPerfectCamera : Camera2D
{
    private Node2D Target = null;
    private Vector2 actualCameraPosition;
    private Vector2 cameraOffsetPossition;
    private Vector2 truePosition;
    
    /// <summary>
    /// Follow responsiveness. Higher is snappier; 0 or less locks rigidly to the target.
    /// Replaces Camera2D's built-in position smoothing, which must stay disabled.
    /// </summary>
    [Export]
    public float FollowSpeed { get; set; } = 10f;

    public override void _Ready()
    {
        MessageManager.instance.AddCameraToMessager(this);
        PositionSmoothingEnabled = false;
        DragHorizontalEnabled = false;
        DragVerticalEnabled = false;
        ProcessCallback = Camera2DProcessCallback.Idle;
        TopLevel = true;
        ProcessPriority = 100; // run after most other nodes
        truePosition = GlobalPosition;

    }

    /// <summary>
    /// Sets the node the camera follows.
    /// </summary>
    /// <param name="newTarget">The node to follow.</param>
    /// <param name="snapImmediately">If true, jump straight to the target instead of easing.</param>
    /// <example>
    /// <code>
    /// camera.SetTarget(player); // e.g. from MessageManager.SetPlayerAsCameraTarget()
    /// </code>
    /// </example>
    public void SetCameraTarget(Node2D newTarget, bool snapImmediately = true)
    {
        Target = newTarget;
        if (snapImmediately && Target != null)
        {
            truePosition = Target.GlobalPosition;
        }
    }
    public void MoveCameraToPosition(Vector2 incomingPosition)
    {
        var TmpPosition = incomingPosition;
        this.GlobalPosition = TmpPosition.Round();
    }
    /// <summary>
    /// Godot lifecycle callback. Moves the float-precision tracked position toward the target,
    /// places the camera on the nearest whole pixel, and sends the remainder to the viewport.
    /// </summary>
    /// <param name="delta">Elapsed time, in seconds, since the previous frame.</param>
    /// <example>
    /// Called automatically by the Godot engine every rendered frame.
    /// It is not intended to be called directly from other scripts.
    /// </example>
    public override void _Process(double delta)
    {
        if (Target != null && IsInstanceValid(Target))
        {
            if (FollowSpeed <= 0f)
            {
                truePosition = Target.GlobalPosition;
            }
            else
            {
                // Frame-rate independent exponential smoothing.
                float weight = 1f - Mathf.Exp(-FollowSpeed * (float)delta);
                truePosition = truePosition.Lerp(Target.GlobalPosition, weight);
            }
        }
        Vector2 snapped = truePosition.Round();
        GlobalPosition = snapped;
        ForceUpdateScroll();
        SubViewportContainer.Instance?.ViewportWholePixelOnlyMovement(truePosition - snapped);
    }

}
/*

using Godot;
/// <summary>
/// Camera that leads the target in the direction of travel. It tracks the target at float
/// precision, offsets the view ahead of its velocity, places itself on whole game pixels,
/// and forwards the sub-pixel remainder to the viewport shader for smooth scrolling.
/// </summary>
public partial class PixelPerfectCamera : Camera2D
{
    /// <summary>
    /// The node the camera follows and leads. Velocity is read from it when it is a
    /// <see cref="CharacterBody2D"/> or <see cref="RigidBody2D"/>.
    /// </summary>
    [Export]
    public Node2D Target { get; set; }
    /// <summary>
    /// How quickly the camera catches up with the target's position. Higher is snappier;
    /// 0 or less locks rigidly to the (look-ahead adjusted) target.
    /// </summary>
    [Export]
    public float FollowSpeed { get; set; } = 10f;
    /// <summary>
    /// Seconds of target velocity to project ahead. At 0.2 s, a target moving at 300 px/s
    /// produces a raw look-ahead of 60 px before clamping.
    /// </summary>
    [Export]
    public float LookAheadTime { get; set; } = 0.2f;
    /// <summary>
    /// Maximum look-ahead distance in game pixels, per axis (X = horizontal, Y = vertical).
    /// Prevents the view from running too far ahead at high speed.
    /// </summary>
    [Export]
    public Vector2 MaxLookAhead { get; set; } = new Vector2(48f, 24f);
    /// <summary>
    /// Per-axis multiplier applied to the look-ahead. Use a Y value below 1 so that
    /// vertical movement (falling, jumping) shifts the view less than horizontal movement.
    /// Set Y to 0 to disable vertical look-ahead entirely.
    /// </summary>
    [Export]
    public Vector2 LookAheadScale { get; set; } = new Vector2(1f, 0.6f);
    /// <summary>
    /// How quickly the look-ahead offset eases toward its desired value. Keeps the view
    /// from lurching when the player reverses direction. 0 or less disables easing.
    /// </summary>
    [Export]
    public float LookAheadSpeed { get; set; } = 4f;
    private Vector2 truePosition;
    private Vector2 currentLookAhead = Vector2.Zero;
    private Vector2 previousTargetPosition;
    private bool hasPreviousTarget = false;
    /// <summary>
    /// Godot lifecycle callback. Disables built-in camera smoothing and drag, detaches the
    /// camera from its parent's transform, and initialises the tracked position.
    /// </summary>
    /// <example>
    /// Called automatically by the Godot engine when the node enters the scene tree.
    /// It is not intended to be called directly from other scripts.
    /// </example>
    public override void _Ready()
    {
        PositionSmoothingEnabled = false;
        DragHorizontalEnabled = false;
        DragVerticalEnabled = false;
        ProcessCallback = Camera2DProcessCallback.Idle;
        TopLevel = true;
        ProcessPriority = 100; // run after most other nodes
        truePosition = GlobalPosition;
    }
    /// <summary>
    /// Sets the node the camera follows and leads.
    /// </summary>
    /// <param name="newTarget">The node to follow, or null to stop following.</param>
    /// <param name="snapImmediately">
    /// If true, jumps straight to the target and clears any look-ahead. If false, eases
    /// to the new target from its current position.
    /// </param>
    /// <example>
    /// Called when a level is loaded, e.g. from MessageManager.SetPlayerAsCameraTarget():
    /// <code>
    /// camera.SetTarget(player);
    /// </code>
    /// </example>
    public void SetCameraTarget(Node2D newTarget, bool snapImmediately = true)
    {
        Target = newTarget;
        hasPreviousTarget = false;
        if (snapImmediately && Target != null)
        {
            truePosition = Target.GlobalPosition;
            currentLookAhead = Vector2.Zero;
        }
    }

    public void MoveCameraToPossition(Vector2 incomingPosition)
    {
        var TmpPosition = incomingPosition;
        this.GlobalPosition = TmpPosition.Round();
        currentLookAhead = Vector2.Zero;
    }
    /// <summary>
    /// Godot lifecycle callback. Updates the look-ahead offset and the tracked position,
    /// places the camera on the nearest whole pixel, and sends the remainder to the viewport.
    /// </summary>
    /// <param name="delta">Elapsed time, in seconds, since the previous frame.</param>
    /// <example>
    /// Called automatically by the Godot engine every rendered frame.
    /// It is not intended to be called directly from other scripts.
    /// </example>
    public override void _Process(double delta)
    {
        if (Target != null && IsInstanceValid(Target))
        {
            Vector2 targetVelocity = GetTargetVelocity(delta);
            // Project velocity forward, scale per axis, and clamp to the maximum offset.
            Vector2 projected = targetVelocity * LookAheadTime * LookAheadScale;
            Vector2 desiredLookAhead = new Vector2(
                Mathf.Clamp(projected.X, -MaxLookAhead.X, MaxLookAhead.X),
                Mathf.Clamp(projected.Y, -MaxLookAhead.Y, MaxLookAhead.Y));
            currentLookAhead = currentLookAhead.Lerp(
                desiredLookAhead, Weight(LookAheadSpeed, delta));
            Vector2 goal = Target.GlobalPosition + currentLookAhead;
            truePosition = truePosition.Lerp(goal, Weight(FollowSpeed, delta));
        }
        Vector2 snapped = truePosition.Round();
        GlobalPosition = snapped;
        ForceUpdateScroll();
        SubViewportContainer.Instance?.ViewportWholePixelOnlyMovement(truePosition - snapped);
    }
    /// <summary>
    /// Returns the target's current velocity in pixels per second. Reads the body's own
    /// velocity when available, otherwise estimates it from the target's movement between frames.
    /// </summary>
    /// <param name="delta">Elapsed time, in seconds, since the previous frame.</param>
    /// <returns>The target's estimated velocity, in pixels per second.</returns>
    private Vector2 GetTargetVelocity(double delta)
    {
        if (Target is CharacterBody2D body)
        {
            return body.Velocity;
        }
        if (Target is RigidBody2D rigid)
        {
            return rigid.LinearVelocity;
        }
        // Fallback for any other Node2D: measure movement between frames. This is noisier
        // when the display refresh rate differs from the physics tick rate.
        Vector2 velocity = Vector2.Zero;
        if (hasPreviousTarget && delta > 0.0)
        {
            velocity = (Target.GlobalPosition - previousTargetPosition) / (float)delta;
        }
        previousTargetPosition = Target.GlobalPosition;
        hasPreviousTarget = true;
        return velocity;
    }
    /// <summary>
    /// Converts a smoothing speed into a frame-rate-independent interpolation weight.
    /// </summary>
    /// <param name="speed">Smoothing speed. 0 or less returns 1 (no smoothing).</param>
    /// <param name="delta">Elapsed time, in seconds, since the previous frame.</param>
    /// <returns>A weight between 0 and 1 for use with <c>Vector2.Lerp</c>.</returns>
    private static float Weight(float speed, double delta)
    {
        return speed <= 0f ? 1f : 1f - Mathf.Exp(-speed * (float)delta);
    }
}



/*using Godot;
/// <summary>
/// Camera that tracks a target at full float precision, leads ahead of the target's
/// direction of travel, and places itself on whole game pixels, forwarding the
/// sub-pixel remainder to the viewport shader for smooth scrolling.
/// The target's velocity is estimated from its position, so no player-side code is required.
/// </summary>
public partial class PixelPerfectCamera : Camera2D
{
    // ───────────────────────── Follow ─────────────────────────
    /// <summary>
    /// The node the camera follows (usually the player).
    /// </summary>
    [ExportGroup("Follow")]
    [Export]
    public Node2D Target { get; set; }
    /// <summary>
    /// Follow responsiveness. Higher is snappier; 0 or less locks rigidly to the goal position.
    /// </summary>
    [Export]
    public float FollowSpeed { get; set; } = 10f;
    // ─────────────────────── Look-Ahead ───────────────────────
    /// <summary>
    /// Enables leading the camera ahead of the target's direction of travel.
    /// </summary>
    [ExportGroup("Look-Ahead")]
    [Export]
    public bool LookAheadEnabled { get; set; } = true;
    /// <summary>
    /// Maximum horizontal look-ahead distance, in game pixels.
    /// </summary>
    [Export]
    public float LookAheadHorizontal { get; set; } = 64f;
    /// <summary>
    /// Maximum look-ahead distance when falling, in game pixels.
    /// </summary>
    [Export]
    public float LookAheadDown { get; set; } = 56f;
    /// <summary>
    /// Maximum look-ahead distance when rising, in game pixels. Kept small so jumps don't jerk the view.
    /// </summary>
    [Export]
    public float LookAheadUp { get; set; } = 8f;
    /// <summary>
    /// Target speed (game pixels per second) at which full look-ahead is reached, per axis.
    /// </summary>
    [Export]
    public Vector2 FullLookAheadSpeed { get; set; } = new Vector2(120f, 300f);
    /// <summary>
    /// Horizontal speed (px/s) below which the target is considered stationary on that axis.
    /// </summary>
    [Export]
    public float HorizontalDeadzone { get; set; } = 10f;
    /// <summary>
    /// Falling speed (px/s) that must be exceeded before the camera looks down.
    /// Set high enough that the downward half of an ordinary jump is ignored.
    /// </summary>
    [Export]
    public float FallDeadzone { get; set; } = 150f;
    /// <summary>
    /// Rising speed (px/s) that must be exceeded before the camera looks up.
    /// </summary>
    [Export]
    public float RiseDeadzone { get; set; } = 150f;
    /// <summary>
    /// How quickly the look-ahead offset eases toward its new value. Lower values give
    /// gentler swings when the target reverses direction.
    /// </summary>
    [Export]
    public float LookAheadSpeed { get; set; } = 2.5f;
    /// <summary>
    /// Smoothing applied to the measured target velocity, to filter out single-tick spikes.
    /// </summary>
    [Export]
    public float VelocitySmoothing { get; set; } = 12f;
    /// <summary>
    /// When true, the camera keeps its horizontal lead after the target stops moving,
    /// instead of recentering.
    /// </summary>
    [Export]
    public bool HoldHorizontalWhenIdle { get; set; } = true;
    /// <summary>
    /// Movement in a single physics tick larger than this (game pixels) is treated as a
    /// teleport (respawn, level load), and the camera jumps straight to the target.
    /// </summary>
    [Export]
    public float TeleportDistance { get; set; } = 64f;
    private Vector2 truePosition;
    private Vector2 lookAheadOffset;
    private Vector2 estimatedVelocity;
    private Vector2 lastTargetPosition;
    private float heldHorizontalOffset;
    /// <summary>
    /// Godot lifecycle callback. Disables built-in smoothing and dragging, detaches the camera
    /// from its parent's transform, orders processing after other nodes, and resets tracking.
    /// </summary>
    /// <example>
    /// Called automatically by the Godot engine when the node enters the scene tree.
    /// It is not intended to be called directly from other scripts.
    /// </example>
    public override void _Ready()
    {
        PositionSmoothingEnabled = false;
        DragHorizontalEnabled = false;
        DragVerticalEnabled = false;
        ProcessCallback = Camera2DProcessCallback.Idle;
        TopLevel = true;
        // Run after the player in both loops, so we always see its latest position.
        ProcessPriority = 100;
        ProcessPhysicsPriority = 100;
        truePosition = GlobalPosition;
        ResetCameraTracking();
    }
    /// <summary>
    /// Sets the node the camera follows.
    /// </summary>
    /// <param name="newTarget">The node to follow.</param>
    /// <param name="snapImmediately">
    /// If true, jump straight to the target and clear any look-ahead, instead of easing.
    /// </param>
    /// <example>
    /// <code>
    /// camera.SetTarget(player); // e.g. from MessageManager.SetPlayerAsCameraTarget()
    /// </code>
    /// </example>
    public void SetCameraTarget(Node2D newTarget, bool snapImmediately = true)
    {
        Target = newTarget;
        if (snapImmediately)
        {
            ResetCameraTracking();
        }
        else if (HasValidTarget())
        {
            lastTargetPosition = Target.GlobalPosition;
        }
    }
    /// <summary>
    /// Godot lifecycle callback. Measures how far the target moved this physics tick and
    /// updates the smoothed velocity estimate used for look-ahead.
    /// </summary>
    /// <param name="delta">Elapsed time, in seconds, since the previous physics tick.</param>
    /// <example>
    /// Called automatically by the Godot engine every physics tick.
    /// It is not intended to be called directly from other scripts.
    /// </example>
    public override void _PhysicsProcess(double delta)
    {
        if (!HasValidTarget() || delta <= 0.0)
        {
            return;
        }
        Vector2 current = Target.GlobalPosition;
        Vector2 moved = current - lastTargetPosition;
        lastTargetPosition = current;
        if (moved.Length() > TeleportDistance)
        {
            ResetCameraTracking();
            return;
        }
        float dt = (float)delta;
        Vector2 rawVelocity = moved / dt;
        estimatedVelocity = estimatedVelocity.Lerp(rawVelocity, ExpWeight(VelocitySmoothing, dt));
    }
    /// <summary>
    /// Godot lifecycle callback. Eases the look-ahead offset, eases the float-precision camera
    /// position toward target + offset, snaps the camera to the nearest whole pixel, and sends
    /// the remainder to the viewport.
    /// </summary>
    /// <param name="delta">Elapsed time, in seconds, since the previous frame.</param>
    /// <example>
    /// Called automatically by the Godot engine every rendered frame.
    /// It is not intended to be called directly from other scripts.
    /// </example>
    public override void _Process(double delta)
    {
        float dt = (float)delta;
        if (HasValidTarget())
        {
            Vector2 desiredLookAhead = LookAheadEnabled ? CalculateDesiredLookAhead() : Vector2.Zero;
            lookAheadOffset = lookAheadOffset.Lerp(desiredLookAhead, ExpWeight(LookAheadSpeed, dt));
            Vector2 goal = Target.GlobalPosition + lookAheadOffset;
            truePosition = FollowSpeed <= 0f
                ? goal
                : truePosition.Lerp(goal, ExpWeight(FollowSpeed, dt));
        }
        Vector2 snapped = truePosition.Round();
        GlobalPosition = snapped;
        ForceUpdateScroll();
        SubViewportContainer.Instance?.ViewportWholePixelOnlyMovement(truePosition - snapped);
    }
    /// <summary>
    /// Converts the estimated target velocity into the look-ahead offset the camera should
    /// be easing toward, applying per-axis deadzones and distance limits.
    /// </summary>
    private Vector2 CalculateDesiredLookAhead()
    {
        Vector2 v = estimatedVelocity;
        // Horizontal: symmetric, optionally held when the target stops.
        float x;
        float horizontalAmount = Ramp(Mathf.Abs(v.X), HorizontalDeadzone, FullLookAheadSpeed.X);
        if (horizontalAmount > 0f)
        {
            x = Mathf.Sign(v.X) * horizontalAmount * LookAheadHorizontal;
            heldHorizontalOffset = x;
        }
        else
        {
            x = HoldHorizontalWhenIdle ? heldHorizontalOffset : 0f;
        }
        // Vertical: Godot's +Y is down, so falling is positive velocity.
        float y = 0f;
        if (v.Y > 0f)
        {
            y = Ramp(v.Y, FallDeadzone, FullLookAheadSpeed.Y) * LookAheadDown;
        }
        else if (v.Y < 0f)
        {
            y = -Ramp(-v.Y, RiseDeadzone, FullLookAheadSpeed.Y) * LookAheadUp;
        }
        return new Vector2(x, y);
    }
    /// <summary>
    /// Returns 0 below the deadzone, rising linearly to 1 at full speed.
    /// </summary>
    private static float Ramp(float speed, float deadzone, float fullSpeed)
    {
        if (speed <= deadzone)
        {
            return 0f;
        }
        float range = Mathf.Max(fullSpeed - deadzone, 0.001f);
        return Mathf.Clamp((speed - deadzone) / range, 0f, 1f);
    }
    /// <summary>
    /// Frame-rate independent exponential smoothing weight.
    /// </summary>
    private static float ExpWeight(float speed, float dt)
    {
        return 1f - Mathf.Exp(-speed * dt);
    }
    /// <summary>
    /// Returns true if the target exists and has not been freed.
    /// </summary>
    private bool HasValidTarget()
    {
        return Target != null && IsInstanceValid(Target);
    }
    /// <summary>
    /// Jumps the camera directly onto the target and clears velocity and look-ahead state.
    /// Used on target change and when a teleport is detected.
    /// </summary>
    public void ResetCameraTracking()
    {
        estimatedVelocity = Vector2.Zero;
        lookAheadOffset = Vector2.Zero;
        heldHorizontalOffset = 0f;
        if (HasValidTarget())
        {
            lastTargetPosition = Target.GlobalPosition;
            truePosition = Target.GlobalPosition;
        }
    }
    public void MoveCameraToPossition(Vector2 incomingPossition)
    {
        estimatedVelocity = Vector2.Zero;
        lookAheadOffset = Vector2.Zero;
        heldHorizontalOffset = 0f;
        Target = null;
        truePosition = incomingPossition.Round();
    }
}
*/
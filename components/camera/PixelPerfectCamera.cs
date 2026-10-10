using Godot;

/// <summary>
/// Pixel-perfect follow camera with directional look-ahead.
///
/// Tracks its target at full float precision, leads ahead of the target's direction of
/// travel (with dead-zones so an ordinary jump barely moves the view while a long fall
/// tilts the camera down), then places itself on whole game pixels and forwards the
/// sub-pixel remainder to the viewport shader for smooth scrolling.
///
/// The target's velocity is estimated locally from its position once per physics tick, so
/// no player-side code is required. The target is set from code via
/// <see cref="SetCameraTarget"/> and is intentionally not exposed in the inspector.
///
/// Per-project tuning lives entirely in the exported properties; the code itself is
/// intended to stay identical across projects.
/// </summary>
public partial class PixelPerfectCamera : Camera2D
{
    // ───────────────────────── Constants ─────────────────────────

    /// <summary>
    /// Process priority high enough that the camera runs after the gameplay nodes it follows,
    /// so it always reads their latest position for the current frame.
    /// </summary>
    private const int RunAfterGameplayNodesPriority = 100;

    /// <summary>
    /// Distance (game pixels) below which an eased value is considered to have arrived at its goal.
    /// </summary>
    private const float ArrivalTolerance = 0.001f;

    /// <summary>
    /// Distance (game pixels) from the new lead at which a horizontal reversal swing is considered finished.
    /// </summary>
    private const float ReversalCompleteTolerance = 0.5f;

    /// <summary>
    /// Smallest allowed gap between a dead-zone and its full-lead speed, preventing divide-by-zero
    /// when a designer sets them equal.
    /// </summary>
    private const float MinimumSpeedRange = 0.001f;

    // ───────────────────────── Follow ─────────────────────────

    /// <summary>
    /// Follow responsiveness. Higher is snappier; 0 or less locks rigidly to the goal position
    /// (target + look-ahead). Replaces Camera2D's built-in position smoothing, which this camera
    /// disables in <see cref="_Ready"/>.
    /// </summary>
    [ExportGroup("Follow")]
    [Export]
    public float FollowSpeed { get; set; } = 10f;

    /// <summary>
    /// Target movement between two velocity samples larger than this (game pixels) is treated
    /// as a teleport (respawn, level load, checkpoint): the camera jumps straight to the target
    /// and all look-ahead is cleared.
    /// </summary>
    [Export]
    public float TeleportDistance { get; set; } = 64f;

    // ─────────────────────── Look-Ahead ───────────────────────

    /// <summary>
    /// Enables leading the camera ahead of the target's direction of travel.
    /// When disabled, any existing lead eases back to centre at <see cref="ReturnRate"/>
    /// (horizontal) and <see cref="VerticalReturnRate"/> (vertical).
    /// </summary>
    [ExportGroup("Look-Ahead")]
    [Export]
    public bool LookAheadEnabled { get; set; } = true;

    /// <summary>
    /// When true, the horizontal lead never eases back toward centre on its own: it only extends
    /// further in the direction of travel or swings across on a reversal, and is cleared only by
    /// a snapped target change, a detected teleport, <see cref="MoveCameraToPosition"/>, or
    /// switching <see cref="LookAheadEnabled"/> off.
    /// When false, the lead eases toward the speed-proportional value at <see cref="ReturnRate"/>
    /// whenever the target slows down or stops (returning fully to centre when stationary).
    /// </summary>
    [Export]
    public bool HoldHorizontalLead { get; set; } = true;

    /// <summary>
    /// Maximum horizontal look-ahead distance, in game pixels (e.g. 64 = 10% of a 640 px wide view).
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
    /// Target speed (game pixels per second), per axis, at which the full look-ahead distance is
    /// reached. Speeds between the dead-zone and this value give a proportionally smaller lead.
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
    /// Smoothing applied to the measured target velocity, to filter out single-tick spikes.
    /// Higher reacts faster; 0 or less uses the raw per-tick velocity.
    /// </summary>
    [Export]
    public float VelocitySmoothing { get; set; } = 12f;

    // ───────────────────── Look-Ahead Easing ─────────────────────

    /// <summary>
    /// Speed (px/s) at which the horizontal lead extends further in the current direction of travel.
    /// </summary>
    [ExportGroup("Look-Ahead Easing")]
    [Export]
    public float AdvanceRate { get; set; } = 90f;

    /// <summary>
    /// Speed (px/s) at which the horizontal lead swings across to the other side when the target
    /// turns around. Should be faster than <see cref="ReturnRate"/> but kept modest to avoid
    /// motion sickness: a full swing of 2 × <see cref="LookAheadHorizontal"/> at 50 px/s takes ~2.5 s.
    /// </summary>
    [Export]
    public float ReverseRate { get; set; } = 50f;

    /// <summary>
    /// Speed (px/s) at which the horizontal lead eases back toward centre. Used when
    /// <see cref="HoldHorizontalLead"/> is false and the target slows or stops, and when
    /// <see cref="LookAheadEnabled"/> is switched off.
    /// </summary>
    [Export]
    public float ReturnRate { get; set; } = 20f;

    /// <summary>
    /// Speed (px/s) at which the vertical lead extends while falling or rising past the dead-zones.
    /// </summary>
    [Export]
    public float VerticalAdvanceRate { get; set; } = 120f;

    /// <summary>
    /// Speed (px/s) at which the vertical lead returns toward centre (e.g. after landing).
    /// </summary>
    [Export]
    public float VerticalReturnRate { get; set; } = 45f;

    /// <summary>
    /// Exponential ease-out applied as a lead approaches its goal, so rate-limited moves settle
    /// softly instead of stopping dead. 0 or less disables the ease-out. If both this and the
    /// relevant rate are 0 the lead snaps instantly.
    /// </summary>
    [Export]
    public float LookAheadEasing { get; set; } = 4f;

    // ───────────────────────── State ─────────────────────────

    /// <summary>The node being followed. Null when the camera is parked by <see cref="MoveCameraToPosition"/>.</summary>
    private Node2D followTarget;

    /// <summary>Float-precision camera position. <c>GlobalPosition</c> is always this value rounded.</summary>
    private Vector2 preciseCameraPosition;

    /// <summary>Current lead, in game pixels, added to the target position to form the follow goal.</summary>
    private Vector2 lookAheadOffset;

    /// <summary>Smoothed target velocity (px/s) derived from position samples.</summary>
    private Vector2 estimatedTargetVelocity;

    /// <summary>Target position at the most recent velocity sample.</summary>
    private Vector2 lastSampledTargetPosition;

    /// <summary>Engine physics-frame count at the most recent velocity sample.</summary>
    private ulong lastSampledPhysicsFrame;

    /// <summary>True while the horizontal lead is swinging from one side to the other after a turn-around.</summary>
    private bool isReversingHorizontalLead;

    // ───────────────────────── Lifecycle ─────────────────────────

    /// <summary>
    /// Godot lifecycle callback. Registers with the MessageManager, disables built-in smoothing and
    /// dragging, detaches the camera from its parent's transform, orders processing after other
    /// nodes, and resets tracking.
    /// </summary>
    /// <example>
    /// Called automatically by the Godot engine when the node enters the scene tree.
    /// It is not intended to be called directly from other scripts.
    /// </example>
    public override void _Ready()
    {
        MessageManager.instance.AddCameraToMessager(this);

        PositionSmoothingEnabled = false;
        DragHorizontalEnabled = false;
        DragVerticalEnabled = false;
        ProcessCallback = Camera2DProcessCallback.Idle;
        TopLevel = true;
        ProcessPriority = RunAfterGameplayNodesPriority;

        preciseCameraPosition = GlobalPosition;
        SnapToTargetAndResetTracking();
    }

    /// <summary>
    /// Godot lifecycle callback. Samples the target's velocity (once per physics tick), eases the
    /// look-ahead offset, eases the float-precision camera position toward target + offset, snaps
    /// the camera to the nearest whole pixel, and sends the remainder to the viewport.
    /// </summary>
    /// <param name="delta">Elapsed time, in seconds, since the previous frame.</param>
    /// <example>
    /// Called automatically by the Godot engine every rendered frame.
    /// It is not intended to be called directly from other scripts.
    /// </example>
    public override void _Process(double delta)
    {
        float frameDelta = (float)delta;

        if (HasValidTarget())
        {
            UpdateTargetVelocityEstimate();
            UpdateLookAheadOffset(frameDelta);

            Vector2 followGoal = followTarget.GlobalPosition + lookAheadOffset;
            preciseCameraPosition = FollowSpeed <= 0f
                ? followGoal
                : preciseCameraPosition.Lerp(followGoal, ExponentialSmoothingWeight(FollowSpeed, frameDelta));
        }

        Vector2 wholePixelPosition = preciseCameraPosition.Round();
        GlobalPosition = wholePixelPosition;
        ForceUpdateScroll();
        SubViewportContainer.Instance?.ViewportWholePixelOnlyMovement(preciseCameraPosition - wholePixelPosition);
    }

    // ───────────────────────── Public API ─────────────────────────

    /// <summary>
    /// Sets the node the camera follows.
    /// </summary>
    /// <param name="newTarget">The node to follow.</param>
    /// <param name="snapImmediately">
    /// If true, jump straight to the target and clear velocity and look-ahead, instead of easing.
    /// </param>
    /// <example>
    /// <code>
    /// camera.SetCameraTarget(player); // e.g. from MessageManager.SetPlayerAsCameraTarget()
    /// </code>
    /// </example>
    public void SetCameraTarget(Node2D newTarget, bool snapImmediately = true)
    {
        followTarget = newTarget;

        if (snapImmediately)
        {
            SnapToTargetAndResetTracking();
        }
        else if (HasValidTarget())
        {
            // Keep the current position and lead so the camera eases across, but start velocity
            // sampling fresh so the jump to the new target isn't read as motion.
            RestartVelocitySampling();
            isReversingHorizontalLead = false;
        }
    }

    /// <summary>
    /// Moves the camera directly to a world position and stops following any target,
    /// clearing velocity and look-ahead state. Call <see cref="SetCameraTarget"/> to resume following.
    /// </summary>
    /// <param name="incomingPosition">World position to place the camera at.</param>
    public void MoveCameraToPosition(Vector2 incomingPosition)
    {
        followTarget = null;
        ClearLookAheadState();

        preciseCameraPosition = incomingPosition.Round();
        GlobalPosition = preciseCameraPosition;
    }

    // ───────────────────────── Velocity Sampling ─────────────────────────

    /// <summary>
    /// Measures how far the target moved since the last physics tick and updates the smoothed
    /// velocity estimate. Sampling is tied to physics ticks (rather than render frames) so a
    /// physics-driven target that hasn't stepped this frame doesn't read as "stopped" at high
    /// refresh rates. Physics ticks always complete before <c>_Process</c> runs, so the position
    /// read here is the latest one. Requires a valid target.
    /// </summary>
    private void UpdateTargetVelocityEstimate()
    {
        ulong physicsFrame = Engine.GetPhysicsFrames();
        if (physicsFrame == lastSampledPhysicsFrame)
        {
            return; // No physics tick since the last sample.
        }

        ulong ticksSinceLastSample = physicsFrame - lastSampledPhysicsFrame;
        lastSampledPhysicsFrame = physicsFrame;

        Vector2 targetPosition = followTarget.GlobalPosition;
        Vector2 displacement = targetPosition - lastSampledTargetPosition;
        lastSampledTargetPosition = targetPosition;

        // Squared comparison avoids a square root every physics tick.
        if (displacement.LengthSquared() > TeleportDistance * TeleportDistance)
        {
            SnapToTargetAndResetTracking();
            return;
        }

        float elapsedSeconds = (float)GetPhysicsProcessDeltaTime() * ticksSinceLastSample;
        if (elapsedSeconds <= 0f)
        {
            return;
        }

        Vector2 measuredVelocity = displacement / elapsedSeconds;
        estimatedTargetVelocity = VelocitySmoothing <= 0f
            ? measuredVelocity
            : estimatedTargetVelocity.Lerp(measuredVelocity, ExponentialSmoothingWeight(VelocitySmoothing, elapsedSeconds));
    }

    // ───────────────────────── Look-Ahead ─────────────────────────

    /// <summary>
    /// Eases <see cref="lookAheadOffset"/> toward the lead implied by the estimated velocity,
    /// or back to centre if look-ahead is disabled.
    /// </summary>
    private void UpdateLookAheadOffset(float frameDelta)
    {
        if (!LookAheadEnabled)
        {
            isReversingHorizontalLead = false;
            lookAheadOffset = new Vector2(
                EaseTowardAtMaxRate(lookAheadOffset.X, 0f, ReturnRate, frameDelta),
                EaseTowardAtMaxRate(lookAheadOffset.Y, 0f, VerticalReturnRate, frameDelta));
            return;
        }

        UpdateHorizontalLookAhead(frameDelta);
        UpdateVerticalLookAhead(frameDelta);
    }

    /// <summary>
    /// Horizontal lead: extends in the direction of travel at <see cref="AdvanceRate"/>, swings
    /// across at <see cref="ReverseRate"/> when the target turns around, and either holds or
    /// eases inward at <see cref="ReturnRate"/> when the target slows or stops, depending on
    /// <see cref="HoldHorizontalLead"/>.
    /// </summary>
    private void UpdateHorizontalLookAhead(float frameDelta)
    {
        float currentLead = lookAheadOffset.X;
        float horizontalSpeed = Mathf.Abs(estimatedTargetVelocity.X);
        float leadFraction = LeadFractionForSpeed(horizontalSpeed, HorizontalDeadzone, FullLookAheadSpeed.X);

        if (leadFraction <= 0f)
        {
            // Target is stationary on this axis.
            isReversingHorizontalLead = false;
            if (!HoldHorizontalLead)
            {
                lookAheadOffset.X = EaseTowardAtMaxRate(currentLead, 0f, ReturnRate, frameDelta);
            }
            return;
        }

        int travelDirection = SignOf(estimatedTargetVelocity.X);
        int currentLeadSide = SignOf(currentLead);
        float desiredLead = travelDirection * leadFraction * LookAheadHorizontal;

        bool alreadyFarEnoughAhead = currentLeadSide == travelDirection
                                     && Mathf.Abs(desiredLead) <= Mathf.Abs(currentLead);
        if (alreadyFarEnoughAhead)
        {
            // Target is moving, but slower than when the current lead was reached.
            if (!HoldHorizontalLead)
            {
                lookAheadOffset.X = EaseTowardAtMaxRate(currentLead, desiredLead, ReturnRate, frameDelta);
            }
            return;
        }

        // A reversal begins when the lead sits on the opposite side to the new direction of
        // travel, and persists until the new lead is reached so the whole swing runs at
        // ReverseRate (not just the half before crossing centre).
        bool leadIsOnOppositeSide = currentLeadSide != 0 && currentLeadSide != travelDirection;
        if (leadIsOnOppositeSide)
        {
            isReversingHorizontalLead = true;
        }

        float rate = isReversingHorizontalLead ? ReverseRate : AdvanceRate;
        float newLead = EaseTowardAtMaxRate(currentLead, desiredLead, rate, frameDelta);

        if (isReversingHorizontalLead && Mathf.Abs(newLead - desiredLead) < ReversalCompleteTolerance)
        {
            isReversingHorizontalLead = false;
        }

        lookAheadOffset.X = newLead;
    }

    /// <summary>
    /// Vertical lead: extends at <see cref="VerticalAdvanceRate"/> while falling or rising past the
    /// dead-zones, and returns at <see cref="VerticalReturnRate"/> otherwise (e.g. after landing).
    /// Godot's +Y is down, so falling is positive velocity.
    /// </summary>
    private void UpdateVerticalLookAhead(float frameDelta)
    {
        float currentLead = lookAheadOffset.Y;
        float verticalVelocity = estimatedTargetVelocity.Y;

        float desiredLead = 0f;
        if (verticalVelocity > 0f)
        {
            desiredLead = LeadFractionForSpeed(verticalVelocity, FallDeadzone, FullLookAheadSpeed.Y) * LookAheadDown;
        }
        else if (verticalVelocity < 0f)
        {
            desiredLead = -LeadFractionForSpeed(-verticalVelocity, RiseDeadzone, FullLookAheadSpeed.Y) * LookAheadUp;
        }

        // "Extending" means pushing further out on the same side (or out from centre).
        // Returning toward centre, or crossing to the other side, uses the gentler return rate.
        bool sameSideOrFromCentre = desiredLead * currentLead >= 0f;
        bool isExtending = desiredLead != 0f
                           && sameSideOrFromCentre
                           && Mathf.Abs(desiredLead) > Mathf.Abs(currentLead);

        float rate = isExtending ? VerticalAdvanceRate : VerticalReturnRate;
        lookAheadOffset.Y = EaseTowardAtMaxRate(currentLead, desiredLead, rate, frameDelta);
    }

    // ───────────────────────── Math Helpers ─────────────────────────

    /// <summary>
    /// Moves <paramref name="current"/> toward <paramref name="goal"/> by at most
    /// <paramref name="maxRate"/> px/s, with an exponential ease-out (<see cref="LookAheadEasing"/>)
    /// as it nears the goal. A rate of 0 or less removes the cap; if the ease-out is also
    /// disabled the value snaps to the goal.
    /// </summary>
    private float EaseTowardAtMaxRate(float current, float goal, float maxRate, float frameDelta)
    {
        float remaining = goal - current;
        if (Mathf.Abs(remaining) < ArrivalTolerance)
        {
            return goal;
        }

        float step = Mathf.Abs(remaining);
        if (LookAheadEasing > 0f)
        {
            step *= ExponentialSmoothingWeight(LookAheadEasing, frameDelta);
        }
        if (maxRate > 0f)
        {
            step = Mathf.Min(step, maxRate * frameDelta);
        }

        return current + (remaining > 0f ? step : -step);
    }

    /// <summary>
    /// Converts a speed into a 0–1 lead fraction: 0 at or below <paramref name="deadzone"/>,
    /// rising linearly to 1 at <paramref name="fullLeadSpeed"/>.
    /// </summary>
    private static float LeadFractionForSpeed(float speed, float deadzone, float fullLeadSpeed)
    {
        if (speed <= deadzone)
        {
            return 0f;
        }

        float speedRange = Mathf.Max(fullLeadSpeed - deadzone, MinimumSpeedRange);
        return Mathf.Clamp((speed - deadzone) / speedRange, 0f, 1f);
    }

    /// <summary>
    /// Frame-rate independent lerp weight for exponential smoothing: the fraction of the remaining
    /// distance to cover this frame for a given responsiveness (higher = faster).
    /// </summary>
    private static float ExponentialSmoothingWeight(float responsiveness, float frameDelta)
    {
        return 1f - Mathf.Exp(-responsiveness * frameDelta);
    }

    /// <summary>
    /// -1, 0 or +1 for the sign of a value.
    /// </summary>
    private static int SignOf(float value)
    {
        return value > 0f ? 1 : (value < 0f ? -1 : 0);
    }

    // ───────────────────────── State Helpers ─────────────────────────

    /// <summary>
    /// Returns true if the target exists and has not been freed.
    /// </summary>
    private bool HasValidTarget()
    {
        return followTarget != null && IsInstanceValid(followTarget);
    }

    /// <summary>
    /// Jumps the camera directly onto the target and clears velocity and look-ahead state.
    /// Used on ready, snapped target change, and when a teleport is detected.
    /// </summary>
    private void SnapToTargetAndResetTracking()
    {
        ClearLookAheadState();
        lastSampledPhysicsFrame = Engine.GetPhysicsFrames();

        if (HasValidTarget())
        {
            lastSampledTargetPosition = followTarget.GlobalPosition;
            preciseCameraPosition = followTarget.GlobalPosition;
        }
    }

    /// <summary>
    /// Restarts velocity sampling from the target's current position, so the next sample measures
    /// only movement from here onward. Requires a valid target.
    /// </summary>
    private void RestartVelocitySampling()
    {
        estimatedTargetVelocity = Vector2.Zero;
        lastSampledTargetPosition = followTarget.GlobalPosition;
        lastSampledPhysicsFrame = Engine.GetPhysicsFrames();
    }

    /// <summary>
    /// Zeroes the velocity estimate, the lead, and any in-progress reversal.
    /// </summary>
    private void ClearLookAheadState()
    {
        estimatedTargetVelocity = Vector2.Zero;
        lookAheadOffset = Vector2.Zero;
        isReversingHorizontalLead = false;
    }
}
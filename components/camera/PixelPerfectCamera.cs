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
    public void MoveToPosition(Vector2 incomingPosition)
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

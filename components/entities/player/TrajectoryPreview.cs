using Godot;
using Godot.Collections;
/// <summary>Draws the predicted bullet path as a series of dots, stopping at the first obstacle.</summary>
public partial class TrajectoryPreview : Node2D
{
    [Export] public int Resolution = 30;          // number of dots
    [Export] public float TimeStep = 0.05f;       // seconds between dots
    [Export] public float DotRadius = 2.0f;
    [Export] public Color DotColor = new(1, 1, 1, 0.9f);
    /// <summary>Physics layers that stop the arc (walls, floors, etc.).</summary>
    [Export(PropertyHint.Layers2DPhysics)] public uint ObstacleCollisionMask = 1;
    /// <summary>Bodies the ray ignores, such as the player itself.</summary>
    public Array<Rid> ExcludedBodies = new();
    private Vector2[] dotPositions = [];
    private int visibleDotCount = 0;
    public override void _Ready()
    {
        // Draw in global coordinates so the dots don't move with the player's
        // parent transform. ToLocal() is used in _Draw to convert back.
        TopLevel = true;
        ZIndex = 10;
    }
    /// <summary>Recalculates the arc from a start point, launch velocity, and gravity.</summary>
    public void UpdateArc(Vector2 startGlobal, Vector2 launchVelocity, float gravity)
    {
        if (dotPositions.Length != Resolution)
            dotPositions = new Vector2[Resolution];
        float secondsPerPhysicsTick = 1.0f / Engine.PhysicsTicksPerSecond;
        int physicsTicksPerDot = Mathf.Max(1, Mathf.RoundToInt(TimeStep / secondsPerPhysicsTick));
        PhysicsDirectSpaceState2D spaceState = GetWorld2D().DirectSpaceState;
        Vector2 currentPosition = startGlobal;
        Vector2 currentVelocity = launchVelocity;
        visibleDotCount = Resolution;
        for (int dotIndex = 0; dotIndex < Resolution; dotIndex++)
        {
            dotPositions[dotIndex] = currentPosition;
            for (int tick = 0; tick < physicsTicksPerDot; tick++)
            {
                // Same order as Bullet._PhysicsProcess: velocity first, then position.
                currentVelocity.Y += gravity * secondsPerPhysicsTick;
                Vector2 nextPosition = currentPosition + currentVelocity * secondsPerPhysicsTick;
                PhysicsRayQueryParameters2D query =
                    PhysicsRayQueryParameters2D.Create(currentPosition, nextPosition, ObstacleCollisionMask);
                query.Exclude = ExcludedBodies;
                Dictionary hit = spaceState.IntersectRay(query);
                if (hit.Count > 0)
                {
                    // Arc ends here. Dots up to and including dotIndex are still drawn.
                    visibleDotCount = dotIndex + 1;
                    QueueRedraw();
                    return;
                }
                currentPosition = nextPosition;
            }
        }
        QueueRedraw();
    }
    public override void _Draw()
    {
        for (int dotIndex = 0; dotIndex < visibleDotCount; dotIndex++)
        {
            // Fade the dots out toward the end of the arc.
            float fade = 1.0f - (float)dotIndex / visibleDotCount;
            Color color = new(DotColor.R, DotColor.G, DotColor.B, DotColor.A * fade);
            DrawCircle(ToLocal(dotPositions[dotIndex]), DotRadius, color);
        }
    }
}
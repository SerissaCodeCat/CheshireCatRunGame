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
    private Vector2[] dotPositions = [];
    private int visibleDotCount = 0;
    public override void _Ready()
    {
        // Draw in global coordinates so the dots don't move with the player's
        // parent transform. ToLocal() is used in _Draw to convert back.
        TopLevel = true;
        ZIndex = 10;
    }

        // These must match the values used by Bullet. If those change, change these too.
    private float BounceDamping = 0.4f;
    private const int MaxImpactsWhenShowingAfterBounce = 2;
    private const float SurfaceSeparationDistance = 0.5f; // nudges the start point off the surface

    /// <summary>
    /// Recalculates the arc from a start point, launch velocity, and gravity.
    /// </summary>
    /// <param name="startGlobal">Where the bullet is launched from, in global coordinates.</param>
    /// <param name="launchVelocity">Initial velocity of the bullet.</param>
    /// <param name="gravity">Gravity applied each physics tick.</param>
    /// <param name="incomingBounceDamping">The amount that the momentum is lost on a bounce. 1.0f carry ALL momentum on a bounce, 0.0f stop dead.</param>
    /// <param name="showAfterBounce">If true, the arc continues through the first bounce.</param>
    public void UpdateArc(
        Vector2 startGlobal,
        Vector2 launchVelocity,
        float gravity,
        float incomingBounceDamping = 0.4f,
        bool showAfterBounce = true)
    {
        if (dotPositions.Length != Resolution)
            dotPositions = new Vector2[Resolution];
        float secondsPerPhysicsTick = 1.0f / Engine.PhysicsTicksPerSecond;
        int physicsTicksPerDot = Mathf.Max(1, Mathf.RoundToInt(TimeStep / secondsPerPhysicsTick));
        int totalPhysicsTicks = Resolution * physicsTicksPerDot;
        int maxImpacts = showAfterBounce ? MaxImpactsWhenShowingAfterBounce : 1;
        int impactCount = 0;
        PhysicsDirectSpaceState2D spaceState = GetWorld2D().DirectSpaceState;
        Vector2 currentPosition = startGlobal;
        Vector2 currentVelocity = launchVelocity;
        visibleDotCount = Resolution;
        BounceDamping = incomingBounceDamping;
        for (int tick = 0; tick < totalPhysicsTicks; tick++)
        {
            int dotIndex = tick / physicsTicksPerDot;
            // Record a dot at the start of each dot interval.
            if (tick % physicsTicksPerDot == 0)
                dotPositions[dotIndex] = currentPosition;
            // Same order as Bullet._PhysicsProcess: gravity first, then movement.
            currentVelocity.Y += gravity * secondsPerPhysicsTick;
            Vector2 nextPosition = currentPosition + currentVelocity * secondsPerPhysicsTick;
            PhysicsRayQueryParameters2D query =
                PhysicsRayQueryParameters2D.Create(currentPosition, nextPosition, ObstacleCollisionMask);
            Dictionary hit = spaceState.IntersectRay(query);
            if (hit.Count == 0)
            {
                currentPosition = nextPosition;
                continue;
            }
            impactCount++;
            Vector2 impactPoint = (Vector2)hit["position"];
            Vector2 impactNormal = (Vector2)hit["normal"];
            // Final impact for this arc: end the dots here.
            if (impactCount >= maxImpacts)
            {
                visibleDotCount = dotIndex + 1;
                QueueRedraw();
                return;
            }
            // Bounce: reflect the velocity and continue from the impact point.
            currentVelocity = currentVelocity.Bounce(impactNormal) * BounceDamping;
            currentPosition = impactPoint + impactNormal * SurfaceSeparationDistance;
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
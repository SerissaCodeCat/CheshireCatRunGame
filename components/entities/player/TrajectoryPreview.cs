using Godot;
/// <summary>Draws the predicted bullet path as a series of dots.</summary>
public partial class TrajectoryPreview : Node2D
{
    [Export] public int Resolution = 30;          // number of dots
    [Export] public float TimeStep = 0.05f;       // seconds between dots
    [Export] public float DotRadius = 2.0f;
    [Export] public Color DotColor = new(1, 1, 1, 0.9f);
    private Vector2[] points = [];
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
		if (points.Length != Resolution)
			points = new Vector2[Resolution];
		float physicsDt = 1.0f / Engine.PhysicsTicksPerSecond;
		int stepsPerDot = Mathf.Max(1, Mathf.RoundToInt(TimeStep / physicsDt));
		Vector2 pos = startGlobal;
		Vector2 vel = launchVelocity;
		for (int i = 0; i < Resolution; i++)
		{
			points[i] = pos;
			for (int s = 0; s < stepsPerDot; s++)
			{
				vel.Y += gravity * physicsDt;   // same order as Bullet._PhysicsProcess
				pos += vel * physicsDt;
			}
		}
		QueueRedraw();
	}
    public override void _Draw()
    {
        for (int i = 0; i < points.Length; i++)
        {
            // Fade the dots out toward the end of the arc.
            float fade = 1.0f - (float)i / points.Length;
            Color c = new(DotColor.R, DotColor.G, DotColor.B, DotColor.A * fade);
            DrawCircle(ToLocal(points[i]), DotRadius, c);
        }
    }
}

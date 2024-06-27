using Godot;
using System.Numerics;

public partial class Bullet : CharacterBody2D
{
	private Godot.Vector2 currentVelocity = new Godot.Vector2 (0.0f, 0.0f);
	const float speed = 650.0f; 
	const float deceleration = 4.0f; 
	private float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
	private Godot.Vector2 spawn;
	private bool goRight = true;
	private double lifespan = 5.0d;
	private short maxBounces = 3;

	public enum bulletTypes
	{
		basic,
		bouncing,
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		base._Ready();
		GlobalPosition = spawn;
	}

	public void setup (Godot.Vector2 incomingSpawn, Godot.Vector2 incomingDirectionalVelocity, bulletTypes incomingType)
	{
		spawn = incomingSpawn;
		currentVelocity = incomingDirectionalVelocity * 5;

		if (incomingType == bulletTypes.basic)
		{
			maxBounces = 0;
		}
		else if (incomingType == bulletTypes.bouncing)
		{
			maxBounces = 2;
		}
	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		//GD.Print("CoOrd X = " + GlobalPosition.X );
		//GD.Print("CoOrd Y = " + GlobalPosition.Y );
		//currentVelocity.Y += gravity * (float)delta;
		//currentVelocity.X = Mathf.MoveToward(currentVelocity.X, 0, deceleration);
		Velocity = currentVelocity;
		MoveAndSlide();

        if ((lifespan -= delta) <= 0.0d || GetSlideCollisionCount() > maxBounces)
		{
			QueueFree();
		}
	}
	
}
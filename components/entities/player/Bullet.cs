using Godot;

public partial class Bullet : CharacterBody2D
{
    private Godot.Vector2 currentVelocity = new Godot.Vector2(0.0f, 0.0f);
    [Export]
    public float speed = 200.0f;
    [Export]
    public short maxBounces = 3;
    [Export]
    public float deceleration = 3.0f;
    private float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
    private Godot.Vector2 spawn;
    private bool goRight = true;
    private double lifespan = 5.0d;

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

    public void setup(Godot.Vector2 incomingSpawn, Godot.Vector2 incomingDirectionalVelocity, bulletTypes incomingType)
    {
        spawn = incomingSpawn;
        currentVelocity = incomingDirectionalVelocity.Normalized() * speed;

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

        currentVelocity.Y += gravity * (float)delta;
        //currentVelocity.X = Mathf.MoveToward(currentVelocity.X, 0, deceleration);
        Velocity = currentVelocity;
        MoveAndSlide();
        int tempCollisionCount = GetSlideCollisionCount();
        if (tempCollisionCount != 0)
        {
            SoundManager.instance.playPossitionalAudio("bulletImpact", GlobalPosition.X, GlobalPosition.Y);
            for (int i = 0; i < tempCollisionCount; i++)
            {
                KinematicCollision2D tmp = GetSlideCollision(i);
                GD.Print("Hit " + (tmp.GetCollider() as Node2D).Name.ToString());
                if ((tmp.GetCollider() as Node2D).Name.ToString().Contains("Enemy"))
                {
                    MessageManager.instance.stunEnemyWithID(tmp.GetColliderId());
                }
                else if ((tmp.GetCollider() as Node2D).Name.ToString().Contains("Button"))
                {
                    MessageManager.instance.activateInteractableWithID(tmp.GetColliderId());
                }

            }
        }
        if ((lifespan -= delta) <= 0.0d)
        {
            QueueFree();
        }
        if (GetSlideCollisionCount() > maxBounces)
        {
            QueueFree();
        }
    }

}

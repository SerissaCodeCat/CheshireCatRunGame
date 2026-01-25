using System;
using Godot;

public partial class Bullet : CharacterBody2D
{
    private Godot.Vector2 currentVelocity = new Godot.Vector2(0.0f, 0.0f);
    private Godot.Vector2 previousLocation = new Godot.Vector2(0.0f, 0.0f);
    [Export]
    public float speed = 200.0f;
    [Export]
    public short maxBounces = 3;
    [Export]
    public float deceleration = 3.0f;
    [Export]
    public float groundDecelerationMultiplier = 2.0f;
    [Export]
    public float standardBounceDamping = 0.4f;
    [Export]
    public float bounceyBallBounceDamping = 0.8f;
    private float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
    private Godot.Vector2 spawn;
    private bool goRight = true;
    [Export]
    // Lifespan in seconds of the bullet. after this time it will be removed from the scene.
    private double lifespan = 5.0d;
    private double timeAlive;
    private bool canActivateThings = true;
    [Export]
    //play impact sound when bullet hits something while moving at this or greater pixel per second speed
    private float impactSoundSpeedThreshold = 10.0f;

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
        timeAlive = 0.0d;
        if (incomingType == bulletTypes.basic)
        {
            maxBounces = 0;
        }
        else if (incomingType == bulletTypes.bouncing)
        {
            maxBounces = 2;
        }
        previousLocation = spawn;
    }
    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _PhysicsProcess(double delta)
    {
        //apply gravity
        if( canActivateThings )
        {
            currentVelocity.Y += gravity * (float)delta;
        
            //calculate standard deceleration on ground
            if (IsOnFloor())
            {
                currentVelocity.X = Mathf.MoveToward(currentVelocity.X, 0, deceleration * groundDecelerationMultiplier);
            }
            //calculate air deceleration
            else
            {   
                currentVelocity.X = Mathf.MoveToward(currentVelocity.X, 0, deceleration);
            }

            //Velocity = currentVelocity;
            //MoveAndSlide();
            int tempCollisionCount = GetSlideCollisionCount();
            if (tempCollisionCount != 0)
            {
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

                if (Math.Abs((Math.Abs(previousLocation.X) - Math.Abs(GlobalPosition.X) + (Math.Abs(previousLocation.Y) - Math.Abs(GlobalPosition.Y)))) <= 0.2f)
                {
                    //QueueFree();
                    //ball has come to a complete stop. disable further movement and collisions
                    currentVelocity = new Godot.Vector2(0.0f, 0.0f);
                    //GetNode<CollisionShape2D>($"CollisionShape2D").Disabled = true;
                    canActivateThings = false;
                }
                else if (previousLocation != GlobalPosition && maxBounces > 0)
                {
                    GD.Print("Bouncey ball bounce!");
                    //reflect velocity based on collision normal
                    Godot.Vector2 normal = GetSlideCollision(0).GetNormal();
                    currentVelocity = currentVelocity.Bounce(normal) * bounceyBallBounceDamping;
                    maxBounces--;
                }
                else if (previousLocation != GlobalPosition && maxBounces == 0)
                {
                    GD.Print("Standard bounce!");
                    Godot.Vector2 normal = GetSlideCollision(0).GetNormal();
                    currentVelocity = currentVelocity.Bounce(normal) * standardBounceDamping;
                    SoundManager.instance.playPossitionalAudio("bulletImpact", GlobalPosition.X, GlobalPosition.Y);
                }
                
                GD.Print("Current Velocity after bounce: " + currentVelocity.ToString());
                GD.Print("calculated Velocity = " + Math.Abs((Math.Abs(previousLocation.X) - Math.Abs(GlobalPosition.X) + (Math.Abs(previousLocation.Y) - Math.Abs(GlobalPosition.Y)))));
            }
            previousLocation = GlobalPosition;
            Velocity = currentVelocity;
            MoveAndSlide();
        }
        if ((timeAlive += delta) >= lifespan)
        {
            QueueFree();
        }

    }

}

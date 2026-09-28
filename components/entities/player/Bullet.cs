using System;
using Godot;

public partial class Bullet : CharacterBody2D
{
    private Godot.Vector2 currentVelocity = new(0.0f, 0.0f);
    private Godot.Vector2 previousLocation = new(0.0f, 0.0f);
    [Export]
    public short maxBounces = 3;
    [Export]
    public float deceleration = 1.0f;
    [Export]
    public float groundDecelerationMultiplier = 2.0f;
    public float BounceDamping; 
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

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        base._Ready();
        GlobalPosition = spawn;
    }

    public void Setup(Godot.Vector2 incomingSpawn, Godot.Vector2 incomingDirectionalVelocity, float incomingBulletSpeed = 200.0f, float incomingBounceDamping = 0.4f)
    {
        spawn = incomingSpawn;
        currentVelocity = incomingDirectionalVelocity.Normalized() * incomingBulletSpeed;
        BounceDamping = incomingBounceDamping;
        timeAlive = 0.0d;
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
                currentVelocity.X = Mathf.MoveToward(currentVelocity.X, 0, deceleration * groundDecelerationMultiplier * 60f * (float)delta);
            }

            Velocity = currentVelocity;
            MoveAndSlide();
            int tempCollisionCount = GetSlideCollisionCount();
            if (tempCollisionCount != 0)
            {
                for (int i = 0; i < tempCollisionCount; i++)
                {
                    KinematicCollision2D tmp = GetSlideCollision(i);
                    if (tmp.GetCollider() is Node2D collider)
                    {
                        string colliderName = collider.Name;
                        if (colliderName.Contains("Enemy"))
                            MessageManager.instance.StunEnemyWithID(tmp.GetColliderId());
                        else if (colliderName.Contains("Button"))
                            MessageManager.instance.ActivateInteractableWithID(tmp.GetColliderId());
                    }
                }

                if (previousLocation.DistanceTo(GlobalPosition) <= 0.2f)                
                {
                    //ball has come to a complete stop. disable further movement and collisions
                    currentVelocity = new Godot.Vector2(0.0f, 0.0f);
                    canActivateThings = false;
                }
                else
                {
                    Godot.Vector2 normal = GetSlideCollision(0).GetNormal();
                    currentVelocity = currentVelocity.Bounce(normal) * BounceDamping;
                    SoundManager.instance.playPossitionalAudio("bulletImpact", GlobalPosition.X, GlobalPosition.Y);
                }
                
            }
            previousLocation = GlobalPosition;

        }
        if ((timeAlive += delta) >= lifespan)
        {
            QueueFree();
        }

    }

}

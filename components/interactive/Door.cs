using Godot;

public partial class Door : iButtonActivation
{

    private Sprite2D doorSprite;
    private StaticBody2D doorCollision;
    public override void _Ready()
    {
        doorSprite = GetNode<Sprite2D>($"Sprite2D");
        doorCollision = GetNode<StaticBody2D>($"StaticBody2D");

        //GD.Print("door collision is: " + doorCollision);
    }
    public override bool Activate()
    {
        doorSprite.Visible = !doorSprite.Visible;
        if (doorSprite.Visible == true)
            doorCollision.CollisionLayer = 9;
        else
            doorCollision.CollisionLayer = 0;
        return true;
    }

    public override int feedback()
    {
        return 0;
    }
}

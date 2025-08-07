using Godot;

public partial class Button : CharacterBody2D
{
    [Export]
    public iButtonActivation buttonLinksTo { get; set; }
    [Export]
    public double reactivationDelay { get; set; } = 5.0d;
    private Sprite2D buttonSprite;
    private bool canBeActivated = true;
    private double reactivationTimer = 0;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        buttonSprite = GetNode<Sprite2D>($"buttonDefault");
        MessageManager.instance.addToInteractableDictionary(this);

    }
    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public bool Activate()
    {
        if (canBeActivated)
        {
            buttonSprite.Visible = false;
            canBeActivated = false;
            reactivationTimer = reactivationDelay;
            if (buttonLinksTo != null)
            {
                buttonLinksTo.Activate();
            }
        }
        return true;
    }
    public override void _Process(double delta)
    {
        base._Process(delta);
        if (!canBeActivated)
        {
            reactivationTimer -= delta;
            if (reactivationTimer <= 0.0d)
            {
                buttonSprite.Visible = true;
                canBeActivated = true;
            }
        }
    }
    public int feedback()
    {
        return 0;
    }

}

using Godot;
using System.Collections.Generic;

public partial class UIControl : Control
{
    private List<TextureRect> HealthPipFills;
    public override void _Ready()
    {

        MessageManager.instance.addUIControlToMessageManager(this);

        HealthPipFills = new List<TextureRect>();
        //health pips must be added in order.
        HealthPipFills.Add(GetNode<TextureRect>($"HealthPipOne/HealthPipOneFill"));
        HealthPipFills.Add(GetNode<TextureRect>($"HealthPipTwo/HealthPipTwoFill"));
        HealthPipFills.Add(GetNode<TextureRect>($"HealthPipThree/HealthPipThreeFill"));
        MessageManager.instance.enquireCurrentHealthofPlayer();

    }

    public void setHealthTo(int incomingHealth)
    {
        GD.Print("HealthPipFill Count: " + HealthPipFills.Count);
        for (int x = 0; x < HealthPipFills.Count; x++)
        {
            if (x < incomingHealth)
            {
                //if health is higher in the list than current health, enforce the visibility of the pip
                HealthPipFills[x].Visible = true;
            }
            else
            {
                //if health is higher in the list than current health, remove the visibility of the pip
                HealthPipFills[x].Visible = false;
            }
        }
    }
}

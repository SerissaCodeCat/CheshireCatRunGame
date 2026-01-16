using Godot;
using System.Collections.Generic;

public partial class UIControl : Control
{
    private List<TextureRect> HealthPipFills;
    private TextureRect EnergyBarFill;
    private const float EnergyBarHeight = 12.0f;
    private const float EnergyBarMaxLength = 130.0f;
    private Vector2 tmp = new Vector2(EnergyBarHeight, EnergyBarMaxLength);
    public override void _Ready()
    {

        MessageManager.instance.addUIControlToMessageManager(this);

        HealthPipFills = new List<TextureRect>();
        //health pips must be added in order.
        HealthPipFills.Add(GetNode<TextureRect>($"HealthPipOne/HealthPipOneFill"));
        HealthPipFills.Add(GetNode<TextureRect>($"HealthPipTwo/HealthPipTwoFill"));
        HealthPipFills.Add(GetNode<TextureRect>($"HealthPipThree/HealthPipThreeFill"));
        MessageManager.instance.enquireCurrentHealthofPlayer();

        EnergyBarFill = GetNode<TextureRect>($"EnergyBarFill");
        setEnergyPercentageTo(MessageManager.instance.GetbulletTimePercentageOfPlayer());
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
    public void setEnergyPercentageTo(double incomingPercentageDecimal)
    {
        if (incomingPercentageDecimal != 0.0d)
        {
            tmp = new Vector2(EnergyBarHeight, EnergyBarMaxLength - (float)(EnergyBarMaxLength * (incomingPercentageDecimal)));
        }
        else
        {
            tmp = new Vector2(EnergyBarHeight, EnergyBarMaxLength);
        }
        EnergyBarFill.SetSize(tmp);
    }
}

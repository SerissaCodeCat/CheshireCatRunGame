using Godot;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Timers;

public partial class UIControl : Control
{
    private List<TextureRect> HealthPipFills;
    private TextureRect EnergyBarFill;
    private const float EnergyBarHeight = 12.0f;
    private const float EnergyBarMaxLength = 130.0f;
    private Vector2 tmp = new Vector2(EnergyBarHeight, EnergyBarMaxLength);
    private int currentHealth = 3;
    private int currentMaxHealth = 3;
    private int currentLifeCount = 3;
    public override void _Ready()
    {

        MessageManager.instance.AddUIControlToMessageManager(this);

        HealthPipFills =
        [
            //health pips must be added in order.
            GetNode<TextureRect>($"HealthPipOne/HealthPipOneFill"),
            GetNode<TextureRect>($"HealthPipTwo/HealthPipTwoFill"),
            GetNode<TextureRect>($"HealthPipThree/HealthPipThreeFill"),
        ];

        EnergyBarFill = GetNode<TextureRect>($"EnergyBarFill");
        SetEnergyPercentageTo(MessageManager.instance.GetbulletTimePercentageOfPlayer());
        SetHealthTo(currentHealth);
    }

    public Godot.Collections.Dictionary<string, Variant> Save()
    {
        //turn values to dictionary, SaveManager will turn this to Json
        return new Godot.Collections.Dictionary<string, Variant>()
        {
                { "componentName", this.Name },
                { "currentHealth", currentHealth},
                { "currentMaxHealth", currentMaxHealth},
                { "currentLifeCount", currentLifeCount}
        };
    } 
    public void SetHealthTo(int incomingHealth)
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
    public void SetEnergyPercentageTo(double incomingPercentageDecimal)
    {
        if (incomingPercentageDecimal != 0.0d)
        {
            tmp = new Vector2(EnergyBarMaxLength - (float)(EnergyBarMaxLength * incomingPercentageDecimal), EnergyBarHeight);
        }
        else
        {
            tmp = new Vector2(EnergyBarMaxLength, EnergyBarHeight);
        }
        EnergyBarFill.SetSize(tmp, true);
    }
    public void IncreaseCurrentHealthBy(int healthUp = 1)//if no specified Heal, basic Healing is 1.
    {
        currentHealth += healthUp;
        if(currentHealth > currentMaxHealth)
        {
            currentHealth = currentMaxHealth;
        }
        SetHealthTo(currentHealth);
    }
    public void DecreaseCurrentHealthBy(int healthDown = 1)//if no specified damage, basic damage is 1.
    {
        currentHealth -= healthDown;
        if (currentHealth > 0)
        {
            SetHealthTo(currentHealth);
        }
        else
        {
            DecrementLifeCount();
            if (currentLifeCount >= 0)
            {
                MessageManager.instance.ResetPlayerToSpawnPosition();
                currentHealth = currentMaxHealth;
            }

        }

    }
    public void KillPlayer()
    {
        DecreaseCurrentHealthBy(currentMaxHealth);
    }
    public void DecrementLifeCount()
    {
        currentLifeCount --;
        if (currentLifeCount < 0 )
        {
            GD.Print("PLAYERR IS DEAD DEAD!");
            //TODO Game Over Screen
        }
    }
    public void IncrementLifeCount()
    {
        currentLifeCount ++;
    }

}

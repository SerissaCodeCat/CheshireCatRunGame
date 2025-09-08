using Godot;
using System;
using System.Collections.Generic;

public partial class MessageManager : Node2D
{
    public static MessageManager instance { get; private set; }
    private Dictionary<ulong, EnemyPatrol> enemies = new System.Collections.Generic.Dictionary<ulong, EnemyPatrol>();
    private Dictionary<ulong, Button> interactables = new System.Collections.Generic.Dictionary<ulong, Button>();
    private PlayerCharacter playerMessagerLink = null;
    private PixelPerfectCamera cameraLink = null;
    private SubViewportContainer viewportLink = null;
    private UIControl UIControlLink = null;


    public override void _Ready()
    {
        instance = this; //ensure that this is the ONLY message manager
    }

    //set up the Dictionaries for messages to be handled by the manager
    public void addToEnemyDictionary(EnemyPatrol enemyInstance)
    {
        enemies.Add(enemyInstance.GetInstanceId(), enemyInstance);
        GD.Print("added Enemy with ID of: " + enemyInstance.GetInstanceId());
    }
    public void addToInteractableDictionary(Button buttonInstance)
    {
        interactables.Add(buttonInstance.GetInstanceId(), buttonInstance);
        GD.Print("added Interactable with ID of: " + buttonInstance.GetInstanceId());
    }
    public void addCameraToMessager(PixelPerfectCamera Camera)
    {
        if (cameraLink == null)
        {
            cameraLink = Camera;
        }
    }

    //part of the pixel perfect camera sytem
    public void addViewportToMessager(SubViewportContainer incomingViewport)
    {
        viewportLink = incomingViewport;
    }

    //used to transition player values between scenes
    public void addPlayerToMessageManager(PlayerCharacter player)
    {
        if (playerMessagerLink == null)
        {
            playerMessagerLink = player;
            player.setValues(3);

        }
        else
        {
            player.setValues(playerMessagerLink.Health);
            playerMessagerLink = player;
        }
        GD.Print("player added to message manager with IDvalue of: " + playerMessagerLink.GetInstanceId());
    }
    public void addUIControlToMessageManager(UIControl incomingUIControl)
    {
        if (UIControlLink == null)
        {
            UIControlLink = incomingUIControl;
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    //////////////// MESSGES TO PLAYER Node ////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    //damage the player, and note where the damage came from so that player node can react physically to the damage.
    public void DamagePlayer(Godot.Vector2 damageComingFrom, int damage = 1)
    {
        //GD.Print("Sending damage message to player");
        playerMessagerLink.DamagePLayer(damageComingFrom.X, damageComingFrom.Y);
    }
    //damage without the bounce. usefull for environmental damage like gas / steam / heat  ect.
    public void sendPlayerNonePhysicalDamage(int damage = 1)
    {
        throw NotImplementedException();
    }
    public int enquireCurrentHealthofPlayer()
    {
        GD.Print("test:");
        return playerMessagerLink.enquireCurrentHealth();
    }
    public double GetbulletTimePercentageOfPlayer()
    {
        return playerMessagerLink.GetbulletTimePercentageDecimal();
    }
    public void sendNewHealthTotalToUI(int currentHealth)
    {
        GD.Print("setHealth");
        UIControlLink.setHealthTo(currentHealth);
    }
    public void sendEnegyPercentageTotalToUI(double incomingPercentage)
    {
        UIControlLink.setEnergyPercentageTo(incomingPercentage);
    }
    ////////////////////////////////////////////////////////////////////////////
    ///////////////////// MESSAGES TO NPSs /////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////
    public void stunEnemyWithID(ulong ID)
    {
        //GD.Print("stunn sent to ID: " + ID);
        enemies[ID].BeStunned();
    }


    ///////////////////////////////////////////////////////////////////////////
    //// MESSAGES TO INTERACTABLE ELEMENTS ////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////
    public void activateInteractableWithID(ulong ID)
    {
        interactables[ID].Activate();
    }

    ////////////////////////////////////////////////////////////////////////////
    /////////////////MESSAGES TO ALL ENTITIES //////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////
    public void pauseEntities()
    {
        throw NotImplementedException();
    }
    public void unpauseEntites()
    {
        throw NotImplementedException();
    }

    ////////////////////////////////////////////////////////////////////////////
    /////////////////MESSAGES TO CAMERA ////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    //camera will smoothly follow target.
    void setPlayerAsCameraTarget()
    {
        cameraLink.SetCameraTarget(playerMessagerLink);
    }
    //useful for zooming the camera to a point of interest
    public void setCameraTarget(Node2D target)
    {
        cameraLink.SetCameraTarget(target);
    }
    //move camera instantly to location. no smoothing.
    public void SetCameraPosition(Vector2 incomingPosition)
    {
        cameraLink.moveToPosition(incomingPosition);
    }
    //set player as follow target, and set starting possition to player's location.
    public void SetCameraStartPosition()
    {
        cameraLink.moveToPosition(playerMessagerLink.Position);
        cameraLink.SetCameraTarget(playerMessagerLink);
    }

    ///////////////////////////////////////////////////////////////////////////
    ///////////////// SHADER UPDATES //////////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////
    public void updateViewportWholePixelOnlyMovement(Vector2 incomingPosition)
    {
        viewportLink.ViewportWholePixelOnlyMovement(incomingPosition);
    }


    ///////////////////////////////////////////////////////////////////////////
    ///////// DEBUGGING AND CRASH HANDLERS ////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////
    private Exception NotImplementedException()
    {
        throw new NotImplementedException();
    }
    private Exception PlayerNotFoundException()
    {
        throw new NotImplementedException();
    }
}

using Godot;
using System;
using System.Collections.Generic;

public partial class MessageManager : Node2D
{
    private enum MenuState
    {
        closed,
        pauseMenu,
        settingsMenu,
        mainMenu,
    }
    private MenuState currentMenuState;
    public static MessageManager instance { get; private set; }
    private Dictionary<ulong, EnemyPatrol> enemies = new System.Collections.Generic.Dictionary<ulong, EnemyPatrol>();
    private Dictionary<ulong, Button> interactables = new System.Collections.Generic.Dictionary<ulong, Button>();
    private PlayerCharacter playerMessagerLink = null;
    private PixelPerfectCamera cameraLink = null;
    private SubViewportContainer viewportLink = null;
    private UIControl UIControlLink = null;
    private SettingsMenu SettingsMenuLink = null;
    private PauseMenu pauseMenuLink = null;

    public override void _Ready()
    {
        instance = this; //ensure that this is the ONLY message manager
        this.ProcessMode = ProcessModeEnum.Always; //set the message manager to ALWAYS be active
    }
    //set up the Dictionaries and links for messages to be handled by the manager
    public void addToEnemyDictionary(EnemyPatrol enemyInstance)
    {
        enemies.Add(enemyInstance.GetInstanceId(), enemyInstance);
        GD.Print("added Enemy with ID of: " + enemyInstance.GetInstanceId());
    }
    public void removeFromEnemyDictionary(ulong enemyInstanceID)
    {
        enemies.Remove(enemyInstanceID);
        GD.Print("Removed Enemy with ID of: " + enemyInstanceID);
    }
    public void addToInteractableDictionary(Button buttonInstance)
    {
        interactables.Add(buttonInstance.GetInstanceId(), buttonInstance);
        GD.Print("added Interactable with ID of: " + buttonInstance.GetInstanceId());
    }
    public void removeFromInteractableDictionary(ulong buttonInstanceID)
    {
        interactables.Remove(buttonInstanceID);
        GD.Print("Removed interactable with ID of: " + buttonInstanceID);
    }
    public void addCameraToMessager(PixelPerfectCamera Camera)
    {
        if (cameraLink == null)
        {
            cameraLink = Camera;
        }
    }
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
    public void flushLevelData()
    {
        playerMessagerLink = null;
        cameraLink = null;
        foreach (var x in interactables.Keys)
        {
            removeFromInteractableDictionary(x);
        }
        foreach (var y in enemies.Keys)
        {
            removeFromEnemyDictionary(y);
        }
        enemies = new Dictionary<ulong, EnemyPatrol>();
    }
    public void addViewportToMessager(SubViewportContainer incomingViewport)
    {
        viewportLink = incomingViewport;
    }
    public void addUIControlToMessageManager(UIControl incomingUIControl)
    {
        if (UIControlLink == null)
        {
            UIControlLink = incomingUIControl;
        }
    }
    public void addPauseMenuToMessageManager(PauseMenu incomingPauseMenu)
    {
        if(pauseMenuLink == null)
        {
            pauseMenuLink = incomingPauseMenu;
        }
    }
    public void addSettingsMenuToMessageManager(SettingsMenu incomingSettingsMenu)
    {
        if (SettingsMenuLink == null)
        {
            SettingsMenuLink = incomingSettingsMenu;
        }
    }

    /// <summary>
    /// ////////////////////////////////////////////////////////////////////////
    /// //////////// MESSAGES TO MENU SYSTEM ///////////////////////////////////
    /// ////////////////////////////////////////////////////////////////////////
    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("Escape"))
        {
            menuNavigationOnEscapeOrBack();
        }
    }

    public void menuNavigationOnEscapeOrBack()
    {
        if(currentMenuState == MenuState.closed)
            {
                if(pauseMenuLink != null)
                {
                    GetTree().Paused = true;
                    pauseMenuLink.Pause();
                    currentMenuState = MenuState.pauseMenu;
                    GD.Print("menu state set to PAUSE");
                }
            }
        else if(currentMenuState == MenuState.pauseMenu)
        {
            if (pauseMenuLink != null)
            {
                pauseMenuLink.Resume();
                GetTree().Paused = false;
                currentMenuState = MenuState.closed;
                GD.Print("menu state set to CLOSED");
            }
        }
        else if(currentMenuState == MenuState.settingsMenu)
        {
            if(SettingsMenuLink != null)
            {
                SettingsMenuLink.goBackToPauseMenu();
                currentMenuState = MenuState.pauseMenu;
                GD.Print("menu state set to PAUSE");
            }
        }
    } 
    
    ////////////////////////////////////////////////////////////////////////////
    //////////////// MESSGES TO PLAYER Node ////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    //damage the player, and note where the damage came from so that player node can react physically to the damage.
    public void DamagePlayer(Godot.Vector2 damageComingFrom, int damage = 1)
    {
        playerMessagerLink.DamagePLayer(damageComingFrom.X, damageComingFrom.Y, damage);
    }
    public void KillPlayer()
    {
        GD.Print("Message Manager telling player to die");
        playerMessagerLink.KillPlayer();
    }
    //damage without the bounce. usefull for environmental damage like gas / steam / heat  ect.
    public void sendPlayerNonePhysicalDamage(int damage = 1)
    {
        throw NotImplementedException();
    }
    public int enquireCurrentHealthofPlayer()
    {
        return playerMessagerLink.enquireCurrentHealth();
    }
    public double GetbulletTimePercentageOfPlayer()
    {
        return playerMessagerLink.GetbulletTimePercentageDecimal();
    }
    public void sendNewHealthTotalToUI(int currentHealth)
    {
        UIControlLink.setHealthTo(currentHealth);
    }
    public void sendEnegyPercentageTotalToUI(double incomingPercentage)
    {
        UIControlLink.setEnergyPercentageTo(incomingPercentage);
    }
    public void setPlayerSpawnPosition(Vector2 incomingPosition)
    {
        playerMessagerLink.SetSpawnPosition(incomingPosition);
    }
    public void resetPlayerToSpawnPosition()
    {
        playerMessagerLink.ResetPlayerToSpawnPosition();
    }
    ////////////////////////////////////////////////////////////////////////////
    ///////////////////// MESSAGES TO NPSs /////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////
    public void stunEnemyWithID(ulong ID)
    {
        enemies[ID].SwitchToStunState();
    }
    ///////////////////////////////////////////////////////////////////////////
    //// MESSAGES TO INTERACTABLE ELEMENTS ////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////
    public void activateInteractableWithID(ulong ID)
    {
        interactables[ID].Activate();
    }
    ////////////////////////////////////////////////////////////////////////////
    /////////////////MESSAGES TO CAMERA ////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    //camera will smoothly follow target.
    public void setPlayerAsCameraTarget()
    {
        if (playerMessagerLink != null)
            cameraLink.SetCameraTarget(playerMessagerLink);
        else
            GD.Print("No player character to link camera to!");
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
    public Vector2 GetCameraCurrentPosition()
    {
        return cameraLink.GlobalPosition;
    }

    ///////////////////////////////////////////////////////////////////////////
    ///////////////// SHADER UPDATES //////////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////
    public void updateViewportWholePixelOnlyMovement(Vector2 incomingPosition)
    {
        viewportLink.ViewportWholePixelOnlyMovement(incomingPosition);
    }
    ///////////////////////////////////////////////////////////////////////////
    ///////////////// MESSAGES TO Viewport ////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////
    public void ShowSettingsPage()
    {
        if(SettingsMenuLink != null)
        {
            SettingsMenuLink.showAndEnableMenu();
            currentMenuState = MenuState.settingsMenu;
            GD.Print("menu state set to SETTINGS");
        }
    }
    public void HideSettingsPage()
    {
        if(SettingsMenuLink != null)
        {
            SettingsMenuLink.hideAndDisableMenu();
            currentMenuState = MenuState.pauseMenu;
            GD.Print("menu state set to PAUSE");
        }
    }
    public void ShowPauseMenu()
    {
        if(pauseMenuLink != null)
        {
            pauseMenuLink.show();
            currentMenuState = MenuState.pauseMenu;
            GD.Print("menu state set to PAUSE");
        }
    }
    public void LoadLevelWithPath(String levelPath)
    {
        //pause all physics based processes!
        GetTree().Paused = true;
        
        GD.Print("Freeing message manager lists & identifiers");
        flushLevelData();
        GD.Print("LEVEL TO LOAD = "+levelPath);
        viewportLink.SetNextLevelPath(levelPath);
        viewportLink.LoadLevel();
    }

    public void SetViewportResolution(int incomingX = 1920, int incomingY = 1080)
    {
        viewportLink.setResolution(incomingX, incomingY);
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

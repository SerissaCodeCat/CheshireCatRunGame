using Godot;
using System;
using System.Collections.Generic;

public partial class MessageManager : Node2D
{
	public static MessageManager instance {get; private set;}
	private Dictionary<ulong, EnemyPatrol> enemies = new System.Collections.Generic.Dictionary<ulong, EnemyPatrol>();
	private Dictionary<ulong, Button> interactables = new System.Collections.Generic.Dictionary<ulong, Button>();
	private PlayerCharacter playerMessagerLink = null;

	// Called when the node enters the scene tree for the first time.
	//intiate the singleton to be used throughout the game session.
	public override void _Ready()
	{
		instance = this;
	}

	//set up the Dictionaries for messages to the manager
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


	//used to transition player values between scenes
	public void addPlayerToMessageManager(PlayerCharacter player)
	{
		if (playerMessagerLink == null)
		{
			playerMessagerLink = player;
			player.setValues(playerMessagerLink);

		}
		else
		{
			player.setValues(playerMessagerLink);
			playerMessagerLink = player;
		}
		GD.Print("player added to message manager with IDvalue of: " + playerMessagerLink.GetInstanceId());
	}

	public void DamagePlayer(Godot.Vector2 damageComingFrom)
	{
		//GD.Print("Sending damage message to player");
		playerMessagerLink.DamagePLayer(damageComingFrom.X, damageComingFrom.Y);
	}
	public void stunEnemyWithID (ulong ID)
	{
		//GD.Print("stunn sent to ID: " + ID);
		enemies[ID].BeStunned();
	}
	public void activateInteractableWithID(ulong ID)
	{
		interactables[ID].Activate();
	}
	public void sendPlayerDamaged()
	{
		throw NotImplementedException();
	}

    private Exception NotImplementedException()
    {
        throw new NotImplementedException();
    }
	private Exception PlayerNotFoundException()
    {
        throw new NotImplementedException();
    }
}

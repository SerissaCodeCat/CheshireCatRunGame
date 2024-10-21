using Godot;
using System;
using System.Collections.Generic;

public partial class MessageManager : Node2D
{
	public static MessageManager instance {get; private set;}
	private Dictionary<ulong, EnemyPatrol> enemies = new System.Collections.Generic.Dictionary<ulong, EnemyPatrol>();
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		instance = this;
		GD.Print("Loaded Messaage Manager");
	}

	public void addToEnemyDictionary(EnemyPatrol enemyInstance)
	{
		enemies.Add(enemyInstance.GetInstanceId(), enemyInstance);
		GD.Print("added Enemy with ID of: " + enemyInstance.GetInstanceId());
	}

	public void stunEnemyWithID (ulong ID)
	{
		GD.Print("stunn sent to ID: " + ID);
		enemies[ID].BeStunned();
	}
	public void SendPlayerDamaged()
	{
		throw NotImplementedException();
		throw PlayerNotFoundException();
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

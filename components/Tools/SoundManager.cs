using Godot;
using System.Collections.Generic;

public partial class SoundManager : Node2D
{
	//set up a singleton so that everything can access this by quick refferance.
	public static SoundManager instance { get; private set; }
	/*Pools are used to create a variety of noises for the same incident. 
	 IE a variation in the sound of a slingshot bullet hitting something to reduce player fatigue with the same audio
	 or different potential dialogue quips for character in a situation.
	*/
	private Dictionary<string, SoundPool2d> SoundEffectPoolByName = new Dictionary<string, SoundPool2d>();

	//dialogue for varied quips has it's own pool to allow the audio to be adjusted seperatly to sound effects, music and significant dialogue
	private Dictionary<string, SoundPool2d> QuipsPoolByName = new Dictionary<string, SoundPool2d>();
	//Significant dialogue is stored by name only, and does not have randomly accessable varients.
	private Dictionary<string, AudioStreamPlayer> DialogueByName = new Dictionary<string, AudioStreamPlayer>();
	private Dictionary<string, AudioStreamPlayer> musicByName = new Dictionary<string, AudioStreamPlayer>();

	int MasterBusIndex = AudioServer.GetBusIndex("Master");
	int SoundEffectsBusIndex = AudioServer.GetBusIndex("SoundEffects");
	int QuipsBusIndex = AudioServer.GetBusIndex("Quips");
	int DialogueBusIndex = AudioServer.GetBusIndex("Dialogue");
	int MusicBusIndex = AudioServer.GetBusIndex("Music");
	public override void _Ready()
	{
		instance = this;

		//add sound effect pools here
		SoundEffectPoolByName.Add("bulletImpact", GetNode<SoundPool2d>("bulletImpactSoundPool"));

		//add quips sound pool here
		//QuipsPoolByName.Add("exampleDialogueTitle", GetNode<SoundPool2d>("exampleCharacterEventPoolName"));

		//add significant dialogue here
		//musicByName.Add("testMusic", GetNode<AudioStreamPlayer>("AudioStreamPlayerTestMusic"));
		//add music here
		musicByName.Add("testMusic", GetNode<AudioStreamPlayer>("AudioStreamPlayerTestMusic"));
	}

	public void playPossitionalAudio(string name, float x, float y)
	{
		if(SoundEffectPoolByName.ContainsKey(name))
		{
			SoundEffectPoolByName[name].playAtLocation(x,y);
			GD.Print("Playing: " + name );
		}
		else
		{
			GD.Print("WARNING: " + name + " not found in list of possitional Audio");
		}
	}
	public void playMusicByName(string name)
	{
		if(musicByName.ContainsKey(name))
		{
			musicByName[name].Play();
		}
		else
		{
			GD.Print("WARNING: " + name + "Not found in list of music");
		}
	}
	public void stopAllMusic()
	{
		foreach(var x in musicByName.Values)
		{
			if(x.Playing == true)
			x.Stop();
		}
	}
	public void stopMusicByName(string name)
	{
		if(musicByName.ContainsKey(name))
		{
			musicByName[name].Stop();
		}
		else
		{
			GD.Print("WARNING: " + name + "Not found in list of music");
		}
	}

	public void changeMasterVolume(double incomingValue)
	{
		AudioServer.SetBusVolumeDb(MasterBusIndex, (float)Mathf.LinearToDb(incomingValue));
		GD.Print("Master Volume set to: " + incomingValue);
	}
	public double getMasterVolume()
	{
		return AudioServer.GetBusVolumeLinear(MasterBusIndex);
	}
	public void changeMusicVolume(double incomingValue)
	{
		AudioServer.SetBusVolumeDb(MusicBusIndex, (float)Mathf.LinearToDb(incomingValue));
		GD.Print("Music Volume set to: " + incomingValue);
	}
	public double getMusicVolume()
	{
		return AudioServer.GetBusVolumeLinear(MusicBusIndex);
	}
	public void changeSoundEffectsVolume(double incomingValue)
	{
		AudioServer.SetBusVolumeDb(SoundEffectsBusIndex, (float)Mathf.LinearToDb(incomingValue));
		GD.Print("SoundFX Volume set to: " + incomingValue);
	}
	public double getSoundEffectsVolume()
	{
		return AudioServer.GetBusVolumeLinear(SoundEffectsBusIndex);
	}
	public void changeDialogueVolume(double incomingValue)
	{
		AudioServer.SetBusVolumeDb(DialogueBusIndex, (float)Mathf.LinearToDb(incomingValue));
		GD.Print("Dialogue Volume set to: " + incomingValue);
	}
	public double getDialogueVolume()
	{
		return AudioServer.GetBusVolumeLinear(DialogueBusIndex);
	}
	public void changeQuipsVolume(double incomingValue)
	{
		AudioServer.SetBusVolumeDb(QuipsBusIndex, (float)Mathf.LinearToDb(incomingValue));
		GD.Print("Quips Volume set to: " + incomingValue);
	}
	public double getQuipVolume()
	{
		return AudioServer.GetBusVolumeLinear(QuipsBusIndex);
	}

}

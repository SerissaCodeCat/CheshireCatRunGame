using Godot;
using System.Collections.Generic;

public partial class SoundManager : Node2D
{
	public static SoundManager instance { get; private set; }	// Called when the node enters the scene tree for the first time.
	private Dictionary<string, SoundPool2d> SoundEffectPoolByName = new Dictionary<string, SoundPool2d>();
	private Dictionary<string, AudioStreamPlayer> DialoguePoolByName = new Dictionary<string, AudioStreamPlayer>();
	private Dictionary<string, AudioStreamPlayer> musicByName = new Dictionary<string, AudioStreamPlayer>();
	public override void _Ready()
	{
		instance = this;

		//add sound effect pools here
		SoundEffectPoolByName.Add("bulletImpact", GetNode<SoundPool2d>("bulletImpactSoundPool"));

		//add dialogue sound pool here
		//DialoguePoolByName.Add("exampleDialogueTitle", GetNode<SoundPool2d>("pathToDialogueLocation"));
		
		//add music here
		//musicByName.Add("ExampleSongName", GetNode<AudioStreamPlayer>("pathToMusicLocation"));
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

	int MasterBusIndex = AudioServer.GetBusIndex("Master");
	int SoundEffectsBusIndex = AudioServer.GetBusIndex("SoundEffects");
	int DialogueBusIndex = AudioServer.GetBusIndex("Dialogue");
	int MusicBusIndex = AudioServer.GetBusIndex("Music");
	public void changeMasterVolume(float incomingValue)
	{
		AudioServer.SetBusVolumeDb(MasterBusIndex, Mathf.LinearToDb(incomingValue));
	}
	public void changeMusicVolume(float incomingValue)
	{
		AudioServer.SetBusVolumeDb(MusicBusIndex, Mathf.LinearToDb(incomingValue));
	}
	public void changeSoundEffectsVolume(float incomingValue)
	{
		AudioServer.SetBusVolumeDb(SoundEffectsBusIndex, Mathf.LinearToDb(incomingValue));
	}
	public void changeDialogueVolume(float incomingValue)
	{
		AudioServer.SetBusVolumeDb(DialogueBusIndex, Mathf.LinearToDb(incomingValue));
	}

}

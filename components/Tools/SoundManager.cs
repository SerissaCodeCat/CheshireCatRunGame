using Godot;
using System.Collections.Generic;

public partial class SoundManager : Node2D
{
	public static SoundManager instance { get; private set; }	// Called when the node enters the scene tree for the first time.
	private Dictionary<string, SoundPool2d> SoundPool2dByName = new Dictionary<string, SoundPool2d>();
	private Dictionary<string, AudioStreamPlayer> musicByName = new Dictionary<string, AudioStreamPlayer>();
	public override void _Ready()
	{
		instance = this;

		//add sound pools here
		SoundPool2dByName.Add("bulletImpact", GetNode<SoundPool2d>("bulletImpactSoundPool"));

		//add music here
	}

	public void playPossitionalAudio(string name, float x, float y)
	{
		if(SoundPool2dByName.ContainsKey(name))
		{
			SoundPool2dByName[name].playAtLocation(x,y);
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

}

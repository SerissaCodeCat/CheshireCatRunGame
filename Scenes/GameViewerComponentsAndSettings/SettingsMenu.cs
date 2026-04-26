using Godot;
using System;

public partial class SettingsMenu : Control
{
	[Export]
	private HSlider master;
	[Export]
	private HSlider music;
	[Export]
	private HSlider dialogue;
	[Export]
	private HSlider soundEffects;
	[Export]
	private HSlider quips;
	public override void _Ready()
	{
		master.ValueChanged += changeMasterVolume; 
		music.ValueChanged += changeMusicVolume;
		dialogue.ValueChanged += changeDialogueVolume; 
		soundEffects.ValueChanged += changeSoundEffectsVolume;
		quips.ValueChanged += changeQuipsVolume;
	}
	private void changeMasterVolume(double incomingValue)
	{		
		SoundManager.instance.changeMasterVolume(incomingValue);
	}
	private void changeMusicVolume(double incomingValue)
	{		
		SoundManager.instance.changeMusicVolume(incomingValue);
	}
	private void changeDialogueVolume(double incomingValue)
	{		
		SoundManager.instance.changeDialogueVolume(incomingValue);
	}
	private void changeSoundEffectsVolume(double incomingValue)
	{		
		SoundManager.instance.changeSoundEffectsVolume(incomingValue);
	}
	private void changeQuipsVolume(double incomingValue)
	{		
		SoundManager.instance.changeQuipsVolume(incomingValue);
	}

}

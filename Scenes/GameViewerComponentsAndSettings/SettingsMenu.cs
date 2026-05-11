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
	[Export]
	private OptionButton resolution;
	[Export]
	private Godot.Button backButton;
	[Export]
	private AnimationPlayer AnimationPlayer;
	private enum settingsMenuAccessedFrom
	{
		pause,
		main,
	}
	public override void _Ready()
	{
		master.Editable = false;
		master.ValueChanged += changeMasterVolume; 
		master.SetValueNoSignal(SoundManager.instance.getMasterVolume());
		
		music.Editable = false;
		music.ValueChanged += changeMusicVolume;
		music.SetValueNoSignal(SoundManager.instance.getMusicVolume());
		
		dialogue.Editable = false;
		dialogue.ValueChanged += changeDialogueVolume;
		dialogue.SetValueNoSignal(SoundManager.instance.getDialogueVolume()); 
		
		soundEffects.Editable = false;
		soundEffects.ValueChanged += changeSoundEffectsVolume;
		soundEffects.SetValueNoSignal(SoundManager.instance.getSoundEffectsVolume());
		
		quips.Editable = false;
		quips.ValueChanged += changeQuipsVolume;
		quips.SetValueNoSignal(SoundManager.instance.getQuipVolume());

		resolution.Disabled = true;
		resolution.ItemSelected += resolutionChanged;

		backButton.Disabled = true;
		backButton.Pressed += goBackToPauseMenu;

		//Allow message Manager to access this settings menu
		MessageManager.instance.addSettingsMenuToMessageManager(this);
	}


	public void showAndEnableMenu()
	{
		AnimationPlayer.Play("Transition");
		master.Editable = true;
		music.Editable = true;
		dialogue.Editable = true;
		soundEffects.Editable = true;
		quips.Editable = true;
		resolution.Disabled = false;
		backButton.Disabled = false;
		master.GrabFocus();
	}
	public void hideAndDisableMenu()
	{
		AnimationPlayer.PlayBackwards("Transition");
		master.Editable = false;
		music.Editable = false;
		dialogue.Editable = false;
		soundEffects.Editable = false;
		quips.Editable = false;
		resolution.Disabled = true;
		backButton.Disabled = true;
	}
	public void goBackToPauseMenu()
	{
		hideAndDisableMenu();
		MessageManager.instance.ShowPauseMenu();
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
	private void resolutionChanged(long index)
	{
		switch (index)
		{
			case 0: DisplayServer.WindowSetSize(new Vector2I(1920,1080));
				break;
			case 1: DisplayServer.WindowSetSize(new Vector2I(1366,768));
				break;
			case 2: DisplayServer.WindowSetSize(new Vector2I(1280,720));
				break;
			case 3: DisplayServer.WindowSetSize(new Vector2I(640,360));
				break;
			default: 
				break;
		}
	}

}

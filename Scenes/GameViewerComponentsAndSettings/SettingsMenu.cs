using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;

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
	private Godot.CheckButton fullScreenCheckButton;
	[Export]
	private Godot.CheckButton borderlessCheckButton;
	[Export]
	private AnimationPlayer AnimationPlayer;
	[Export]
	private PackedScene InputMappingButton;
	[Export]
	private VBoxContainer ActionList; 
	private Dictionary<String, String> inputActions = new()
	{
		["left"] = "Move Left", 
		["right"] = "Move Right",
		["up"] = "Move Up",
		["down"] = "Move Down",
		["jump"] = "Jump",
		["teleport"] = "Dash",
		["crouch"] = "Sneak",
		["fire"] = "Slingshot",
	};
	private enum settingsMenuAccessedFrom
	{
		pause,
		main,
	}
	private bool isRemapping = false;
	private string actionToRemap = null;
	private Godot.Button remappingButton = null;
	public override void _Ready()
	{
		createActionList();
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

		fullScreenCheckButton.Disabled = true;
		fullScreenCheckButton.Pressed += EnableDisableFullScreen;
		fullScreenCheckButton.SetPressedNoSignal(DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen);

		borderlessCheckButton.Disabled = true;
		borderlessCheckButton.Pressed += EnableBorderlessWindow;
		borderlessCheckButton.SetPressedNoSignal (GetWindow().Borderless == true);

		backButton.Disabled = true;
		backButton.Pressed += goBackToPauseMenu;

		//Allow message Manager to access this settings menu
		MessageManager.instance.addSettingsMenuToMessageManager(this);
	}

    private void createActionList()
	{
		/////
		/// following tutorial at:
		/// https://www.youtube.com/watch?v=ZDPM45cHHlI
		/////
		InputMap.LoadFromProjectSettings();
		foreach(Node x in ActionList.GetChildren())
		{
			//ensures action List does not contain any children.
			x.QueueFree();
		}
		foreach(string ActionName in inputActions.Keys)
		{
			Node buttonScene = InputMappingButton.Instantiate();
			Godot.Button actualButton = (Godot.Button)buttonScene;
			Label actionLabel = buttonScene.GetNode<Label>("MarginContainer/HBoxContainer/LabelAction");
			Label inputLabel = buttonScene.GetNode<Label>("MarginContainer/HBoxContainer/LabelInput");

			actionLabel.Text = inputActions[ActionName];

			var events = InputMap.ActionGetEvents(ActionName);
			if (events.Count > 0)
			{
				inputLabel.Text = events[0].AsText().TrimSuffix(" - Physical");
			}
			else
			{
				inputLabel.Text = "...";
			}
			ActionList.AddChild(buttonScene);
			actualButton.Pressed += ()=> {onInputButtonPressed(actualButton, ActionName);};
		}
	}

    public void onInputButtonPressed(Godot.Button button, string action)
    {
        if (isRemapping == false)
		{
			isRemapping = true;
			actionToRemap = action;
			remappingButton = button;
			remappingButton.GetNode<Label>("MarginContainer/HBoxContainer/LabelInput").Text = "Listening For Key...";
		}
		//return ()=> {GD.Print("blep");};
    }

	   public override void _Input(InputEvent @event)
    {
        base._Input(@event);
		if(isRemapping)
		{
			if(@event is InputEventKey || (@event is InputEventMouseButton && @event.IsPressed()))
			{
				InputMap.ActionEraseEvents(actionToRemap);
				GD.Print("Erased actions for "+ actionToRemap);
				InputMap.ActionAddEvent(actionToRemap, @event);
				GD.Print("Added " +@event.ToString() + " as a trigger for " +actionToRemap);
				updateActionList(remappingButton, @event);

				isRemapping = false;
				actionToRemap = null;
				remappingButton = null;
			}
		}
    }

    private void updateActionList(Godot.Button button, InputEvent @event)
    {
        button.GetNode<Label>("MarginContainer/HBoxContainer/LabelInput").Text = @event.AsText().TrimSuffix(" - Physical");
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
		fullScreenCheckButton.Disabled = false;
		borderlessCheckButton.Disabled = false;
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
		fullScreenCheckButton.Disabled = true;
		borderlessCheckButton.Disabled = true;
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
					MessageManager.instance.SetViewportResolution(1920,1080);
				break;
			case 1: DisplayServer.WindowSetSize(new Vector2I(1366,768));
					MessageManager.instance.SetViewportResolution(1366,768);
				break;
			case 2: DisplayServer.WindowSetSize(new Vector2I(1280,720));
					MessageManager.instance.SetViewportResolution(1280,720);
				break;
			case 3: DisplayServer.WindowSetSize(new Vector2I(640,360));
					MessageManager.instance.SetViewportResolution(640,360);
				break;
			default: 
				break;
		}
	}
	private void EnableDisableFullScreen()
	{
		if(DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Windowed)
		{
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
			//double checks that the button is updated correctly. not nessisary but a good sanity check
			fullScreenCheckButton.SetPressedNoSignal(true);
			borderlessCheckButton.SetPressedNoSignal(false);
			var window = GetWindow();
			window.Borderless = true;
		}
		else
		{
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
			//double checks that the button is updated correctly. not nessisary but a good sanity check
			fullScreenCheckButton.SetPressedNoSignal(false);
		}

	}
	private void EnableBorderlessWindow()
	{
		var window = GetWindow();
            
		if (borderlessCheckButton.ButtonPressed == true)
		{
			//GD.Print("Borderless Activated");
			if(DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen)
			{
				//GD.Print("Detected full screen currently active");
				EnableDisableFullScreen();
			}
			if ( window.Borderless == false)
			{
				//GD.Print("window currently borderless. ACTIVATE THE BORDERS");
				window.Borderless = true;
			}
			borderlessCheckButton.SetPressedNoSignal(true);
		}
		else
		{
			if(DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen)
			{
				EnableDisableFullScreen();
			}
			if ( window.Borderless == true)
			{
				window.Borderless = false;
			}	
			borderlessCheckButton.SetPressedNoSignal(false);
		}
	}
}

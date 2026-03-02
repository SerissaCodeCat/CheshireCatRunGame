using Godot;
using System;

public partial class PauseMenu : Control 
{
	[Export]
	private Godot.Button ResumeButton;
	[Export]
	private Godot.Button OptionsButton;
	[Export]
	private Godot.Button QuitButton;
	[Export]
	private Godot.Button MainMenuButton;
	[Export]
	private AnimationPlayer AnimationPlayer;

	////////////////////////////////
	/// READY FUNCTION /////////////
	////////////////////////////////
	public override void _Ready()
	{
		ResumeButton.Pressed += resume;
		OptionsButton.Pressed += options;
		QuitButton.Pressed += quit;
		MainMenuButton.Pressed += mainMenu;

		ResumeButton.Disabled = true;
		OptionsButton.Disabled = true;
		QuitButton.Disabled = true;
		MainMenuButton.Disabled = true;
		//resume();
	}

	public override void _Process(double delta)
	{
		if(Input.IsActionJustPressed("Escape"))
		{
			if(GetTree().Paused)
			{
				resume();
			}
			else
			{
				pause();
			}
		}
	}

	//////////////////////////////////
	/// BUTTON FUNCTIONS /////////////
	//////////////////////////////////
	public void resume()
	{
		GetTree().Paused = false;
		AnimationPlayer.PlayBackwards("PauseAnimation");
		ResumeButton.Disabled = true;
		OptionsButton.Disabled = true;
		QuitButton.Disabled = true;
		MainMenuButton.Disabled = true;
	}
	public void pause()
	{
		GetTree().Paused = true;
		AnimationPlayer.Play("PauseAnimation");
		ResumeButton.Disabled = false;
		OptionsButton.Disabled = false;
		QuitButton.Disabled = false;
		MainMenuButton.Disabled = false;
	}
	public void options()
	{
		//INSERT OPTIONS MENU HERE
	}
	public void quit()
	{
		GetTree().Quit();
	}
	public void mainMenu()
	{
		//INSERT RETURN TO MAIN MENU HERE
	}
}

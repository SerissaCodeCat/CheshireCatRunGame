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
		MessageManager.instance.addPauseMenuToMessageManager(this);
		ResumeButton.Pressed += ResumePressed;
		ResumeButton.GrabFocus(); // makes the resume button be the default highlighted button
		OptionsButton.Pressed += options;
		QuitButton.Pressed += quit;
		MainMenuButton.Pressed += mainMenu;

		ResumeButton.Disabled = true;
		//ResumeButton.Modulate = new Color(1.0f, 1.0f, 1.0f, 0.0f);
		OptionsButton.Disabled = true;
		//OptionsButton.Modulate = new Color(1.0f, 1.0f, 1.0f, 0.0f);
		QuitButton.Disabled = true;
		//QuitButton.Modulate = new Color(1.0f, 1.0f, 1.0f, 0.0f);
		MainMenuButton.Disabled = true;
		//MainMenuButton.Modulate = new Color(1.0f, 1.0f, 1.0f, 0.0f);
		//resume();
	}

	//we should never need to hit the process function. this was useful for testing, but this should be initiated through the MessageManager.
	/*public override void _Process(double delta)
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
	}*/

	//////////////////////////////////
	/// BUTTON FUNCTIONS /////////////
	//////////////////////////////////
	
	private void ResumePressed()
	{
		MessageManager.instance.menuNavigationOnEscapeOrBack();
	}
	//resumes game from where we left off. 
	public void Resume()
	{
		AnimationPlayer.PlayBackwards("PauseAnimation");
		ResumeButton.Disabled = true;
		OptionsButton.Disabled = true;
		QuitButton.Disabled = true;
		MainMenuButton.Disabled = true;
	}
	public void Pause()
	{
		ResumeButton.GrabFocus(); // makes the resume button be the default highlighted button
		AnimationPlayer.Play("PauseAnimation");
		ResumeButton.Disabled = false;
		OptionsButton.Disabled = false;
		QuitButton.Disabled = false;
		MainMenuButton.Disabled = false;
	}
	public void hide()
	{
		ResumeButton.Disabled = true;
		OptionsButton.Disabled = true;
		QuitButton.Disabled = true;
		MainMenuButton.Disabled = true;
		this.Visible = false;
	}
	public void show()
	{
		ResumeButton.GrabFocus(); // makes the resume button be the default highlighted button
		ResumeButton.Disabled = false;
		OptionsButton.Disabled = false;
		QuitButton.Disabled = false;
		MainMenuButton.Disabled = false;
		this.Visible = true;
	}
	public void options()
	{
		hide();
		MessageManager.instance.ShowSettingsPage();
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

using Godot;
using System;

public partial class PauseMenu : Control 
{
	[Export]
	private Godot.Button ResumeButton;
	[Export]
	private Godot.Button SaveButton;
	[Export]
	private Godot.Button LoadButton;
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
		MessageManager.instance.AddPauseMenuToMessageManager(this);
		ResumeButton.Pressed += ResumePressed;
		ResumeButton.GrabFocus(); // makes the resume button be the default highlighted button
		SaveButton.Pressed += SaveButtonPressed;
		LoadButton.Pressed += LoadButtonPressed;
		OptionsButton.Pressed += OptionsButtonPressed;
		QuitButton.Pressed += QuitButtonPressed;
		MainMenuButton.Pressed += MainMenuButtonPressed;

		DisableButtons();
	}

    //////////////////////////////////
    /// BUTTON FUNCTIONS /////////////
    //////////////////////////////////

    private void ResumePressed()
	{
		MessageManager.instance.MenuNavigationOnEscapeOrBack();
	}
	//resumes game from where we left off. 
	public void Resume()
	{
		AnimationPlayer.PlayBackwards("PauseAnimation");
		DisableButtons();
	}
	public void Pause()
	{
		ResumeButton.GrabFocus(); // makes the resume button be the default highlighted button
		AnimationPlayer.Play("PauseAnimation");
		EnableButtons();
	}
	public void HidePauseMenu()
	{
		DisableButtons();
		this.Visible = false;
	}
	public void ShowPauseMenu()
	{
		ResumeButton.GrabFocus(); // makes the resume button be the default highlighted button
		EnableButtons();
		this.Visible = true;
	}
	public void SaveButtonPressed()
	{
		Savemanager.instance.SaveGame();
	}
	private void LoadButtonPressed()
    {
		Savemanager.instance.LoadGame();
	}
	public void OptionsButtonPressed()
	{
		HidePauseMenu();
		MessageManager.instance.ShowSettingsPage();
	}
	public void QuitButtonPressed()
	{
		GetTree().Quit();
	}
	public void MainMenuButtonPressed()
	{
		//INSERT RETURN TO MAIN MENU HERE
	}
	private void DisableButtons()
	{
		ResumeButton.Disabled = true;
		SaveButton.Disabled = true;
		LoadButton.Disabled = true;
		OptionsButton.Disabled = true;
		QuitButton.Disabled = true;
		MainMenuButton.Disabled = true;
	}
	private void EnableButtons()
	{
		ResumeButton.Disabled = false;
		SaveButton.Disabled = false;
		LoadButton.Disabled = false;
		OptionsButton.Disabled = false;
		QuitButton.Disabled = false;
		MainMenuButton.Disabled = false;
	}
}

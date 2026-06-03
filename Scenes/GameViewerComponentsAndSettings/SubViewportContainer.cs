using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public partial class SubViewportContainer : Godot.SubViewportContainer
{
	public static SubViewportContainer instance {get; private set;}
	private Node currentLevel = null;
	[Export]
	public SubViewport viewportLink {get; set;} = null;
	[Export]
	/*
	starting level path exposed to the editor for easy setup
	of the initial level to be loaded by the main scene.
	*/
	public String startingLevelPath {get; set;} = null;

	/*
	system filepath to the next level to be loaded, 
	should be set by individual level doors to allow 
	level-skips through the MessageManager
	if needed / desired.
	*/
	public String nextLevelPath {get; set;} = null;

	private ShaderMaterial material = null;
	private String levelToLoad = null;

	public override void _Ready()
	{
		instance = this;
		if (startingLevelPath == null)
		{
			startingLevelPath = "res://Scenes/TestLevel.tscn";
		}
		MessageManager.instance.addViewportToMessager(instance);
		material = (ShaderMaterial)this.Material;
		viewportLink = this.GetChild<SubViewport>(0);
		LoadLevel();

	}
	public void ViewportWholePixelOnlyMovement(Vector2 incomingUpdate)
	{
		material.SetShaderParameter("camera_offset", incomingUpdate);
	}
	public void SetNextLevelPath(String incomingPath)
	{
		nextLevelPath = incomingPath;
		GD.Print("Setting level path to load to:" + nextLevelPath );
	}
	public void LoadLevel()
	{
		GD.Print("current level = " + currentLevel);
		if (currentLevel != null)
		{
			GD.Print("Freeing current level...");
			currentLevel.QueueFree();
		}
		if (nextLevelPath != null)
		{
			levelToLoad = nextLevelPath;
			nextLevelPath = null;
		}
		else
		{
			levelToLoad = startingLevelPath;
		}
		PackedScene levelScene = GD.Load<PackedScene>(levelToLoad);
		currentLevel = levelScene.Instantiate();
		viewportLink.AddChild(currentLevel);
		GD.Print("Level loaded successfully.");
		MessageManager.instance.setPlayerAsCameraTarget();
	}
	public void setResolution(int incomingX = 1920, int incomingY = 1080)
	{
		this.GetParent<Control>().SetSize(new Vector2(incomingX, incomingY));
		if (incomingX == 1920 && incomingY ==1080)
		{
			this.StretchShrink = 3;
		}
		if (incomingX == 1280 && incomingY ==720)
		{
			this.StretchShrink = 2;
		}
		if (incomingX == 640 && incomingY ==360)
		{
			this.StretchShrink = 1;
		}
		GetTree().Root.ContentScaleSize = new Vector2I(incomingX, incomingY);
	}
}

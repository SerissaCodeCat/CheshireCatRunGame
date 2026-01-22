using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public partial class SubViewportContainer : Godot.SubViewportContainer
{
	private Node currentLevel = null;
	[Export]
	public SubViewport viewportLink {get; set;} = null;
	[Export]
	/*
	starting level path exposed to the editor for easy setup
	of the initial level to be loaded by the main scene.
	*/
	public String startingLevelPath {get; set;} = "res://Scenes/TestLevel.tscn";

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
		MessageManager.instance.addViewportToMessager(this);
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
	}
	public void LoadLevel()
	{
		if (currentLevel != null)
		{
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
	}
}

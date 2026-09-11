using Godot;
using System;
using System.Threading.Tasks;

public partial class Savemanager : Node
{
	public static Savemanager instance {get; private set;}
	private string savePath = "user://"; //do not save to Res:// as it is read only after compile.
	private string saveFileName = "save.json"; //individual save game name. each slot will contain only 1 file.
	private int saveSlot = 1; //default save slot. multiple saaves result in a folder for each slot. 
	public override void _Ready()
	{
		instance = this;
		this.ProcessMode = ProcessModeEnum.Always; //set the Save manager to ALWAYS be active and availiable.

	}
	public void SaveGame (int slot = 1)
	{
		/////////////////////////////////////////////////////////////////////////
		//Taken an modified from Godot documentation ////////////////////////////
		// https://docs.godotengine.org/en/4.4/tutorials/io/saving_games.html ///
		///////////////////////////////////////////////////////////////////////// 

		Error saveDirectoryError = DirAccess.MakeDirRecursiveAbsolute(GetSaveDirectory(slot));
		if (saveDirectoryError != Error.Ok)
		{
			GD.PrintErr($"unable to locate or create save Dirrectory {GetSaveDirectory(slot)}");
			return;
		} 
		using var saveFile = FileAccess.Open(GetFullSavePath(slot), FileAccess.ModeFlags.Write);
		//if save file cannot be opened or created. return error.
		if (saveFile == null)
		{
			GD.PrintErr($"Cannot find or create save file {GetFullSavePath(slot)}, with the error {FileAccess.GetOpenError()}");
			return;
		}

		//get an array of nodes in the game that are part of the persistThroughSave Group.
		Godot.Collections.Array<Node> saveNodes = GetTree().GetNodesInGroup("persistThroughSave");
		foreach (Node saveNode in saveNodes)
		{
			// Check the node is an instanced scene so it can be instanced again during load.
			if (string.IsNullOrEmpty(saveNode.SceneFilePath))
			{
				GD.Print($"persistent node '{saveNode.Name}' is not an instanced scene, skipped");
				continue;
			}

			// Check the node has a save function.
			if (!saveNode.HasMethod("Save"))
			{
				GD.Print($"persistent node '{saveNode.Name}' is missing a Save() function, skipped");
				continue;
			}

			// Call the node's save function.
			var nodeData = saveNode.Call("Save");

			// Json provides a static method to serialized JSON string.
			var jsonString = Json.Stringify(nodeData);

			// Store the save dictionary as a new line in the save file.
			saveFile.StoreLine(jsonString);
		}
	}
	public void LoadGame (int slot = 1)
	{
		//todo
	}
	public void DeleteGame (int slot =1)
	{
		//todo
	}
	private string GetFullSavePath (int slot = 1)
	{
		//todo
		return $"{savePath}slot{slot}/{saveFileName}";
	}
	private string GetSaveDirectory (int slot = 1)
	{
		return $"{savePath}slot{slot}";
	}

}

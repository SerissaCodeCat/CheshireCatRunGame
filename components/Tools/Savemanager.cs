using Godot;
public partial class Savemanager : Node
{
    /// <summary>
    /// Singleton instance of the save manager, assigned when the node is ready.
    /// </summary>
    public static Savemanager instance { get; private set; }
    // Save to user:// because res:// is read-only after the project is exported.
    private string savePath = "user://";
    // Each save slot contains a single save file with this name.
    private string saveFileName = "save.json";
    [Export]
    private string savableNodeGroup = "persistThroughSave";
    // Key that persistent nodes must write in their Save() dictionary to identify themselves.
    private const string SaveIdKey = "saveId";
    // Maps a node's SaveId to the live scene node, built at the start of a load.
    private System.Collections.Generic.Dictionary<string, Node> SavedNodes = [];
    /// <summary>
    /// Stores the singleton reference and keeps the save manager running even while the tree is paused.
    /// </summary>
    public override void _Ready()
    {
        instance = this;
        // Keep processing while the game is paused so saving/loading stays available.
        ProcessMode = ProcessModeEnum.Always;
    }
    /// <summary>
    /// Serialises every node in the savable group to a JSON file for the given slot.
    /// Adapted from the Godot documentation:
    /// https://docs.godotengine.org/en/4.4/tutorials/io/saving_games.html
    /// </summary>
    /// <param name="slot">int; which save slot directory the save file is written into.</param>
    public void SaveGame(int slot = 1)
    {
        string saveDirectory = GetSaveDirectory(slot);
        string saveFilePath = GetFullSavePath(slot);
        // Create the slot directory (and any missing parents) before opening the file.
        Error saveDirectoryError = DirAccess.MakeDirRecursiveAbsolute(saveDirectory);
        if (saveDirectoryError != Error.Ok)
        {
            GD.PrintErr($"unable to locate or create save Dirrectory {saveDirectory}");
            return;
        }
        using var saveFile = FileAccess.Open(saveFilePath, FileAccess.ModeFlags.Write);
        if (saveFile == null)
        {
            GD.PrintErr($"Cannot find or create save file {saveFilePath}, with the error {FileAccess.GetOpenError()}");
            return;
        }
        // Every node that asked to be persisted through a save.
        Godot.Collections.Array<Node> saveNodes = GetTree().GetNodesInGroup(savableNodeGroup);
        foreach (Node saveNode in saveNodes)
        {
            // Skip nodes that are not instanced scenes; they cannot be re-instanced on load.
            if (string.IsNullOrEmpty(saveNode.SceneFilePath))
            {
                GD.Print($"persistent node '{saveNode.Name}' is not an instanced scene, skipped");
                continue;
            }
            // Skip nodes that cannot provide their own save data.
            if (!saveNode.HasMethod("Save"))
            {
                GD.Print($"persistent node '{saveNode.Name}' is missing a Save() function, skipped");
                continue;
            }
            // Ask the node for its save data and append it to the file as one JSON line.
            Variant nodeData = saveNode.Call("Save");
            saveFile.StoreLine(Json.Stringify(nodeData));
        }
        GD.Print(OS.GetDataDir());
    }
    /// <summary>
    /// Loads the save file for the given slot and hands each saved line back to its matching scene node.
    /// </summary>
    /// <param name="slot">int; which save slot directory the save file is read from.</param>
    public void LoadGame(int slot = 1)
    {
        string saveFilePath = GetFullSavePath(slot);
        if (!FileAccess.FileExists(saveFilePath))
        {
            GD.PrintErr($"no save file found at {saveFilePath}");
            return;
        }
        MapSavableNodesById();
        ApplySaveFile(saveFilePath);
    }
    /// <summary>
    /// Builds the SaveId-to-node lookup from every node currently in the savable group.
    /// Nodes without a string 'SaveId' property or a 'Load' method are reported and skipped.
    /// </summary>
    private void MapSavableNodesById()
    {
        foreach (Node node in GetTree().GetNodesInGroup(savableNodeGroup))
        {
            // Get() returns a Nil variant when the property does not exist on the node.
            Variant idVariant = node.Get("SaveId");
            if (idVariant.VariantType != Variant.Type.String)
            {
                GD.PrintErr($"{node.GetPath()} needs a string 'SaveId' property");
                if (!node.HasMethod("Load"))
                {
                    GD.PrintErr($"{node.GetPath()} needs a 'Load' method.");
                }
                continue;
            }
            string nodeId = idVariant.AsString();
            // Store in a System dictionary so standard helpers like TryAdd/Remove are available.
            if (!SavedNodes.TryAdd(nodeId, node))
            {
                GD.PrintErr($"Duplicate SaveId '{nodeId}' on {node.GetPath()}; ignoring.");
            }
        }
    }
    /// <summary>
    /// Reads a save file line by line and passes each JSON object to the node with the matching SaveId.
    /// </summary>
    /// <param name="saveFilePath">string; the full user:// path of the save file to read.</param>
    private void ApplySaveFile(string saveFilePath)
    {
        // The file's existence is checked in LoadGame, so Open is expected to succeed here.
        using var saveFile = FileAccess.Open(saveFilePath, FileAccess.ModeFlags.Read);
        int lineNumber = 0;
        while (saveFile.GetPosition() < saveFile.GetLength())
        {
            string jsonString = saveFile.GetLine();
            lineNumber++;
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                continue;
            }
            var json = new Json();
            if (json.Parse(jsonString) != Error.Ok)
            {
                GD.PrintErr($"Save line {lineNumber}: JSON parse error: {json.GetErrorMessage()}");
                continue;
            }
            if (json.Data.VariantType != Variant.Type.Dictionary)
            {
                GD.PrintErr($"Save line {lineNumber}: expected a JSON object, got {json.Data.VariantType}.");
                continue;
            }
            var nodeData = json.Data.AsGodotDictionary<string, Variant>();
            // Every saved line must carry a string SaveId so it can be routed to a node.
            if (!nodeData.TryGetValue(SaveIdKey, out Variant idVariant)
                || idVariant.VariantType != Variant.Type.String)
            {
                GD.PrintErr($"Save line {lineNumber}: missing or invalid '{SaveIdKey}'.");
                continue;
            }
            string id = idVariant.AsString();
            // Remove the matched node so each saved line is only applied once.
            if (!SavedNodes.Remove(id, out Node target))
            {
                GD.PushWarning($"Save line {lineNumber}: no node with SaveId '{id}' in the current scene.");
                continue;
            }
            // The node returns false when it refuses the supplied data.
            if (!target.Call("Load", nodeData).AsBool())
            {
                GD.PrintErr($"Save line {lineNumber}: node '{id}' rejected its data.");
            }
        }
    }
    /// <summary>
    /// Deletes the save file for the given slot.
    /// </summary>
    /// <param name="slot">int; which save slot to delete.</param>
    public void DeleteGame(int slot = 1)
    {
        //todo
    }
    /// <summary>
    /// Builds the full user:// path to the save file for a slot.
    /// </summary>
    /// <param name="slot">int; the save slot number.</param>
    /// <returns>string; the full path to the slot's save file.</returns>
    private string GetFullSavePath(int slot = 1)
    {
        return $"{savePath}slot{slot}/{saveFileName}";
    }
    /// <summary>
    /// Builds the user:// directory path for a save slot.
    /// </summary>
    /// <param name="slot">int; the save slot number.</param>
    /// <returns>string; the directory path that contains the slot's save file.</returns>
    private string GetSaveDirectory(int slot = 1)
    {
        return $"{savePath}slot{slot}";
    }
}
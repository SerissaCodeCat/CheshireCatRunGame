using System;
using Godot;
/// <summary>
/// Hosts the currently loaded level inside a low-resolution <see cref="SubViewport"/>,
/// handles level swapping, integer-scaled resolution changes, and pixel-perfect rendering.
/// </summary>
public partial class SubViewportContainer : Godot.SubViewportContainer
{
    /// <summary>
    /// Globally accessible instance of the level viewport container (set in <see cref="_Ready"/>).
    /// </summary>
    public static SubViewportContainer Instance { get; private set; }
    /// <summary>
    /// The native (unscaled) resolution the game is drawn at. The integer
    /// <see cref="Godot.SubViewportContainer.StretchShrink"/> is derived from this.
    /// </summary>
    [Export]
    public Vector2I BaseResolution { get; set; } = new Vector2I(640, 360);
    /// <summary>
    /// When true, the camera's sub-pixel remainder is applied as a screen-space offset,
    /// giving smooth scrolling while all sprites stay locked to the game-pixel grid.
    /// When false, the view moves strictly in whole game pixels.
    /// </summary>
    [Export]
    public bool SmoothSubPixelScrolling { get; set; } = true;
    /// <summary>
    /// The SubViewport that renders the active level. If left empty in the editor,
    /// the first child of this container is used.
    /// </summary>
    [Export]
    public SubViewport ViewportLink { get; set; } = null;
    /// <summary>
    /// Starting level path exposed to the editor for easy setup of the initial level
    /// to be loaded by the main scene.
    /// </summary>
    [Export]
    public string StartingLevelPath { get; set; } = null;
    /// <summary>
    /// System filepath to the next level to be loaded. Should be set by individual level
    /// doors to allow level-skips through the MessageManager if needed / desired.
    /// </summary>
    public string NextLevelPath { get; set; } = null;
    private const string SubPixelOffsetParam = "subpixel_offset";
    private const string DefaultLevelPath = "res://Scenes/TestLevel.tscn";
    private ShaderMaterial pixelMaterial = null;
    private Node currentLevel = null;
    /// <summary>
    /// Godot lifecycle callback. Registers the singleton instance, configures pixel-perfect
    /// rendering on the SubViewport, applies the edge margin, and loads the starting level.
    /// </summary>
    /// <example>
    /// Called automatically by the Godot engine when the node enters the scene tree.
    /// It is not intended to be called directly from other scripts.
    /// </example>
    public override void _Ready()
    {
        Instance = this;
        if (string.IsNullOrEmpty(StartingLevelPath))
        {
            StartingLevelPath = DefaultLevelPath;
        }
        ViewportLink ??= GetChild<SubViewport>(0);
        pixelMaterial = Material as ShaderMaterial;
        if (pixelMaterial == null)
        {
            GD.PushWarning("SubViewportContainer: no ShaderMaterial assigned; sub-pixel smoothing disabled.");
        }
        ConfigurePixelPerfectRendering();
        ApplyEdgeMargin();
        MessageManager.instance.AddViewportToMessager(Instance);
        LoadLevel();
    }
    /// <summary>
    /// Applies the camera's sub-pixel remainder to the display shader. The camera itself
    /// must already be positioned on a whole game pixel; this only shifts the final image
    /// by the leftover fraction, converted to screen pixels.
    /// </summary>
    /// <param name="subPixelRemainder">
    /// The camera's true (unrounded) position minus its rounded position, in game pixels.
    /// Each component should be in the range -0.5 to 0.5.
    /// </param>
    /// <example>
    /// Called once per frame by the active camera, after it snaps itself:
    /// <code>
    /// Vector2 snapped = truePosition.Round();
    /// GlobalPosition = snapped;
    /// SubViewportContainer.instance.ViewportWholePixelOnlyMovement(truePosition - snapped);
    /// </code>
    /// </example>
    public void ViewportWholePixelOnlyMovement(Vector2 subPixelRemainder)
    {
        if (pixelMaterial == null)
        {
            return;
        }
        // Camera further right than drawn => world must shift further left, hence the negation.
        Vector2 screenOffset = SmoothSubPixelScrolling
            ? -subPixelRemainder * StretchShrink
            : Vector2.Zero;
        pixelMaterial.SetShaderParameter(SubPixelOffsetParam, screenOffset);
    }
    /// <summary>
    /// Sets the path of the level that will be loaded by the next call to <see cref="LoadLevel"/>.
    /// </summary>
    /// <param name="incomingPath">Resource path of the level scene, e.g. "res://Scenes/Level2.tscn".</param>
    /// <example>
    /// Called by a level door before triggering a transition:
    /// <code>
    /// SubViewportContainer.instance.SetNextLevelPath("res://Scenes/Level2.tscn");
    /// SubViewportContainer.instance.LoadLevel();
    /// </code>
    /// </example>
    public void SetNextLevelPath(string incomingPath)
    {
        NextLevelPath = incomingPath;
        GD.Print("Setting level path to load to: " + NextLevelPath);
    }
    /// <summary>
    /// Requests a level swap. Unloads the current level and loads <see cref="NextLevelPath"/>
    /// if set, otherwise <see cref="StartingLevelPath"/>. The swap is deferred to the end of
    /// the frame so it is safe to call from physics callbacks (e.g. a door's body_entered).
    /// </summary>
    /// <example>
    /// <code>
    /// SubViewportContainer.instance.LoadLevel();
    /// </code>
    /// </example>
    public void LoadLevel()
    {
        Callable.From(PerformLevelSwap).CallDeferred();
    }
    /// <summary>
    /// Resizes the game to the given window resolution, choosing the largest integer scale
    /// of <see cref="BaseResolution"/> that fits, so game pixels remain perfectly square.
    /// </summary>
    /// <param name="incomingX">Target width in screen pixels.</param>
    /// <param name="incomingY">Target height in screen pixels.</param>
    /// <example>
    /// Called from a settings menu:
    /// <code>
    /// SubViewportContainer.instance.setResolution(1280, 720); // StretchShrink becomes 2
    /// </code>
    /// </example>
    public void setResolution(int incomingX = 1920, int incomingY = 1080)
    {
        int scale = Math.Max(1, Math.Min(incomingX / BaseResolution.X, incomingY / BaseResolution.Y));
        GetParent<Control>().Size = new Vector2(incomingX, incomingY);
        StretchShrink = scale;
        ApplyEdgeMargin();
        GetTree().Root.ContentScaleSize = new Vector2I(incomingX, incomingY);
    }
    /// <summary>
    /// Frees the old level, instantiates the new one, resets the shader offset, and
    /// re-targets the camera. Runs deferred via <see cref="LoadLevel"/>.
    /// </summary>
    private void PerformLevelSwap()
    {
        if (currentLevel != null && IsInstanceValid(currentLevel))
        {
            GD.Print("Freeing current level...");
            // Remove first so the old player/camera can't be found during the same frame.
            ViewportLink.RemoveChild(currentLevel);
            currentLevel.QueueFree();
        }
        string levelToLoad = !string.IsNullOrEmpty(NextLevelPath) ? NextLevelPath : StartingLevelPath;
        NextLevelPath = null;
        PackedScene levelScene = GD.Load<PackedScene>(levelToLoad);
        if (levelScene == null)
        {
            GD.PushError("SubViewportContainer: failed to load level at " + levelToLoad);
            return;
        }
        currentLevel = levelScene.Instantiate();
        ViewportLink.AddChild(currentLevel);
        ViewportWholePixelOnlyMovement(Vector2.Zero);
        GD.Print("Level loaded successfully: " + levelToLoad);
        MessageManager.instance.SetPlayerAsCameraTarget();
    }
    /// <summary>
    /// Enables whole-pixel snapping and nearest-neighbour filtering on the SubViewport
    /// and on the container that displays it.
    /// </summary>
    private void ConfigurePixelPerfectRendering()
    {
        Stretch = true;
        TextureFilter = TextureFilterEnum.Nearest;
        ViewportLink.Snap2DTransformsToPixel = true;
        ViewportLink.Snap2DVerticesToPixel = true;
        ViewportLink.CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest;
    }
    /// <summary>
    /// Grows the container by one game pixel on every side (clipped by the parent) so that
    /// shifting the image by the sub-pixel offset never reveals an empty edge.
    /// </summary>
    private void ApplyEdgeMargin()
    {
        if (GetParent() is Control parent)
        {
            parent.ClipContents = true;
        }
        int margin = SmoothSubPixelScrolling ? StretchShrink : 0;
        SetAnchorsPreset(LayoutPreset.FullRect);
        OffsetLeft = -margin;
        OffsetTop = -margin;
        OffsetRight = margin;
        OffsetBottom = margin;
    }
}
using Godot;

public partial class PixelPerfectCamera : Camera2D
{
    private Node2D Target = null;
    private Vector2 actualCameraPosition;
    private Vector2 cameraOffsetPossition;
    public override void _Ready()
    {
        MessageManager.instance.addCameraToMessager(this);
    }
    public void SetCameraTarget(Node2D incomingTarget)
    {
        GD.Print("Taget Set to" + incomingTarget.Name);
        Target = incomingTarget;
    }
    public void moveToPosition(Vector2 incomingPosition)
    {
        this.Position = incomingPosition;
    }
    public override void _Process(double delta)
    {
        actualCameraPosition = actualCameraPosition.Lerp(Target.GlobalPosition, (float)delta * 3.0f);
        cameraOffsetPossition = actualCameraPosition.Round() - actualCameraPosition;

        MessageManager.instance.updateViewportWholePixelOnlyMovement(cameraOffsetPossition);

        this.GlobalPosition = actualCameraPosition.Round();
    }

}

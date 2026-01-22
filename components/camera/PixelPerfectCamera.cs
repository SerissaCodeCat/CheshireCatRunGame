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
        Vector2 actualCameraTargetPosition;
        if (Target == null)
        {
            return;
        }
        //keep the camera from going above Y of 0, but allow X movement. remember that in Godot, Y increases downwards.
        actualCameraTargetPosition = Target.GlobalPosition.Y > 0 ? new Vector2(Target.GlobalPosition.X, 0) : Target.GlobalPosition;
        
        actualCameraPosition = actualCameraPosition.Lerp(actualCameraTargetPosition, (float)delta * 3.0f);
        cameraOffsetPossition = actualCameraPosition.Round() - actualCameraPosition;

        MessageManager.instance.updateViewportWholePixelOnlyMovement(cameraOffsetPossition);

        this.GlobalPosition = actualCameraPosition.Round();
    }

}

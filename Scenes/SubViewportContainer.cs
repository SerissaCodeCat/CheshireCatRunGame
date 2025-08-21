using Godot;

public partial class SubViewportContainer : Godot.SubViewportContainer
{
    private ShaderMaterial material = null;
    public override void _Ready()
    {
        MessageManager.instance.addViewportToMessager(this);
        material = (ShaderMaterial)this.Material;

    }
    public void ViewportWholePixelOnlyMovement(Vector2 incomingUpdate)
    {
        material.SetShaderParameter("camera_offset", incomingUpdate);
    }
}

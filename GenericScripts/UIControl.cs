using Godot;
using System.Collections.Generic;

public partial class UIControl : Control
{

    private Node healthPipFillOne;
    private Node healthPipFillTwo;
    private Node healthPipFillThree;
    private List<Node> HealthPipFills;
    public override void _Ready()
    {
        MessageManager.instance.addUIControlToMessageManager(this);
        HealthPipFills.Add(GetNode<Node>($"HealthPipOne/HealthPipOneFill"));
        HealthPipFills.Add(GetNode<Node>($"HealthPipTwo/HealthPipTwoFill"));
        HealthPipFills.Add(GetNode<Node>($"HealthPipThree/HealthPipThreeFill"));
    }




}

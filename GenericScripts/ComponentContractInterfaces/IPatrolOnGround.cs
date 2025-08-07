public interface IPatrolOnGround
{
	// Called when the node enters the scene tree for the first time.
	abstract void Move(ref Godot.Vector2 incomingVelocity, double incomingDelta);
	abstract void DetectedMove(ref Godot.Vector2 incomingVelocity, bool detectionDirrection, double incomingDelta);
}

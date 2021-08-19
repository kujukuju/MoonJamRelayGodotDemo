using Godot;

public class StartArea : Area2D
{
	Scene owner;

	public override void _Ready() {
		owner = GetTree().GetNodesInGroup("scene")[0] as Scene;
	}

	public override void _PhysicsProcess(float _delta) {
		if (owner.HasStarted) {
			QueueFree();
		}
	}
}

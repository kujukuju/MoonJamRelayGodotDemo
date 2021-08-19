using Godot;

public class Timer : PanelContainer
{
	Scene owner;
	uint start;
	Label label;

	public override void _Ready() {
		owner = GetTree().GetNodesInGroup("scene")[0] as Scene;
		label = GetNode("Timer") as Label;
		Visible = false;
		SetPhysicsProcess(false);
	}

	public override void _Process(float _delta) {
		if (owner.HasStarted) {
			SetProcess(false);
			SetPhysicsProcess(true);
			start = OS.GetTicksMsec();
			Visible = true;
		}
	}

	public override void _PhysicsProcess(float delta) {
		label.Text = GetElapsedTime();
		RectSize = Vector2.Zero;
	}

	public string GetElapsedTime() {
		uint elapsed = OS.GetTicksMsec() - start;
		uint ms = elapsed % 1000;
		uint seconds = (elapsed - ms) / 1000;
		uint minutes = seconds / 60;
		seconds %= 60;
		return $"{minutes}:{seconds:00}.{ms:000}";
	}
}

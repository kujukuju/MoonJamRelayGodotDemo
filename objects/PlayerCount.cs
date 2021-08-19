using Godot;

public class PlayerCount : RichTextLabel {
	Scene owner;

	public override void _Ready() {
		owner = GetTree().GetNodesInGroup("scene")[0] as Scene;
	}

	public override void _PhysicsProcess(float _delta) {
		int playerCount = owner.players.Count + 1;
		// Text = $"Currently there are\n{playerCount} players";
		if (playerCount > 1) {
			Text = $"Currently there are\n{playerCount} players";
			return;
		}
		Text = $"Currently there is\n{playerCount} player";
	}
}

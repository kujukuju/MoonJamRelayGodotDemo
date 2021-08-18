using Godot;

public class PlayerCount : Label {
	Scene owner;

	public override void _Ready() {
		owner = Owner as Scene;
	}

	public override void _PhysicsProcess(float _delta) {
		int playerCount = owner.players.Count + 1;
		// Text = $"Currently there are\n{playerCount} players";
		if (playerCount > 1) {
			Text = $"{playerCount} players";
			return;
		}
		Text = $"{playerCount} player";
	}
}

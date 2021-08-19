using Godot;

public class HUDPlayerCount : Label {
	Scene owner;

	public override void _Ready() {
		owner = GetTree().GetNodesInGroup("scene")[0] as Scene;
	}

	public override void _PhysicsProcess(float _delta) {
		switch (owner.ConnectionStatus) {
			case NetworkedMultiplayerPeer.ConnectionStatus.Connecting:
				Text = "Connecting";
				return;
			case NetworkedMultiplayerPeer.ConnectionStatus.Disconnected:
				Text = "Disconnected";
				return;
		}
		int playerCount = owner.players.Count + 1;
		// Text = $"Currently there are\n{playerCount} players";
		if (playerCount > 1) {
			Text = $"{playerCount} players";
			return;
		}
		Text = $"{playerCount} player";
	}
}

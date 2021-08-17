using Godot;
using System;

public class PlayerCount : RichTextLabel {
	public override void _Process(float delta) {
		Scene scene = GetParent() as Scene;
		int playerCount = scene.players.Count + 1;
		
		SetText("Currently there are\n" + playerCount + " players");
	}
}

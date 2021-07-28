using Godot;
using System;

public class AngledBoxes : TileMap {
	public override void _Ready() {
		SetMeta("angled", true);
	}
}

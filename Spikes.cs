using Godot;
using System;

public class Spikes : TileMap {
	public override void _Ready() {
		SetMeta("spikes", true);
	}
}

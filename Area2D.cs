using Godot;
using System;

public class Area2D : Godot.Area2D {
	const float POP_COOLDOWN = 2000;
	
	float remainingPoppedTime = 0;
	
	public override void _Process(float delta) {
		// milliseconds
		delta *= 1000;
		
		if (!HasNode("/root/scene/Player")) {
			return;
		}
		
		Player player = GetNode<Player>("/root/scene/Player");
		if (remainingPoppedTime == 0) {
			if (OverlapsBody(player)) {
				remainingPoppedTime = POP_COOLDOWN;
				player.Pop();
			}
		} else {
			if (player.remainingPoppedTime == 0) {
				remainingPoppedTime = Math.Max(remainingPoppedTime - delta, 0);
			}
		}
		
		if (remainingPoppedTime == 0) {
			AnimatedSprite owner = (AnimatedSprite) GetParent();
			owner.Visible = true;
		} else {
			AnimatedSprite owner = (AnimatedSprite) GetParent();
			owner.Visible = false;
		}
	}
}

using Godot;
using System;

public class AnimatedPlayer : AnimatedSprite {
	const float EPSILON = 0.000001f;
	const float MS_PER_FRAME = 100;
	
	float deltaTime = 0;

	public override void _Process(float delta) {
		// milliseconds
		delta *= 1000;
		
		Vector2 velocity = (Vector2) Owner.Get("velocity");
		if (velocity[0] < -EPSILON) {
			FlipH = false;
		}
		if (velocity[0] > EPSILON) {
			FlipH = true;
		}
		
		// do animation stuff
		bool falling = (bool) Owner.Get("falling");
		bool jumping = (bool) Owner.Get("jumping");
		if (falling) {
			Animation = "falling";
		} else if (jumping) {
			Animation = "jumping";
		} else if (Math.Abs(velocity[0]) > EPSILON) {
			Animation = "running";
			deltaTime += Math.Abs(velocity[0]) / Player.MAX_VELOCITY * delta;
		} else {
			Animation = "idle";
			deltaTime += delta;
		}
		
		Frame = (int) (deltaTime / MS_PER_FRAME) % Frames.GetFrameCount(Animation);
	}
}

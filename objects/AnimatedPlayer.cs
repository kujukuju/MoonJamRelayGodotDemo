using Godot;
using System;

public class AnimatedPlayer : AnimatedSprite {
	const float MAX_VELOCITY = 200;
	const float FORCED_DEAD_TIME = 400;
	const float EPSILON = 0.000001f;
	const float MS_PER_FRAME = 100;

	IPlayer player;
	float deltaTime = 0;

	public override void _Ready() {
		player = GetParent() as IPlayer;
	}

	public override void _Process(float delta) {
		// milliseconds
		delta *= 1000;

		Vector2 velocity = player.Velocity;
		if (velocity.x < -EPSILON) {
			FlipH = false;
		}
		if (velocity.x > EPSILON) {
			FlipH = true;
		}

		// do animation stuff
		bool dead = player.dead;
		bool falling = player.falling;
		bool jumping = player.jumping;
		bool loop = true;
		if (dead) {
			Animation = "death";
			if (player.remainingForcedDeadTime > 0) {
				deltaTime = FORCED_DEAD_TIME - player.remainingForcedDeadTime;
			} else {
				deltaTime = player.remainingResurrectTime;
			}
			loop = false;
		} else if (falling) {
			Animation = "falling";
			deltaTime = 0;
		} else if (jumping) {
			Animation = "jumping";
			deltaTime = 0;
		} else if (Math.Abs(velocity.x) > EPSILON) {
			Animation = "running";
			deltaTime += Math.Abs(velocity.x) / MAX_VELOCITY * delta;
		} else {
			Animation = "idle";
			deltaTime += delta;
		}

		if (loop) {
			Frame = (int) (deltaTime / MS_PER_FRAME) % Frames.GetFrameCount(Animation);
		} else {
			Frame = Math.Min((int) (deltaTime / MS_PER_FRAME), Frames.GetFrameCount(Animation) - 1);
		}
	}
}

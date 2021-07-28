using Godot;
using System;

public class Player : KinematicBody2D {
	public const float MAX_VELOCITY = 200;
	public const float FORCED_DEAD_TIME = 400;
	
	const float EPSILON = 0.000001f;
	const float ACCEL = 2.5f;
	const float AIR_ACCEL = 2.5f;
	const float FRICTION = 1;
	const float GRAVITY = 1.2f;
	const float JUMP_INSTANT_VELOCITY = 240;
	const float JUMP_HOLD_ACCEL = 1.0f;
	const float POP_TIME = 1000;
	
	public Vector2 velocity = new Vector2(0, 0);
	public bool jumping = false;
	public bool falling = false;
	public float remainingPoppedTime = 0;
	public float remainingForcedDeadTime = 0;
	public float remainingResurrectTime = 0;
	public bool dead = false;
	public bool killable = true;
	
	public void Pop() {
		remainingPoppedTime = POP_TIME;
		velocity[0] /= 10;
		velocity[1] /= 10;
	}

	public override void _PhysicsProcess(float delta) {
		// milliseconds
		delta *= 1000;
		
		float poppedMul = remainingPoppedTime > 0 ? 0.5f - remainingPoppedTime / POP_TIME / 2.0f : 1;
		remainingPoppedTime = Math.Max(remainingPoppedTime - delta, 0);
		
		// check spike collisions for death
		bool angled = false;
		if (!dead && killable) {
			for (int i = 0; i < GetSlideCount(); i++) {
				KinematicCollision2D collisions = GetSlideCollision(i);
				if (collisions.Collider.HasMeta("spikes")) {
					dead = true;
					killable = false;
					remainingForcedDeadTime = FORCED_DEAD_TIME;
					remainingResurrectTime = FORCED_DEAD_TIME;
				}
			}
		}
		
		for (int i = 0; i < GetSlideCount(); i++) {
			KinematicCollision2D collisions = GetSlideCollision(i);
			if (collisions.Collider.HasMeta("angled")) {
				angled = true;
			}
		}
		
		if (dead) {
			remainingForcedDeadTime = Math.Max(remainingForcedDeadTime - delta, 0);
			if (remainingForcedDeadTime == 0 && velocity.Length() < EPSILON) {
				if (remainingResurrectTime > 0) {
					remainingResurrectTime = Math.Max(remainingResurrectTime - delta, 0);
				} else {
					dead = false;
				}
			}
		}
		
		if (!dead && !killable && velocity.Length() >= EPSILON) {
			killable = true;
		}
		
		bool left = Input.IsActionPressed("left") && !dead && !angled;
		bool right = Input.IsActionPressed("right") && !dead && !angled;
		bool up = Input.IsActionPressed("up") && !dead;
		bool down = Input.IsActionPressed("down") && !dead;
		bool jump = Input.IsActionPressed("jump") && !dead;
		bool dash = Input.IsActionPressed("dash") && !dead;
		
		// accel
		float accel = 0;
		if (left) {
			accel -= (IsOnFloor() ? ACCEL : AIR_ACCEL) * poppedMul;
		}
		if (right) {
			accel += (IsOnFloor() ? ACCEL : AIR_ACCEL) * poppedMul;
		}
		
		// friction
		if (IsOnFloor()) {
			if (Math.Abs(velocity[0]) <= FRICTION * delta * poppedMul) {
				velocity[0] = 0;
			} else {
				float frictionSpeed = Math.Max(Math.Abs(velocity[0]) - FRICTION * delta * poppedMul, 0);
				velocity[0] = Math.Sign(velocity[0]) * frictionSpeed;
			}
		}
		
		// input acceleration
		if (Math.Abs(velocity[0]) < MAX_VELOCITY) {
			velocity[0] += accel * delta * poppedMul;
			
			if (Math.Abs(velocity[0]) > MAX_VELOCITY) {
				velocity[0] = Math.Sign(velocity[0]) * MAX_VELOCITY;
			}
		} else {
			float previousSpeed = Math.Abs(velocity[0]);
			velocity[0] += accel * delta * poppedMul;
			
			float desiredSpeed = Math.Min(previousSpeed, Math.Abs(velocity[0]));
			velocity[0] = Math.Sign(velocity[0]) * desiredSpeed;
		}
		
		// jumping
		bool onRightWall = TestMove(Transform, new Vector2(1, -2)) && TestMove(Transform, new Vector2(1, 2));
		bool onLeftWall = TestMove(Transform, new Vector2(-1, -2)) && TestMove(Transform, new Vector2(-1, 2));
		bool onWall = (onRightWall || onLeftWall) && onRightWall != onLeftWall;
		
		bool canJump = IsOnFloor() || onWall;
		if (jumping && !jump) {
			jumping = false;
		}
		if (jumping && canJump && velocity[1] >= 0) {
			jumping = false;
		}
		
		if (jumping) {
			// if this jump is being held from the last frame reduce gravity
			velocity[1] -= Math.Min(Math.Abs(Math.Min(velocity[1], 0)) / JUMP_INSTANT_VELOCITY, 1) * JUMP_HOLD_ACCEL * delta * poppedMul;
		}
		
		if (!jumping && canJump && jump) {
			velocity[1] = Math.Max(velocity[1] - JUMP_INSTANT_VELOCITY * poppedMul, -JUMP_INSTANT_VELOCITY * poppedMul);
			if (onWall && !IsOnFloor()) {
				float direction = onRightWall ? -1 : 1;
				velocity[0] += direction * JUMP_INSTANT_VELOCITY * poppedMul;
			}
			
			jumping = true;
		}
		
		if (remainingPoppedTime > 0 && dash) {
			Vector2 dashDirection = new Vector2();
			if (up) {
				dashDirection[1] -= 1;
			}
			if (down) {
				dashDirection[1] += 1;
			}
			if (left) {
				dashDirection[0] -= 1;
			}
			if (right) {
				dashDirection[0] += 1;
			}
			
			dashDirection = dashDirection.Normalized();
			
			velocity[0] += dashDirection[0] * JUMP_INSTANT_VELOCITY * 2;
			velocity[1] = Math.Max(Math.Min(velocity[1], 0) + dashDirection[1] * JUMP_INSTANT_VELOCITY * 2, -JUMP_INSTANT_VELOCITY);
			jumping = true;
			
			remainingPoppedTime = 0;
		}
		
		// phase through one way platforms
		if (IsOnFloor() && down) {
			Position = new Vector2(Position[0], Position[1] + 1);
		}
		
		// gravity
		velocity[1] += GRAVITY * delta * poppedMul;
		falling = !IsOnFloor() && velocity[1] > EPSILON;
		
		velocity = MoveAndSlide(velocity, new Vector2(0, -1), false, 4, (float) Math.PI / 16, true);
	}
}

using Godot;
using System;

public class Player : KinematicBody2D {
	public const float MAX_VELOCITY = 200;
	const float EPSILON = 0.000001f;
	const float ACCEL = 2.5f;
	const float FRICTION = 1;
	const float GRAVITY = 1.2f;
	const float JUMP_INSTANT_VELOCITY = 240;
	const float JUMP_HOLD_ACCEL = 1.0f;
	
	Vector2 velocity = new Vector2(0, 0);
	bool jumping = false;
	bool falling = false;

	public override void _PhysicsProcess(float delta) {
		// milliseconds
		delta *= 1000;
		
		float accel = 0;
		if (Input.IsActionPressed("ui_left")) {
			accel -= ACCEL;
		}
		if (Input.IsActionPressed("ui_right")) {
			accel += ACCEL;
		}
		
		// friction
		if (Math.Abs(velocity[0]) <= FRICTION * delta) {
			velocity[0] = 0;
		} else {
			float frictionSpeed = Math.Max(Math.Abs(velocity[0]) - FRICTION * delta, 0);
			velocity[0] = Math.Sign(velocity[0]) * frictionSpeed;
		}
		
		// input acceleration
		if (Math.Abs(velocity[0]) < MAX_VELOCITY) {
			velocity[0] += accel * delta;
			
			if (Math.Abs(velocity[0]) > MAX_VELOCITY) {
				velocity[0] = Math.Sign(velocity[0]) * MAX_VELOCITY;
			}
		} else {
			float previousSpeed = Math.Abs(velocity[0]);
			velocity[0] += accel * delta;
			
			float desiredSpeed = Math.Min(previousSpeed, Math.Abs(velocity[0]));
			velocity[0] = Math.Sign(velocity[0]) * desiredSpeed;
		}
		
		// jumping
		if (jumping && !Input.IsActionPressed("ui_up")) {
			jumping = false;
		}
		if (jumping && IsOnFloor()) {
			jumping = false;
		}
		
		if (jumping) {
			// if this jump is being held from the last frame reduce gravity
			velocity[1] -= Math.Abs(Math.Min(velocity[1], 0)) / JUMP_INSTANT_VELOCITY * JUMP_HOLD_ACCEL * delta;
		}
		
		if (!jumping && IsOnFloor() && Input.IsActionPressed("ui_up")) {
			jumping = true;
			
			velocity[1] -= JUMP_INSTANT_VELOCITY;
		}
		
		// gravity
		velocity[1] += GRAVITY * delta;
		falling = !IsOnFloor() && velocity[1] > EPSILON;
		
		velocity = MoveAndSlide(velocity, new Vector2(0, -1));
	}
}

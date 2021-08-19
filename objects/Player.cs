using Godot;
using System;

public class Player : KinematicBody2D {
	public const float MAX_VELOCITY = 200;
	public const float FORCED_DEAD_TIME = 400;

	const byte STATE_JUMPING   = 1 << 0;
	const byte STATE_FALLING   = 1 << 1;
	const byte STATE_DEAD      = 1 << 2;
	const byte STATE_KILLABLE  = 1 << 3;
	const byte STATE_JUST_DIED = 1 << 4;
	const byte STATE_HAS_STARTED = 1 << 5;

	const float EPSILON = 0.000001f;
	[Export] float ACCEL = 2.5f;
	[Export] float AIR_ACCEL = 2.5f;
	[Export] float FRICTION = 1;
	[Export] float AIR_FRICTION = 0.05f;
	[Export] float GRAVITY = 1.1f;
	[Export] float GRAVITY_MULT = 1.1f;
	[Export] float JUMP_INSTANT_VELOCITY = 215;
	[Export] float JUMP_HOLD_ACCEL = 1.0f;
	[Export] float POP_TIME = 1000;
	[Export] float WALLJUMP_GRACE_TIME = 300;

	public uint id;
	public Vector2 velocity = new Vector2(0, 0);
	public bool jumping = false;
	public bool falling = false;
	public float remainingPoppedTime = 0;
	public float remainingForcedDeadTime = 0;
	public float remainingResurrectTime = 0;
	public bool dead = false;
	public bool killable = true;
	public bool justDied = false;
	public float walljumpGrace = 0;

	public bool hasStarted = false;

	public float timeSinceLastUpdate = 0.0f;

	delegate void ProcessFunc(float delta);

	ProcessFunc process;

	public byte RemoteState {
		get {
			byte state = 0;
			if (jumping)
				state += STATE_JUMPING;
			if (falling)
				state += STATE_FALLING;
			if (dead)
				state += STATE_DEAD;
			if (killable)
				state += STATE_KILLABLE;
			if (justDied)
				state += STATE_JUST_DIED;
			if (hasStarted)
				state += STATE_HAS_STARTED;
			return state;
		}
		set {
			bool lastDead = dead;
			jumping  = (value & STATE_JUMPING) != 0;
			falling  = (value & STATE_FALLING) != 0;
			dead     = (value & STATE_DEAD) != 0;
			killable = (value & STATE_KILLABLE) != 0;
			justDied = (value & STATE_JUST_DIED) != 0;
			hasStarted = (value & STATE_HAS_STARTED) != 0;
			// @Note(sushi): if the remote player just died, set the timer so the animation plays
			if (justDied || (!lastDead && dead)) {
				remainingForcedDeadTime = FORCED_DEAD_TIME;
				remainingResurrectTime = FORCED_DEAD_TIME;
			} else if (!dead) {
				remainingForcedDeadTime = 0;
				remainingResurrectTime = 0;
			}
		}
	}

	public override void _Ready() {
		AddToGroup("players");
		process = RemoteProcess;
	}

	public void Init(uint newId, bool isKing) {
		id = newId;
		if (isKing) {
			GetNode("AnimatedPleb").QueueFree();
			return;
		}
		GetNode("AnimatedKing").QueueFree();
	}

	public void InitLocal(bool isKing) {
		AddToGroup("local_player");
		process = LocalProcess;
		Camera2D camera = new Camera2D();
		// this enables bubble interactions just for the local player
		CollisionMask |= 1 << 1;
		AddChild(camera);
		camera.Zoom = new Vector2(0.5f, 0.5f);
		camera.Current = true;
		ZIndex = 100;
		AnimatedSprite sprite;
		if (isKing) {
			sprite = GetNode("AnimatedKing") as AnimatedSprite;
		} else {
			sprite = GetNode("AnimatedPleb") as AnimatedSprite;
		}
		sprite.Material = GD.Load("res://assets/outline_shader.tres") as Material;
	}

	public void Pop() {
		remainingPoppedTime = POP_TIME;
		velocity.x /= 10;
		velocity.y /= 10;
	}

	public override void _PhysicsProcess(float delta) {
		if (process == null)
			return;
		process(delta);
	}

	// this is only run on the local player
	public void LocalProcess(float delta) {
		// milliseconds
		delta *= 1000;

		float poppedMul = remainingPoppedTime > 0 ? 0.5f - (remainingPoppedTime / POP_TIME / 2.0f) : 1;
		remainingPoppedTime = Math.Max(remainingPoppedTime - delta, 0);
		walljumpGrace -= delta;

		// check spike collisions for death
		bool angled = false;
		if (!dead && killable) {
			for (int i = 0; i < GetSlideCount(); i++) {
				KinematicCollision2D collisions = GetSlideCollision(i);
				if (IsInstanceValid(collisions.Collider) && collisions.Collider.HasMeta("spikes")) {
					dead = true;
					justDied = true;
					killable = false;
					remainingForcedDeadTime = FORCED_DEAD_TIME;
					remainingResurrectTime = FORCED_DEAD_TIME;
				}
			}
		}

		for (int i = 0; i < GetSlideCount(); i++) {
			KinematicCollision2D collisions = GetSlideCollision(i);
			if (IsInstanceValid(collisions.Collider) && collisions.Collider.HasMeta("angled")) {
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
			if (Math.Abs(velocity.x) <= FRICTION * delta * poppedMul) {
				velocity.x = 0;
			} else {
				float frictionSpeed = Math.Max(Math.Abs(velocity.x) - (FRICTION * delta * poppedMul), 0);
				velocity.x = Math.Sign(velocity.x) * frictionSpeed;
			}
		}

		// input acceleration
		if (Math.Abs(velocity.x) < MAX_VELOCITY) {
			velocity.x += accel * delta * poppedMul;

			if (Math.Abs(velocity.x) > MAX_VELOCITY) {
				velocity.x = Math.Sign(velocity.x) * MAX_VELOCITY;
			}
		} else {
			float previousSpeed = Math.Abs(velocity.x);
			velocity.x += accel * delta * poppedMul;

			if (!angled && walljumpGrace <= 0 && Mathf.Abs(accel) < 0.01) {
				float decay = 1 / (1 + (delta * AIR_FRICTION));
				velocity.x *= decay;
			}

			float desiredSpeed = Math.Min(previousSpeed, Math.Abs(velocity.x));
			velocity.x = Math.Sign(velocity.x) * desiredSpeed;
		}

		// jumping
		bool onRightWall = TestMove(Transform, new Vector2(1, -2)) && TestMove(Transform, new Vector2(1, 2));
		bool onLeftWall = TestMove(Transform, new Vector2(-1, -2)) && TestMove(Transform, new Vector2(-1, 2));
		bool onWall = (onRightWall || onLeftWall) && onRightWall != onLeftWall;

		bool canJump = IsOnFloor() || onWall;
		if (jumping && !jump) {
			jumping = false;
		}
		if (jumping && canJump && velocity.y >= 0) {
			jumping = false;
		}

		if (jumping) {
			// if this jump is being held from the last frame reduce gravity
			velocity.y -= Math.Min(Math.Abs(Math.Min(velocity.y, 0)) / JUMP_INSTANT_VELOCITY, 1) * JUMP_HOLD_ACCEL * delta * poppedMul;
		}

		if (!jumping && canJump && jump) {
			velocity.y = Math.Max(velocity.y - (JUMP_INSTANT_VELOCITY * poppedMul), -JUMP_INSTANT_VELOCITY * poppedMul);
			if (onWall && !IsOnFloor()) {
				float direction = onRightWall ? -1.25f : 1.25f;
				velocity.x += direction * JUMP_INSTANT_VELOCITY * poppedMul;
				walljumpGrace = WALLJUMP_GRACE_TIME;
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

			velocity.x += dashDirection[0] * JUMP_INSTANT_VELOCITY * 2;
			velocity.y = Math.Max(Math.Min(velocity.y, 0) + (dashDirection[1] * JUMP_INSTANT_VELOCITY * 2), -JUMP_INSTANT_VELOCITY);
			jumping = true;

			remainingPoppedTime = 0;
		}

		// phase through one way platforms
		if (IsOnFloor() && down) {
			Position = new Vector2(Position[0], Position[1] + 1);
		}

		// gravity
		float gravity = GRAVITY * delta * poppedMul;
		if (velocity.y > 0)
			gravity *= GRAVITY_MULT;
		velocity.y += gravity;
		falling = !IsOnFloor() && velocity.y > EPSILON;

		velocity = MoveAndSlide(velocity, new Vector2(0, -1), false, 4, (float) Math.PI / 16, true);
	}

	// this is only run on remote players
	public void RemoteProcess(float delta) {
		delta *= 1000;

		timeSinceLastUpdate += delta;
		if (timeSinceLastUpdate > 4000) {
			var nodes = GetTree().GetNodesInGroup("scene");
			if (nodes.Count > 0) {
				Scene scene = nodes[0] as Scene;
				scene.FreePlayer(this);
			}
			return;
		}
		if (dead) {
			remainingForcedDeadTime = Math.Max(remainingForcedDeadTime - delta, 0);
			if (remainingForcedDeadTime == 0 && velocity.Length() < EPSILON && remainingResurrectTime > 0) {
				remainingResurrectTime = Math.Max(remainingResurrectTime - delta, 0);
			}
		}
		velocity = MoveAndSlide(velocity, new Vector2(0, -1), false, 4, (float) Math.PI / 16, true);
	}
}

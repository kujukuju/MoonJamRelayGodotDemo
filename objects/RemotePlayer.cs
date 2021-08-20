using Godot;
using System;

public class RemotePlayer : Node2D, IPlayer {
	public const float FORCED_DEAD_TIME = 400;
	public const float EPSILON = 0.000001f;

	public const byte STATE_JUMPING   = 1 << 0;
	public const byte STATE_FALLING   = 1 << 1;
	public const byte STATE_DEAD      = 1 << 2;
	public const byte STATE_KILLABLE  = 1 << 3;
	public const byte STATE_JUST_DIED = 1 << 4;
	public const byte STATE_HAS_STARTED = 1 << 5;

	public uint id { get; set; }
	public Vector2 velocity;
	public Vector2 Velocity {
		get {
			return velocity;
		}
		set {
			velocity = value;
		}
	}
	public bool jumping { get; set; }
	public bool falling { get; set; }
	public float remainingPoppedTime { get; set; }
	public float remainingForcedDeadTime { get; set; }
	public float remainingResurrectTime { get; set; }
	public bool dead { get; set; }
	public bool killable { get; set; }
	public bool justDied { get; set; }
	public float walljumpGrace { get; set; }

	public bool hasStarted { get; set; }

	public float timeSinceLastUpdate { get; set; }

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
	}

	public void Init(uint newId, bool isKing) {
		id = newId;
		AnimatedSprite sprite;
		if (isKing) {
			sprite = GetNode("AnimatedKing") as AnimatedSprite;
		} else {
			sprite = GetNode("AnimatedKing") as AnimatedSprite;
		}
		sprite.QueueFree();
	}

	public override void _PhysicsProcess(float delta) {
		Position += velocity * delta;

		delta *= 1000;

		timeSinceLastUpdate += delta;
		if (timeSinceLastUpdate > 4000) {
			var nodes = GetTree().GetNodesInGroup("scene");
			if (nodes.Count > 0) {
				Scene scene = nodes[0] as Scene;
				scene.FreePlayer(id);
			}
			return;
		}
		if (dead) {
			remainingForcedDeadTime = Math.Max(remainingForcedDeadTime - delta, 0);
			if (remainingForcedDeadTime == 0 && velocity.Length() < EPSILON && remainingResurrectTime > 0) {
				remainingResurrectTime = Math.Max(remainingResurrectTime - delta, 0);
			}
		}
	}
}

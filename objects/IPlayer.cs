using Godot;

public interface IPlayer {
	uint id { get; set; }
	Vector2 Velocity { get; set; }
	bool jumping { get; set; }
	bool falling { get; set; }
	float remainingPoppedTime { get; set; }
	float remainingForcedDeadTime { get; set; }
	float remainingResurrectTime { get; set; }
	bool dead { get; set; }
	bool killable { get; set; }
	bool justDied { get; set; }
	float walljumpGrace { get; set; }

	bool hasStarted { get; set; }

	float timeSinceLastUpdate { get; set; }

	byte RemoteState { get; set; }
}

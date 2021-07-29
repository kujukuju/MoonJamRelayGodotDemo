using Godot;
using System;

public class JumpBubble : AnimatedSprite
{
	const float POP_COOLDOWN = 2000;

	float remainingPoppedTime = 0;

	public override void _Ready()
	{
		Area2D area = GetNode("Area2D") as Area2D;
		area.Connect("body_entered", this, nameof(BodyEntered));
		Playing = true;
	}

	public override void _Process(float delta) {
		delta *= 1000;
		remainingPoppedTime -= delta;
		Visible = remainingPoppedTime < 0;
	}

	public void BodyEntered(Node body) {
		if (remainingPoppedTime > 0)
			return;
		if (!(body is Player player))
			return;
		remainingPoppedTime = POP_COOLDOWN;
		player.Pop();
	}
}

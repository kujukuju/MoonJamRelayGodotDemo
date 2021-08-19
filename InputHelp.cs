using Godot;

public class InputHelp : Node2D
{
	bool isDirJump;

	Sprite jumpSpaceSprite;
	Sprite jumpDirSprite;
	Sprite dashShiftSprite;
	Sprite dashSpaceSprite;

	public override void _Ready() {
		jumpSpaceSprite = GetNode("JumpSpace") as Sprite;
		jumpDirSprite = GetNode("JumpDir") as Sprite;
		dashShiftSprite = GetNode("DashShift") as Sprite;
		dashSpaceSprite = GetNode("DashSpace") as Sprite;
	}

	public override void _UnhandledInput(InputEvent @event) {
		if (@event.IsActionPressed("toggle_input")) {
			InputMap.ActionEraseEvents("jump");
			InputMap.ActionEraseEvents("dash");
			if (!isDirJump) {
				InputEventKey key = new InputEventKey { Scancode = (uint)KeyList.W };
				InputMap.ActionAddEvent("jump", key);
				key = new InputEventKey { Scancode = (uint)KeyList.Up };
				InputMap.ActionAddEvent("jump", key);
				key = new InputEventKey { Scancode = (uint)KeyList.Space };
				InputMap.ActionAddEvent("dash", key);
				key = new InputEventKey { Scancode = (uint)KeyList.Shift };
				InputMap.ActionAddEvent("dash", key);
				jumpSpaceSprite.Visible = false;
				jumpDirSprite.Visible = true;
				dashSpaceSprite.Visible = true;
			} else {
				InputEventKey key = new InputEventKey { Scancode = (uint)KeyList.Space };
				InputMap.ActionAddEvent("jump", key);
				key = new InputEventKey { Scancode = (uint)KeyList.Shift };
				InputMap.ActionAddEvent("dash", key);
				jumpSpaceSprite.Visible = true;
				jumpDirSprite.Visible = false;
				dashSpaceSprite.Visible = false;
			}
			isDirJump = !isDirJump;
		}
	}
}

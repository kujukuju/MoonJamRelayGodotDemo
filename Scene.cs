using System;
using Godot;

public class Scene : Node2D
{
	const float TICK_RATE = 1.0f / 15;

	WebSocketClient socket = new WebSocketClient();
	WebSocketPeer peer;
	// buffer for the send packet
	//  4 : room/game id
	//  4 : player id int
	//  16: 4*float for pos/vel
	byte[] sendBuffer = new byte[4 + sizeof(int) + (4 * sizeof(float))];
	IntToByteLE id = new IntToByteLE();
	float[] playerMovementData = new float[4];
	float accumulator;

	public override void _Ready()
	{
		socket.Connect("connection_established", this, nameof(Connected));
		socket.Connect("connection_closed", this, nameof(Closed));
		socket.Connect("connection_error", this, nameof(Error));
		socket.Connect("data_received", this, nameof(Data));
		socket.Connect("server_close_request", this, nameof(CloseRequest));

		Buffer.BlockCopy("lole".ToUTF8(), 0, sendBuffer, 0, 4);

		id.Value = 69;
		sendBuffer[4] = id.B0;
		sendBuffer[5] = id.B1;
		sendBuffer[6] = id.B2;
		sendBuffer[7] = id.B3;

		Godot.Error attempt = socket.ConnectToUrl("ws://127.0.0.1:58008");
		if (attempt == Godot.Error.Ok) {
			GD.Print("Websocket connected. " + attempt + " " + Name);
		} else {
			GD.Print("Websocket failed to connect. " + attempt);
		}
	}

	public override void _Process(float delta) {
		socket.Poll();

		if (peer == null)
			return;

		accumulator += delta;
		if (accumulator < TICK_RATE)
			return;
		accumulator = 0.0f;
		Player player = GetTree().GetNodesInGroup("players")[0] as Player;
		playerMovementData[0] = player.Position.x;
		playerMovementData[1] = player.Position.y;
		playerMovementData[2] = player.velocity.x;
		playerMovementData[3] = player.velocity.y;
		Buffer.BlockCopy(playerMovementData, 0, sendBuffer, 8, playerMovementData.Length * sizeof(float));
		peer.PutPacket(sendBuffer);
	}

	public void Connected(string protocol) {
		GD.Print("Connected..." + protocol);

		peer = socket.GetPeer(1);
	}

	public void Closed(bool clean) {
		GD.Print("Closed... " + clean);
	}

	public void Error() {
		GD.Print("Error...");
	}

	public void Data() {
		GD.Print("Data...");

		byte[] data = peer.GetPacket();
		id.B0 = data[0];
		id.B1 = data[1];
		id.B2 = data[2];
		id.B3 = data[3];
		Buffer.BlockCopy(data, 4, playerMovementData, 0, data.Length - 4);

		GD.Print(id.Value);
		for(int i = 0; i < playerMovementData.Length; i++) {
			GD.Print(playerMovementData[i]);
		}
	}

	public void CloseRequest(int code, String reason) {
		GD.Print("Close request... " + code + " " + reason);
	}
}

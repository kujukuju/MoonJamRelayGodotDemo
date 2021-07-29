using System;
using System.Collections.Generic;
using Godot;

public class Scene : Node2D {
	const float TICK_RATE = 1.0f / 15;
	// @Todo(sushi): make this load in from a file instead?
	const string LOBBY_KEY = "lole";

	[Export]
	public PackedScene playerScene;

	WebSocketClient socket = new WebSocketClient();
	WebSocketPeer peer;
	// buffer for the send packet
	//  4 : room/game id
	//  4 : player id int
	//  1 : player flags
	//  16: 4*float for pos/vel
	byte[] sendBuffer = new byte[4 + sizeof(int) + 1 + (4 * sizeof(float))];
	UIntToByteLE id = new UIntToByteLE();
	float[] movementBuffer = new float[4];
	float accumulator;

	Vector2 startPos;

	Dictionary<uint, Player> players = new Dictionary<uint, Player>();
	Player myPlayer;

	public override void _Ready() {
		socket.Connect("connection_established", this, nameof(Connected));
		socket.Connect("connection_closed", this, nameof(Closed));
		socket.Connect("connection_error", this, nameof(Error));
		socket.Connect("data_received", this, nameof(Data));
		socket.Connect("server_close_request", this, nameof(CloseRequest));
		GD.Seed(OS.GetUnixTime());

		Position2D start = GetNode("Start") as Position2D;
		startPos = start.Position;

		myPlayer = playerScene.Instance() as Player;
		AddChild(myPlayer);
		myPlayer.Position = startPos;
		myPlayer.InitLocal();

		Buffer.BlockCopy(LOBBY_KEY.ToUTF8(), 0, sendBuffer, 0, 4);

		id.Value = GD.Randi();
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
		movementBuffer[0] = myPlayer.Position.x;
		movementBuffer[1] = myPlayer.Position.y;
		movementBuffer[2] = myPlayer.velocity.x;
		movementBuffer[3] = myPlayer.velocity.y;
		sendBuffer[8] = myPlayer.RemoteState;
		Buffer.BlockCopy(movementBuffer, 0, sendBuffer, 9, movementBuffer.Length * sizeof(float));
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

		// @Fix(sushi): handle concatenated packets. either use specified packet sizes to split,
		//  or start using packet ids so we can expand the demo later with special interactions
		byte[] data = peer.GetPacket();
		if (data.Length == 0 || data.Length % 21 != 0) {
			return;
		}

		id.B0 = data[0];
		id.B1 = data[1];
		id.B2 = data[2];
		id.B3 = data[3];

		// copy the movement floats into the buffer, ignoring the id/state bytes
		Buffer.BlockCopy(data, 5, movementBuffer, 0, data.Length - 5);

		Player player;
		if (!players.ContainsKey(id.Value)) {
			player = playerScene.Instance() as Player;
			players[id.Value] = player;
			AddChild(player);
		} else {
			player = players[id.Value];
		}
		player.RemoteState = data[4];
		player.timeSinceLastUpdate = 0.0f;
		player.Position = new Vector2(movementBuffer[0], movementBuffer[1]);
		player.velocity = new Vector2(movementBuffer[2], movementBuffer[3]);
	}

	public void CloseRequest(int code, String reason) {
		GD.Print("Close request... " + code + " " + reason);
	}
}

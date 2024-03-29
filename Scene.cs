using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;

public class Scene : Node2D {
	const string RELAY_URL = "wss://relay.moonjam.dev/v1";
	const bool MOON = false;
	const string MOON_KEY = "oE7y";
	const string PLEB_KEY = "UO97";

	// with 300mbps~ upload on the relay, we get about 37.5MB/s outgoing traffic
	// the traffic will be measured as such:
	//   BPS = PACKET_SIZE * TICK_PER_SECOND
	//   37500000 = BPS*x*x - BPS*x
	// the positive root will be the theoretical limit for number of players, since
	//  outgoing traffic on the relay grows geometrically, where incoming is linear
	public const float TICK_RATE = 1.0f / 15;
	const int PACKET_SIZE = sizeof(int) + 2 + (4 * sizeof(float));

	[Export] public PackedScene localPlayerScene;
	[Export] public PackedScene remotePlayerScene;
	
	public static long renderTime;

	WebSocketClient socket = new WebSocketClient();
	WebSocketPeer peer;
	// buffer for the send packet
	//  4 : room/game id
	//  4 : player id int
	//  1 : moon flag
	//  1 : player flags
	//  16: 4*float for pos/vel
	// this could be optimized by using a 24-bit id, packing the flags and
	//  into the id as well as using short/half precision values for pos/vel
	// it would bring down the total to 16 vs 26 outgoing, and 12 vs 22 incoming
	byte[] sendBuffer = new byte[4 + PACKET_SIZE];
	UIntToByteLE id = new UIntToByteLE();
	float[] movementBuffer = new float[4];
	float accumulator;

	public Dictionary<uint, RemotePlayer> players = new Dictionary<uint, RemotePlayer>();
	LocalPlayer myPlayer;
	Vector2 lastPos;
	float afkAccumulator;

	Area2D StartArea;
	Area2D EndArea;
	public bool HasStarted;
	PanelContainer HUDContainer;
	PanelContainer DebugContainer;
	Label DebugText;
	float debugAccumulator;
	float debugBytes;

	public NetworkedMultiplayerPeer.ConnectionStatus ConnectionStatus => socket.GetConnectionStatus();

	public Scene() {
		AddToGroup("scene");
	}

	public override void _Ready() {
		socket.Connect("connection_established", this, nameof(Connected));
		socket.Connect("connection_closed", this, nameof(Closed));
		socket.Connect("connection_error", this, nameof(Error));
		socket.Connect("data_received", this, nameof(Data));
		socket.Connect("server_close_request", this, nameof(CloseRequest));
		GD.Seed(OS.GetUnixTime());

		HUDContainer = GetNode("HUD/PanelContainer") as PanelContainer;
		InitDebugContainer();

		// try loading moon's lobby key first
		string key = MOON_KEY;
		id.Value = GD.Randi();

		// if we don't find it, then join as a pleb
		if (!MOON) {
			key = PLEB_KEY;
		}

		// set the first 4 bytes of the buffer to the lobby key
		Buffer.BlockCopy(key.ToUTF8(), 0, sendBuffer, 0, 4);
		// set the next 4 bytes of the buffer to the current player's id
		sendBuffer[4] = id.B0;
		sendBuffer[5] = id.B1;
		sendBuffer[6] = id.B2;
		sendBuffer[7] = id.B3;
		// the next 1 byte is reserved for the flag of whether or not this is moonmoon
		sendBuffer[8] = MOON ? (byte) 1 : (byte) 0;

		ConnectToRelay();

		Position2D start = GetNode("Level/Start") as Position2D;
		myPlayer = localPlayerScene.Instance() as LocalPlayer;
		AddChild(myPlayer);
		myPlayer.Position = start.Position;
		myPlayer.Init(id.Value, MOON);

		StartArea = GetNode("StartArea") as Area2D;
		if (MOON)
			StartArea.Connect("body_entered", this, nameof(StartArea_Entered));
		EndArea = GetNode("EndArea") as Area2D;
		EndArea.Connect("body_entered", this, nameof(EndArea_Entered));
	}

	private void ConnectToRelay() {
		peer = null;
		Error attempt = socket.ConnectToUrl(RELAY_URL);
		if (attempt == Godot.Error.Ok) {
			GD.Print("Websocket connected. " + attempt);
		} else {
			GD.Print("Websocket failed to connect. " + attempt);
		}
	}

	private string LoadKey(string keyFile) {
		using(File file = new File()) {
			if (!file.FileExists(keyFile))
				return null;
			file.Open(keyFile, File.ModeFlags.Read);
			return file.GetLine().StripEdges();
		}
	}

	private void StartArea_Entered(Node body) {
		if (!(body is LocalPlayer player)) {
			return;
		}
		player.hasStarted = true;
		HasStarted = true;
	}

	private void EndArea_Entered(Node body) {
		Timer timer = GetNode("HUD/StopwatchContainer") as Timer;
		timer.SetPhysicsProcess(false);
	}

	public override void _Process(float delta) {
		renderTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		
		socket.Poll();
		ResizePlayerCount();

		switch (socket.GetConnectionStatus()) {
			case NetworkedMultiplayerPeer.ConnectionStatus.Connecting:
				return;
			case NetworkedMultiplayerPeer.ConnectionStatus.Disconnected:
				// @Note(sushi): try to reconnect every 15 seconds while disconnected
				accumulator += delta;
				if (accumulator > 15.0f) {
					ConnectToRelay();
					accumulator = 0;
				}
				return;
		}

		if (peer == null)
			return;

		UpdateDebugPanel(delta);

		accumulator += delta;

		// @Note(sushi): this reduces the updates to be once a second if the player
		//  has been standing in 1 spot for too long
		if ((lastPos - myPlayer.Position).LengthSquared() < 1) {
			afkAccumulator += delta;
			if (afkAccumulator > 2) {
				afkAccumulator = 1;
				SendPacket();
			}
			if (afkAccumulator >= 1) {
				return;
			}
		} else {
			afkAccumulator = 0;
		}

		if (accumulator < TICK_RATE)
			return;
		accumulator = 0.0f;
		lastPos = myPlayer.Position;
		SendPacket();
	}

	private void SendPacket() {
		// populate the movementBuffer with our current pos/vel
		movementBuffer[0] = myPlayer.Position.x;
		movementBuffer[1] = myPlayer.Position.y;
		movementBuffer[2] = myPlayer.velocity.x;
		movementBuffer[3] = myPlayer.velocity.y;
		sendBuffer[9] = myPlayer.RemoteState;
		// copy the movementBuffer bytes to the sendBuffer and push it
		Buffer.BlockCopy(movementBuffer, 0, sendBuffer, 10, movementBuffer.Length * sizeof(float));
		peer.PutPacket(sendBuffer);
		myPlayer.justDied = false;
	}

	public void Connected(string protocol) {
		GD.Print("Connected..." + protocol);
		peer = socket.GetPeer(1);
	}

	public void Closed(bool clean) {
		GD.Print("Closed... " + clean);
		peer = null;
	}

	public void Error() {
		GD.Print("Error...");
		peer = null;
	}

	public void Data() {
		// @Todo(sushi): start using a packet processor so we can expand the demo later with special interactions
		byte[] data = peer.GetPacket();
		// @Note(sushi): this breaks if somebody starts sending packets that aren't the expected size
		if (data.Length == 0 || data.Length % PACKET_SIZE != 0) {
			return;
		}
		debugBytes += data.Length;

		for (int offset = 0; offset < data.Length; offset += PACKET_SIZE) {
			// read in the remote player's id
			id.B0 = data[offset + 0];
			id.B1 = data[offset + 1];
			id.B2 = data[offset + 2];
			id.B3 = data[offset + 3];
			bool isMoon = data[offset + 4] == 1;

			// check if the player exists, otherwise instance a new one
			RemotePlayer player;
			if (!players.ContainsKey(id.Value)) {
				player = remotePlayerScene.Instance() as RemotePlayer;
				players[id.Value] = player;
				AddChild(player);
				player.Init(id.Value, isMoon);
			} else {
				player = players[id.Value];
			}
			if (!IsInstanceValid(player)) {
				return;
			}

			// update the state for animations, and time so it isn't deleted
			player.RemoteState = data[offset + 5];
			player.timeSinceLastUpdate = 0.0f;

			// copy the movement floats into the buffer, ignoring the id/state bytes
			Buffer.BlockCopy(data, offset + 6, movementBuffer, 0, movementBuffer.Length * 4);

			player.positions[0] = player.positions[1];
			player.positions[1] = new Vector2(movementBuffer[0], movementBuffer[1]);
			player.velocities[0] = player.velocities[1];
			player.velocities[1] = new Vector2(movementBuffer[2], movementBuffer[3]);
			player.Velocity = player.velocities[1];
			player.times[0] = player.times[1];
			player.times[1] = renderTime;

			if (player.hasStarted)
				HasStarted = true;
		}
	}

	public void CloseRequest(int code, String reason) {
		GD.Print("Close request... " + code + " " + reason);
	}

	public void FreePlayer(uint playerId) {
		Node2D player = null;
		if (players.ContainsKey(playerId)) {
			player = players[playerId];
			players.Remove(playerId);
		}
		if (IsInstanceValid(player) && !player.IsQueuedForDeletion()) {
			player.QueueFree();
		}
	}

	private void ResizePlayerCount() {
		HUDContainer.RectSize = Vector2.Zero;
	}

	[Conditional("DEBUG")]
	private void InitDebugContainer()
	{
		DebugContainer = GetNode("HUD/DebugContainer") as PanelContainer;
		DebugText = DebugContainer.GetNode("DebugText") as Label;
		DebugContainer.Visible = true;
	}

	[Conditional("DEBUG")]
	private void UpdateDebugPanel(float delta) {
		debugAccumulator += delta;
		if (debugAccumulator < 1)
			return;
		debugAccumulator -= 1;
		if (debugBytes > 1000)
			DebugText.Text = $"{debugBytes / 1000:0.0}kB/s";
		else
			DebugText.Text = $"{debugBytes}B/s";
		DebugContainer.RectSize = Vector2.Zero;
		debugBytes = 0;
	}
}

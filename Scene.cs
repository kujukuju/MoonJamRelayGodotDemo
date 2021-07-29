using Godot;
using System;

public class Scene : Node2D {
	WebSocketClient socket = new WebSocketClient();
	
	public override void _Ready() {
		socket.Connect("connection_established", this, nameof(Connected));
		socket.Connect("connection_closed", this, nameof(Closed));
		socket.Connect("connection_error", this, nameof(Error));
		socket.Connect("data_received", this, nameof(Data));
		socket.Connect("server_close_request", this, nameof(CloseRequest));
		
		Godot.Error attempt = socket.ConnectToUrl("ws://127.0.0.1:58008");
		if (attempt == Godot.Error.Ok) {
			GD.Print("Websocket connected. " + attempt);
		} else {
			GD.Print("Websocket failed to connect. " + attempt);
		}
	}
	
	public void Connected() {
		GD.Print("Connected...");
	}
	
	public void Closed(bool clean) {
		GD.Print("Closed... " + clean);
	}
	
	public void Error() {
		GD.Print("Error...");
	}
	
	public void Data() {
		GD.Print("Data...");
	}
	
	public void CloseRequest(int code, String reason) {
		GD.Print("Close request... " + code + " " + reason);
	}
}

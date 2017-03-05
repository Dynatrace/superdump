using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WebSocketManager;
using WebSocketManager.Common;

namespace SuperDump.Webterm {
	public class WebTermHandler : WebSocketHandler {
		private Dictionary<string, ConsoleAppManager> socketIdToProcess = new Dictionary<string, ConsoleAppManager>();
		private Dictionary<ConsoleAppManager, string> processToSocketId = new Dictionary<ConsoleAppManager, string>();

		public WebTermHandler(WebSocketConnectionManager webSocketConnectionManager) : base(webSocketConnectionManager) {
		}

		private void Mgr_ErrorTextReceived(object sender, string e) {
			Console.WriteLine("e>>>" + e + "<<<e");
			string socketId;
			if (processToSocketId.TryGetValue(sender as ConsoleAppManager, out socketId)) {
				SendToClient(socketId, null, e).Wait();
			}
		}

		private void Mgr_ProcessExited(object sender, EventArgs e) {
			Console.WriteLine(">>>exit<<<");
		}

		private void Mgr_StandartTextReceived(object sender, string e) {
			Console.WriteLine(">>>" + e + "<<<");
			string socketId;
			if (processToSocketId.TryGetValue(sender as ConsoleAppManager, out socketId)) {
				SendToClient(socketId, e, null).Wait();
			}
		}

		public override async Task OnConnected(WebSocket socket) {
			await base.OnConnected(socket);

			var socketId = WebSocketConnectionManager.GetId(socket);

			var message = new Message() {
				MessageType = MessageType.Text,
				Data = "{ \"Output\": \"connected\", \"Error\": \"\" }"
			};

			System.Console.WriteLine("new connection");

			var mgr = new ConsoleAppManager($@"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe");
			socketIdToProcess[socketId] = mgr;
			processToSocketId[mgr] = socketId;
			mgr.StandartTextReceived += Mgr_StandartTextReceived;
			mgr.ErrorTextReceived += Mgr_ErrorTextReceived;
			mgr.ProcessExited += Mgr_ProcessExited;

			string dumpPath = @"C:\workspaces\superdump\data\dumps\oad0312\uvl6925\unhandledex-64bit-net4.6.1080.dmp";
			mgr.ExecuteAsync($"-z {dumpPath}");

			await SendMessageToAllAsync(message);
		}

		public async Task ReceiveMessage(string socketId, string input) {
			System.Console.WriteLine($"ReceiveMessage ({socketId}): {input}");
			socketIdToProcess[socketId].Write(input);
		}

		public async Task SendToClient(string socketId, string output, string error) {
			System.Console.WriteLine($"SendToClient ({socketId}): {output}");
			await InvokeClientMethodAsync(socketId, "receiveMessage", new object[] {
				new {
					Output = output,
					Error = error
				}}
			);
		}

		public override async Task OnDisconnected(WebSocket socket) {
			var socketId = WebSocketConnectionManager.GetId(socket);

			await base.OnDisconnected(socket);

			var message = new Message() {
				MessageType = MessageType.Text,
				Data = "{'Output': 'disconnected', 'Error': ''}"
			};

			var mgr = socketIdToProcess[socketId];
			mgr.Kill();
			processToSocketId.Remove(mgr);
			socketIdToProcess.Remove(socketId);

			System.Console.WriteLine("disconnected");
			await SendMessageToAllAsync(message);
		}
	}
}
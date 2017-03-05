using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WebSocketManager;
using WebSocketManager.Common;
using SuperDumpService.Services;

namespace SuperDump.Webterm {
	public class WebTermHandler : WebSocketHandler {
		private Dictionary<string, ConsoleAppManager> socketIdToProcess = new Dictionary<string, ConsoleAppManager>();
		private Dictionary<ConsoleAppManager, string> processToSocketId = new Dictionary<ConsoleAppManager, string>();
		private DumpRepository dumpRepo;

		public WebTermHandler(WebSocketConnectionManager webSocketConnectionManager, DumpRepository dumpRepo) : base(webSocketConnectionManager) {
			this.dumpRepo = dumpRepo;
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

		private void StartCdb(string socketId, string dumpPath, bool is64Bit) {
			string cdbPath;
			if (is64Bit) {
				cdbPath = @"C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe";
			} else {
				cdbPath = @"C:\Program Files (x86)\Windows Kits\10\Debuggers\x86\cdb.exe";
			}
			var mgr = new ConsoleAppManager(cdbPath);
			socketIdToProcess[socketId] = mgr;
			processToSocketId[mgr] = socketId;
			mgr.StandartTextReceived += Mgr_StandartTextReceived;
			mgr.ErrorTextReceived += Mgr_ErrorTextReceived;
			mgr.ProcessExited += Mgr_ProcessExited;
			mgr.ExecuteAsync($"-z {dumpPath}");

			mgr.WriteLine(".cordll -ve -u -l"); // load DAC and SOS
		}

		public void ReceiveMessage(string socketId, string input) {
			System.Console.WriteLine($"ReceiveMessage ({socketId}): {input}");
			socketIdToProcess[socketId].Write(input);
		}

		public void StartSession(string socketId, string bundleId, string dumpId) {
			System.Console.WriteLine($"StartSession ({socketId}): {bundleId}, {dumpId}");
			var dumpInfo = dumpRepo.Get(bundleId, dumpId);
			bool is64bit = dumpInfo.Is64Bit.HasValue ? dumpInfo.Is64Bit.Value : true; // default to 64 bit in case it's not known.
			StartCdb(socketId, dumpRepo.GetDumpFilePath(bundleId, dumpId), is64bit);
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
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WebSocketManager;
using WebSocketManager.Common;
using SuperDumpService.Services;
using SuperDumpService;
using Microsoft.Extensions.Options;

namespace SuperDump.Webterm {
	public class WebTermHandler : WebSocketHandler {
		private Dictionary<string, ConsoleAppManager> socketIdToProcess = new Dictionary<string, ConsoleAppManager>();
		private Dictionary<ConsoleAppManager, string> processToSocketId = new Dictionary<ConsoleAppManager, string>();
		private DumpRepository dumpRepo;
		private IOptions<SuperDumpSettings> settings;

		public WebTermHandler(WebSocketConnectionManager webSocketConnectionManager, DumpRepository dumpRepo, IOptions<SuperDumpSettings> settings) : base(webSocketConnectionManager) {
			this.dumpRepo = dumpRepo;
			this.settings = settings;
		}

		private void Mgr_ErrorTextReceived(object sender, string e) {
			string socketId;
			if (processToSocketId.TryGetValue(sender as ConsoleAppManager, out socketId)) {
				SendToClient(socketId, null, e).Wait();
			}
		}

		private void Mgr_StandartTextReceived(object sender, string e) {
			string socketId;
			if (processToSocketId.TryGetValue(sender as ConsoleAppManager, out socketId)) {
				SendToClient(socketId, e, null).Wait();
			}
		}

		private void StartCdb(string socketId, string dumpPath, bool is64Bit) {
			string cdbPath;
			if (is64Bit) {
				cdbPath = settings.Value.Cdbx64;
			} else {
				cdbPath = settings.Value.Cdbx86;
			}
			var mgr = new ConsoleAppManager(cdbPath);
			socketIdToProcess[socketId] = mgr;
			processToSocketId[mgr] = socketId;
			mgr.StandartTextReceived += Mgr_StandartTextReceived;
			mgr.ErrorTextReceived += Mgr_ErrorTextReceived;
			mgr.ExecuteAsync($"-z {dumpPath}");

			mgr.WriteLine(".cordll -ve -u -l"); // load DAC and SOS
		}

		public void ReceiveMessage(string socketId, string input) {
			socketIdToProcess[socketId].Write(input);
		}

		public void StartSession(string socketId, string bundleId, string dumpId) {
			System.Console.WriteLine($"StartSession ({socketId}): {bundleId}, {dumpId}");
			if (string.IsNullOrEmpty(bundleId) || string.IsNullOrEmpty(dumpId)) {
				return;
			}
			var dumpInfo = dumpRepo.Get(bundleId, dumpId);
			bool is64bit = dumpInfo.Is64Bit.HasValue ? dumpInfo.Is64Bit.Value : true; // default to 64 bit in case it's not known.
			StartCdb(socketId, dumpRepo.GetDumpFilePath(bundleId, dumpId), is64bit);
		}

		public async Task SendToClient(string socketId, string output, string error) {
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
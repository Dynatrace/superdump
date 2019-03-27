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
using System.IO;
using SuperDumpService.Helpers;
using System.Net;
using SuperDump.Models;
using SuperDumpService.Models;

namespace SuperDump.Webterm {
	public class WebTermHandler : WebSocketHandler {
		private Dictionary<string, ConsoleAppManager> socketIdToProcess = new Dictionary<string, ConsoleAppManager>();
		private Dictionary<ConsoleAppManager, string> processToSocketId = new Dictionary<ConsoleAppManager, string>();
		private SuperDumpRepository superdumpRepo;
		private DumpRepository dumpRepo;
		private IOptions<SuperDumpSettings> settings;

		public WebTermHandler(WebSocketConnectionManager webSocketConnectionManager, SuperDumpRepository superdumpRepo, DumpRepository dumpRepo, IOptions<SuperDumpSettings> settings) : base(webSocketConnectionManager) {
			this.superdumpRepo = superdumpRepo;
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

		private ConsoleAppManager StartCdb(string socketId, DirectoryInfo workingDir, FileInfo dumpPath, bool is64Bit, DumpIdentifier id) {
			string command = is64Bit ? settings.Value.WindowsInteractiveCommandx64 : settings.Value.WindowsInteractiveCommandx86;
			if (string.IsNullOrEmpty(command)) throw new ArgumentException("WindowsInteractiveCommandx86/X64 not set.");
			return RunConsoleApp(socketId, workingDir, dumpPath, command, id);
		}

		private ConsoleAppManager RunConsoleApp(string socketId, DirectoryInfo workingDir, FileInfo dumpPath, string command, DumpIdentifier id) {

			command = command.Replace("{bundleid}", id.BundleId);
			command = command.Replace("{dumpid}", id.DumpId);
			command = command.Replace("{dumppath}", dumpPath?.FullName);
			command = command.Replace("{dumpname}", dumpPath?.Name);
			command = command.Replace("{dumpdir}", workingDir?.FullName);

			Utility.ExtractExe(command, out string executable, out string arguments);

			var mgr = new ConsoleAppManager(executable, workingDir);
			socketIdToProcess[socketId] = mgr;
			processToSocketId[mgr] = socketId;
			mgr.StandartTextReceived += Mgr_StandartTextReceived;
			mgr.ErrorTextReceived += Mgr_ErrorTextReceived;
			mgr.ExecuteAsync(arguments);
			return mgr;
		}

		public void ReceiveMessage(string socketId, string input) {
			try {
				socketIdToProcess[socketId].Write(input);
			} catch (Exception e) {
				Console.WriteLine($"Error in ReceiveMessage: {e}");
			}
		}

		// called by WebSocketManager
		public void StartSession(string socketId, string bundleId, string dumpId, string initialCommand) {
			var id = DumpIdentifier.Create(bundleId, dumpId);
			try {
				System.Console.WriteLine($"StartSession ({socketId}): {id}");
				if (string.IsNullOrEmpty(id.BundleId) || string.IsNullOrEmpty(id.DumpId)) {
					return;
				}
				var dumpInfo = dumpRepo.Get(id);
				var dumpFilePath = dumpRepo.GetDumpFilePath(id);
				var dumpFilePathInfo = dumpFilePath != null ? new FileInfo(dumpFilePath) : null;
				var workingDirectory = dumpFilePathInfo?.Directory;

				var sdResult = dumpRepo.GetResult(id).Result;
				bool is64bit = sdResult?.SystemContext.ProcessArchitecture.Contains("64") ?? true; // default to 64 bit in case it's not known
				ConsoleAppManager mgr = null;
				var initialCommands = new List<string>();
				if (dumpInfo.DumpFileName.EndsWith(".dmp", StringComparison.OrdinalIgnoreCase)) {
					mgr = StartCdb(socketId, workingDirectory, dumpFilePathInfo, is64bit, id);
					initialCommands.Add(".cordll -ve -u -l"); // load DAC and SOS
				} else {
					throw new NotSupportedException($"file extension of '{dumpInfo.DumpFileName}' not supported for interactive mode.");
				}
				if (mgr != null && !string.IsNullOrEmpty(initialCommand)) {
					initialCommands.Add(WebUtility.UrlDecode(initialCommand));
				}
				RunInitialCommandsAsync(socketId, mgr, initialCommands);
			} catch (Exception e) {
				Console.WriteLine($"Error in StartSession: {e}");
			}
		}

		private void RunInitialCommandsAsync(string socketId, ConsoleAppManager mgr, List<string> initialCommands) {
			Task.Run(async () => {
				await Task.Delay(1000); // that's pretty ugly. we actually would need to wait until the console is ready for input, then run this command.
				foreach (var cmd in initialCommands) {
					await WriteLineAndTellClient(socketId, cmd, mgr);
					await Task.Delay(1000); // that's pretty ugly. we actually would need to wait until the console is ready for input, then run this command.
				}
			});
		}

		private async Task WriteLineAndTellClient(string socketId, string line, ConsoleAppManager mgr) {
			await SendToClient(socketId, line + "\n", null);
			mgr.WriteLine(line);
		}

		public async Task SendToClient(string socketId, string output, string error) {
			try {
				await InvokeClientMethodAsync(socketId, "receiveMessage", new object[] {
					new {
						Output = output,
						Error = error
					}}
				);
			} catch (Exception e) {
				Console.WriteLine($"Error in SendToClient: {e}");
			}
		}

		public override async Task OnDisconnected(WebSocket socket) {
			try {
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
			} catch (Exception e) {
				Console.WriteLine($"Error in OnDisconnected: {e}");
			}
		}
	}
}
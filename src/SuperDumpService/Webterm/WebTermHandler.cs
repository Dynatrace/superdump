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

		private ConsoleAppManager StartCdb(string socketId, DirectoryInfo workingDir, FileInfo dumpPath, bool is64Bit, string bundleId, string dumpId) {
			string command = is64Bit ? settings.Value.WindowsInteractiveCommandx64 : settings.Value.WindowsInteractiveCommandx86;
			if (string.IsNullOrEmpty(command)) throw new ArgumentException("WindowsInteractiveCommandx86/X64 not set.");
			return RunConsoleApp(socketId, workingDir, dumpPath, command, bundleId, dumpId);
		}

		private ConsoleAppManager StartGdb(string socketId, DirectoryInfo workingDir, FileInfo dumpPath, bool is64Bit, string bundleId, string dumpId) {
			string command = settings.Value.LinuxInteractiveCommand;
			if (string.IsNullOrEmpty(command)) throw new ArgumentException("LinuxInteractiveCommand not set.");
			return RunConsoleApp(socketId, workingDir, dumpPath, command, bundleId, dumpId);
		}

		private ConsoleAppManager RunConsoleApp(string socketId, DirectoryInfo workingDir, FileInfo dumpPath, string command, string bundleId, string dumpId) {

			command = command.Replace("{bundleid}", bundleId);
			command = command.Replace("{dumpid}", dumpId);
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
			try {
				System.Console.WriteLine($"StartSession ({socketId}): {bundleId}, {dumpId}");
				if (string.IsNullOrEmpty(bundleId) || string.IsNullOrEmpty(dumpId)) {
					return;
				}
				var dumpInfo = dumpRepo.Get(bundleId, dumpId);
				var dumpFilePath = dumpRepo.GetDumpFilePath(bundleId, dumpId);
				var dumpFilePathInfo = dumpFilePath != null ? new FileInfo(dumpFilePath) : null;
				var workingDirectory = dumpFilePathInfo?.Directory;

				bool is64bit = dumpInfo.Is64Bit.HasValue ? dumpInfo.Is64Bit.Value : true; // default to 64 bit in case it's not known.
				ConsoleAppManager mgr = null;
				var initialCommands = new List<string>();
				if (dumpInfo.DumpFileName.EndsWith(".dmp", StringComparison.OrdinalIgnoreCase)) {
					mgr = StartCdb(socketId, workingDirectory, dumpFilePathInfo, is64bit, bundleId, dumpId);
					initialCommands.Add(".cordll -ve -u -l"); // load DAC and SOS
				} else if (dumpInfo.DumpFileName.EndsWith(".core.gz", StringComparison.OrdinalIgnoreCase)) {
					mgr = StartGdb(socketId, workingDirectory, dumpFilePathInfo, is64bit, bundleId, dumpId);
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
using SuperDump.Models;
using SuperDumpModels;
using System;
using System.Collections.Generic;

namespace SuperDump.Analyzer.Linux {

	public class GdbOutputParser {
		private enum State { SKIPPING, THREAD, STACKFRAME, ARGS, LOCALS };

		private SDResult analysisResult;

		private State state;
		private uint activeThread;
		private int activeFrame;

		public GdbOutputParser(SDResult analysisResult) {
			this.analysisResult = analysisResult;
		}

		public void Parse(string gdb) {
			state = State.SKIPPING;
			activeThread = 0;
			activeFrame = 0;
			ParseGdbLines(gdb.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));
		}

		private void ParseGdbLines(string[] lines) {
			for(int i = 0; i < lines.Length; i++) {
				switch (state) {
					case State.SKIPPING:
						if (lines[i].Contains(">>thread")) {
							int lastSpace = lines[i].LastIndexOf(' ');
							activeThread = UInt32.Parse(lines[i].Substring(lastSpace));
							state = State.THREAD;
						}
						break;
					case State.THREAD:
						if (lines[i].EndsWith(">>finish thread")) {
							state = State.SKIPPING;
						} else if(lines[i].Contains(">>select")) {
							int lastSpace = lines[i].LastIndexOf(' ');
							activeFrame = Int32.Parse(lines[i].Substring(lastSpace));
							state = State.STACKFRAME;
						}
						break;
					case State.STACKFRAME:
						if (lines[i].EndsWith(">>info args")) {
							state = State.ARGS;
						} else if (lines[i].EndsWith(">>info locals")) {
							state = State.LOCALS;
						} else if (lines[i].EndsWith(">>finish frame")) {
							state = State.THREAD;
						}
						break;
					case State.ARGS:
						if (lines[i].Contains("=")) {
							string fullContent = lines[i];
							// add following lines if they start with a whitespace
							for (int ii = i+1; ii < lines.Length && lines[ii].StartsWith(" "); ii++, i++) {
								if (lines[ii].StartsWith(" ")) {
									fullContent += " " + lines[ii].Trim();
								}
							}
							SetArgument(fullContent);
						} else if (lines[i].EndsWith(">>finish frame")) {
							state = State.THREAD;
						} else if(lines[i].EndsWith(">>info locals")) {
							state = State.LOCALS;
						}
						break;
					case State.LOCALS:
						if(lines[i].Contains("=")) {
							string fullContent = lines[i];
							// add following lines if they start with a whitespace
							for (int ii = i + 1; ii < lines.Length && lines[ii].StartsWith(" "); ii++, i++) {
								if (lines[ii].StartsWith(" ")) {
									fullContent += " " + lines[ii].Trim();
								}
							}
							SetLocal(fullContent);
						} else if(lines[i].EndsWith(">>finish frame")) {
							state = State.THREAD;
						} else if(lines[i].EndsWith(">>info args")) {
							state = State.ARGS;
						}
						break;
				}
			}
		}

		private void SetArgument(string line) {
			KeyValuePair<string, string> keyValue = ParseVarInfo(line);
			if (analysisResult.ThreadInformation[activeThread].StackTrace[activeFrame] is SDCDCombinedStackFrame frame) {
				try {
					frame.Args.Add(keyValue);
				} catch(ArgumentException e) {
					// Sometimes, the same argument is printed twice. GDB bug?
					Console.WriteLine("Failed to add key! " + e.Message);
				}
			} else {
				throw new InvalidCastException("Invalid stackframe type! Use SDCD prefix for declaring stackframes!");
			}
		}

		private void SetLocal(string line) {
			KeyValuePair<string, string> keyValue = ParseVarInfo(line);
			if (analysisResult.ThreadInformation[activeThread].StackTrace[activeFrame] is SDCDCombinedStackFrame frame) {
				frame.Locals.Add(keyValue);
			} else {
				throw new InvalidCastException("Invalid stackframe type! Use SDCD prefix for declaring stackframes!");
			}
		}

		private KeyValuePair<string,string> ParseVarInfo(string line) {
			int indexEquals = line.IndexOf("=");
			if(indexEquals > 0) {
				string key = line.Substring(0, indexEquals - 1);
				if(key.StartsWith("(gdb) ")) {
					key = key.Substring(6);
				}
				return new KeyValuePair<string, string>(key.Trim(), line.Substring(indexEquals + 1).Trim());
			}
			throw new ArgumentException("Illegal assignment in gdb output: " + line);
		}
	}
}

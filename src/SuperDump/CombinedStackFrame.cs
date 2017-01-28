using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Interop;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SuperDump.Models;
using SuperDumpModels;
using System;
using System.IO;
using System.Text;

namespace SuperDump {
	/// <summary>
	/// Represents a frame of a thread's stack trace, can be native and managed
	/// </summary>
	[Serializable]
	public class CombinedStackFrame : IEquatable<CombinedStackFrame>, ISerializableJson {
		[JsonConverter(typeof(StringEnumConverter))]
		public StackFrameType Type { get; set; }

		public string ModuleName { get; set; } = string.Empty;
		public string MethodName { get; set; } = string.Empty;
		public ulong OffsetInMethod { get; set; }
		public ulong InstructionPointer { get; set; }
		public ulong StackPointer { get; set; }
		public ulong ReturnOffset { get; set; }

		public CombinedStackFrame LinkedStackFrame { get; set; }
		public SDFileAndLineNumber SourceInfo { get; set; }

		// source location support, not implemented yet!
		//public string SourceFileName { get; set; }
		//public uint SourceLineNumber { get; set; }
		//public uint SourceLineNumberEnd { get; set; }
		//public uint SourceColumnNumber { get; set; }
		//public uint SourceColumnNumberEnd { get; set; }

		/// <summary>
		/// Constructor for native frame
		/// </summary>
		/// <param name="frame"></param>
		/// <param name="debugSymbols"></param>
		public CombinedStackFrame(DEBUG_STACK_FRAME frame, IDebugSymbols3 debugSymbols) {
			Type = StackFrameType.Native; //native frame
			InstructionPointer = frame.InstructionOffset;
			StackPointer = frame.StackOffset;
			ReturnOffset = frame.ReturnOffset;

			uint moduleIndex;
			ulong dummy;

			//if no module is associated with this frame, the method is unknown, can happen with CLR-JIT code
			if (debugSymbols.GetModuleByOffset(InstructionPointer, 0, out moduleIndex, out dummy) != 0) {
				ModuleName = "UNKNOWN";
				MethodName = "UNKNOWN";
				return; //no more action needed
			}
			var name = new StringBuilder(1024); //create buffer for storing method name
			ulong displacement;
			uint nameSize;
			Utility.CheckHRESULT(debugSymbols.GetNameByOffset(InstructionPointer, name, name.Capacity, out nameSize, out displacement));
			OffsetInMethod = displacement; //offset of InstructionPointer and base location of symbol, could be null

			//get module name and method name, example for name : 
			string[] parts = name.ToString().Split('!');
			ModuleName = parts[0]; //module is the first part before '!'
			if (parts.Length > 1) {
				MethodName = parts[1]; //second part, after the '!'
			}
			if (string.IsNullOrEmpty(ModuleName)) {
				ModuleName = "UNKNOWN";
			}
			// check if method name was found or not
			if (string.IsNullOrEmpty(MethodName)) {
				MethodName = "UNKNOWN";
			}

			// get source information
			uint line;
			var fileBuffer = new StringBuilder(4 * 1024);
			uint fileSize;
			debugSymbols.GetLineByOffset(InstructionPointer, out line, fileBuffer, fileBuffer.Capacity, out fileSize, out displacement);
			if (fileSize > 0) {
				this.SourceInfo = new SDFileAndLineNumber { File = fileBuffer.ToString(), Line = (int)line };
			}
		}

		public CombinedStackFrame(ClrStackFrame frame) {
			if (frame.Kind == ClrStackFrameType.ManagedMethod) {
				Type = StackFrameType.Managed;
			}
			if (frame.Kind == ClrStackFrameType.Runtime) {
				Type = StackFrameType.Special;
			}

			InstructionPointer = frame.InstructionPointer;
			StackPointer = frame.StackPointer;

			if (frame.Method == null) {
				MethodName = frame.DisplayString; //for example GCFrame
				return;
			}

			MethodName = frame.Method.GetFullSignature();
			if (frame.Method.Type != null) {
				ModuleName = Path.GetFileNameWithoutExtension(frame.Method.Type.Module.Name);
				if (string.IsNullOrEmpty(ModuleName)) {
					ModuleName = "UNKNOWN";
				}
			}

			// calculate IL offset with instruction pointer of frame and instruction pointer 
			// in the target dump file of the start of the method's assembly
			OffsetInMethod = InstructionPointer - frame.Method.NativeCode;

			this.SourceInfo = frame.GetSourceLocation();
		}

		[JsonConstructor]
		public CombinedStackFrame(StackFrameType type, string moduleName, string methodName,
			ulong offsetInMethod, ulong instructionPointer, ulong stackPointer,
			ulong returnOffset, CombinedStackFrame linkedFrame) {
			this.Type = type;
			this.ModuleName = moduleName;
			this.MethodName = methodName;
			this.OffsetInMethod = offsetInMethod;
			this.InstructionPointer = instructionPointer;
			this.StackPointer = stackPointer;
			this.ReturnOffset = returnOffset;
			this.LinkedStackFrame = linkedFrame;
		}

		public bool Equals(CombinedStackFrame other) {
			bool equals = false;
			if (this.Type.Equals(other.Type)
				&& this.InstructionPointer.Equals(other.InstructionPointer)
				&& this.MethodName.Equals(other.MethodName)
				&& this.ModuleName.Equals(other.ModuleName)
				&& this.OffsetInMethod.Equals(other.OffsetInMethod)
				&& this.ReturnOffset.Equals(other.ReturnOffset)
				&& this.StackPointer.Equals(other.StackPointer)) {
				if (this.LinkedStackFrame == null && other.LinkedStackFrame == null) {
					equals = true;
				} else if (this.LinkedStackFrame == null && other.LinkedStackFrame != null) {
					equals = false;
				} else if (this.LinkedStackFrame.Equals(other.LinkedStackFrame)) {
					equals = true;
				} else {
					equals = false;
				}
			}
			return equals;
		}

		public string SerializeToJSON() {
			return JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings {
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore
			});
		}

		public override string ToString() {
			return $"{this.ModuleName}!{this.MethodName}";
		}
	}
}

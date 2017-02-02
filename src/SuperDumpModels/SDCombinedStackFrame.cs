using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SuperDumpModels;
using System;
using System.Collections.Generic;
using System.IO;

namespace SuperDump.Models {
	public enum StackFrameType {
		Managed,
		Native,
		Special
	}

	public class SDCombinedStackFrame : IEquatable<SDCombinedStackFrame>, ISerializableJson, ITaggableItem {
		[JsonConverter(typeof(StringEnumConverter))]
		public StackFrameType Type { get; set; }

		public string ModuleName { get; set; } = string.Empty;
		public string MethodName { get; set; } = string.Empty;
		public ulong OffsetInMethod { get; set; }
		public ulong InstructionPointer { get; set; }
		public ulong StackPointer { get; set; }
		public ulong StackPointerOffset { get; set; } // offset to first stackframe (in bytes)

		public ulong ReturnOffset { get; set; }
		public ISet<SDTag> Tags { get; } = new HashSet<SDTag>();

		public SDCombinedStackFrame LinkedStackFrame { get; set; }
		public SDFileAndLineNumber SourceInfo { get; private set; }

		public SDCombinedStackFrame(StackFrameType type, string moduleName, string methodName, ulong offsetInMethod, ulong ip, ulong sp, ulong returnOffset, ulong spOffset, SDFileAndLineNumber sourceInfo) {
			this.Type = type;
			this.ModuleName = moduleName;
			this.MethodName = methodName;
			this.OffsetInMethod = offsetInMethod;
			this.InstructionPointer = ip;
			this.StackPointer = sp;
			this.ReturnOffset = returnOffset;
			this.LinkedStackFrame = null;
			this.StackPointerOffset = spOffset;
			this.SourceInfo = sourceInfo;
		}
		
		public SDCombinedStackFrame(StackFrameType type, ulong instructionPointer, ulong stackPointer, string methodName, string moduleName, ulong nativeCode) {
			Type = type;
			InstructionPointer = instructionPointer;
			StackPointer = stackPointer;
			MethodName = methodName;
			ModuleName = moduleName;
			
			// calculate IL offset with instruction pointer of frame and instruction pointer 
			// in the target dump file of the start of the method's assembly
			OffsetInMethod = InstructionPointer - nativeCode;
		}

		[JsonConstructor]
		public SDCombinedStackFrame(StackFrameType type, string moduleName, string methodName,
			ulong offsetInMethod, ulong instructionPointer, ulong stackPointer,
			ulong returnOffset, SDCombinedStackFrame linkedFrame, ulong spOffset, SDFileAndLineNumber sourceInfo) {
			this.Type = type;
			this.ModuleName = moduleName;
			this.MethodName = methodName;
			this.OffsetInMethod = offsetInMethod;
			this.InstructionPointer = instructionPointer;
			this.StackPointer = stackPointer;
			this.ReturnOffset = returnOffset;
			this.LinkedStackFrame = linkedFrame;
			this.StackPointerOffset = spOffset;
			this.SourceInfo = sourceInfo;
		}

		public bool Equals(SDCombinedStackFrame other) {
			bool equals = false;
			if (this.Type.Equals(other.Type)
				&& this.InstructionPointer.Equals(other.InstructionPointer)
				&& this.MethodName.Equals(other.MethodName)
				&& this.ModuleName.Equals(other.ModuleName)
				&& this.OffsetInMethod.Equals(other.OffsetInMethod)
				&& this.ReturnOffset.Equals(other.ReturnOffset)
				&& this.StackPointer.Equals(other.StackPointer)
				&& this.StackPointerOffset.Equals(other.StackPointerOffset)
				&& this.SourceInfo.Equals(other.SourceInfo)) {
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

		public bool IsAgentFrame() {
			return Tags.Contains(SDTag.DynatraceAgentTag);
		}
	}
}

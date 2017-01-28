using Microsoft.Diagnostics.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperDump.Models {
	public class SDCombinedStackFrame : IEquatable<SDCombinedStackFrame>, ISerializableJson {
		[JsonConverter(typeof(StringEnumConverter))]
		public StackFrameType Type { get; set; }

		public string ModuleName { get; set; } = string.Empty;
		public string MethodName { get; set; } = string.Empty;
		public ulong OffsetInMethod { get; set; }
		public ulong InstructionPointer { get; set; }
		public ulong StackPointer { get; set; }
		public ulong ReturnOffset { get; set; }

		public SDCombinedStackFrame LinkedStackFrame { get; set; }

		public SDCombinedStackFrame(string moduleName, string methodName, ulong offsetInMethod, ulong ip, ulong sp, ulong returnOffset) {
			this.ModuleName = moduleName;
			this.MethodName = methodName;
			this.OffsetInMethod = offsetInMethod;
			this.InstructionPointer = ip;
			this.StackPointer = sp;
			this.ReturnOffset = returnOffset;
			this.LinkedStackFrame = null;
		}
		public SDCombinedStackFrame(ClrStackFrame frame) {
			if (frame.Kind == ClrStackFrameType.ManagedMethod)
				Type = StackFrameType.Managed;
			if (frame.Kind == ClrStackFrameType.Runtime)
				Type = StackFrameType.Special;

			InstructionPointer = frame.InstructionPointer;
			StackPointer = frame.StackPointer;


			if (frame.Method == null) {
				MethodName = frame.DisplayString; //for example GCFrame
				return;
			}

			MethodName = frame.Method.GetFullSignature();
			if (frame.Method.Type != null) {
				ModuleName = Path.GetFileNameWithoutExtension(frame.Method.Type.Module.Name);
			}

			// calculate IL offset with instruction pointer of frame and instruction pointer 
			// in the target dump file of the start of the method's assembly
			OffsetInMethod = InstructionPointer - frame.Method.NativeCode;
		}
		[JsonConstructor]
		public SDCombinedStackFrame(StackFrameType type, string moduleName, string methodName,
			ulong offsetInMethod, ulong instructionPointer, ulong stackPointer,
			ulong returnOffset, SDCombinedStackFrame linkedFrame) {
			this.Type = type;
			this.ModuleName = moduleName;
			this.MethodName = methodName;
			this.OffsetInMethod = offsetInMethod;
			this.InstructionPointer = instructionPointer;
			this.StackPointer = stackPointer;
			this.ReturnOffset = returnOffset;
			this.LinkedStackFrame = linkedFrame;
		}

		public bool Equals(SDCombinedStackFrame other) {
			bool equals = false;
			if (this.Type.Equals(other.Type)
				&& this.InstructionPointer.Equals(other.InstructionPointer)
				&& this.MethodName.Equals(other.MethodName)
				&& this.ModuleName.Equals(other.ModuleName)
				&& this.OffsetInMethod.Equals(other.OffsetInMethod)
				&& this.ReturnOffset.Equals(other.ReturnOffset)
				&& this.StackPointer.Equals(other.StackPointer)) {
				if (this.LinkedStackFrame == null && other.LinkedStackFrame == null)
					equals = true;
				else if (this.LinkedStackFrame == null && other.LinkedStackFrame != null)
					equals = false;
				else if (this.LinkedStackFrame.Equals(other.LinkedStackFrame)) {
					equals = true;
				}
				else {
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
	}
}

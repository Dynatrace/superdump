using ByteSizeLib;
using Microsoft.Diagnostics.Runtime;
using SuperDump.Models;
using System.Collections.Generic;
using System.Linq;
using SuperDump.ModelHelpers;

namespace SuperDump.Analyzers {
	public class MemoryAnalyzer {
		private DumpContext context;
		public Dictionary<ulong, SDMemoryObject> memDict;
		public List<SDBlockingObject> blockingObjects;

		public MemoryAnalyzer(DumpContext context) {
			this.context = context;
			this.memDict = new Dictionary<ulong, SDMemoryObject>();
			this.blockingObjects = new List<SDBlockingObject>();

			this.GetBlockingObjects();
			this.InitMemoryStat();
			this.PrintMemoryStat();
		}

		//TODO serialize this properly
		public void InitMemoryStat() {
			if (this.context.Runtime != null && this.context.Heap != null) {
				foreach (var obj in this.context.Heap.EnumerateObjectAddresses()) {
					ulong value;

					// get value at pointer
					this.context.Runtime.ReadPointer(obj, out value);

					ClrType type = this.context.Heap.GetObjectType(obj);
					if (type == null || string.IsNullOrEmpty(type.Name) || type.IsFree) {
						continue;
					}

					// needs obj reference, because types can be variable sized in CLR!
					ulong size = type.GetSize(obj);

					if (memDict.ContainsKey(value)) {
						memDict[value].Count++;
						memDict[value].Size += size;
					} else {
						memDict.Add(value, new SDMemoryObject { Count = 1, Size = size, Type = type.Name });
					}
				}
			}
		}

		public void GetBlockingObjects() {
			context.WriteLine(" --- Blocking objects in heap ---");

			if (this.context.Heap != null && this.context.Heap.CanWalkHeap) {
				context.WriteLine("{0,-20} {1,-10} {2,-8} {3,-20} {4,-20}", "Address", "Type", "Locked", "Owners", "Pending");
				foreach (BlockingObject obj in this.context.Heap.EnumerateBlockingObjects()) {
					this.blockingObjects.Add(obj.ToSDModel());
				}
			} else {
				context.WriteWarning("no heap information avaliable!");
			}
		}

		public void PrintMemoryStat() {
			context.WriteLine("{0,-20} {1,-10} {2,-10} {3}", "MT", "Count", "TotalSize", "Class Name");
			foreach (var memObj in from e in memDict
									orderby e.Value.Size
									descending select e) {
				context.WriteLine("{0,-20:x16} {1,-10} {2,-10} {3}",
					memObj.Key, memObj.Value.Count, ByteSize.FromBytes(memObj.Value.Size), memObj.Value.Type);
			}
		}

		public void PrintExceptionsObjects() {
			if (this.context.Heap != null && this.context.Heap.CanWalkHeap) {
				foreach (var address in this.context.Heap.EnumerateObjectAddresses()) {
					ClrType type = this.context.Heap.GetObjectType(address);
					if (type != null) {
						if (this.context.Heap.GetObjectType(address).IsException) {
							ClrException exception = this.context.Heap.GetExceptionObject(address);
							if (exception != null) {
								context.WriteLine("Address: {0:X}, Type: {1}, Message: {2}",
									exception.Address,
									exception.Type.Name,
									exception.GetExceptionMessageSafe());
								context.WriteLine("Stacktrace:");
								foreach (ClrStackFrame frame in exception.StackTrace) {
									context.WriteLine("{0,-20:x16} {1}!{2}",
										frame.InstructionPointer,
										frame.ModuleName,
										frame.DisplayString);
								}
							}
						}
					}
				}
			} else {
				context.WriteError("no heap information is avaliable");
			}
		}

		public void GetObjectsLikeType(string type) {
			if (this.context.Heap != null && this.context.Heap.CanWalkHeap) {
				context.WriteLine("--- objects of type '{0}'", type);

				// heap query for type and count them
				foreach (var q in from obj in context.Heap.EnumerateObjectAddresses()
								   let t = this.context.Heap.GetObjectType(obj)
								   where t.Name.ToUpper().Contains(type)
								   group obj by t.Name into g
								   let count = g.Count()
								   orderby count descending
								   select new { Type = g.Key, Count = count }) {
					context.WriteLine("Type: {0,-120}, Count: {1}", q.Type, q.Count);
				}
			} else {
				this.context.WriteError("no heap information is avaliable");
			}
		}

		public void GetHttpObjects() {
			if (this.context.Heap != null && this.context.Heap.CanWalkHeap) {
				foreach (var q in from obj in this.context.Heap.EnumerateObjectAddresses()
								   let t = this.context.Heap.GetObjectType(obj)
								   where t.Name.ToUpper().Contains("HTTP")
								   group obj by t.Name into g
								   let count = g.Count()
								   orderby count descending
								   select new { Type = g.Key, Count = count }) {
					context.WriteLine("Type: {0,-120}, Count: {1}", q.Type, q.Count);
				}
			} else {
				this.context.WriteError("no heap information is avaliable");
			}
		}
	}
}

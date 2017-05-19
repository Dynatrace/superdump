using Microsoft.Diagnostics.Runtime;
using SuperDump.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperDump.ModelHelpers {
	public static class ModelHelpers {
		public static SDCombinedStackFrame ToSDModel(this ClrStackFrame frame) {
			StackFrameType type =
			  (frame.Kind == ClrStackFrameType.ManagedMethod) ? StackFrameType.Managed :
			  (frame.Kind == ClrStackFrameType.Runtime) ? StackFrameType.Special :
			  /* here be dragons */ StackFrameType.Special;


			string methodName;
			string moduleName = string.Empty;
			if (frame.Method == null) {
				methodName = frame.DisplayString; //for example GCFrame
			} else {
				methodName = frame.Method.GetFullSignature();
				if (frame.Method.Type != null) {
					moduleName = Path.GetFileNameWithoutExtension(frame.Method.Type.Module.Name);
					if (string.IsNullOrEmpty(moduleName)) {
						moduleName = "UNKNOWN";
					}
				}
			}
			return new SDCombinedStackFrame(type, frame.InstructionPointer, frame.StackPointer, methodName, moduleName, (frame.Method != null ? frame.Method.NativeCode : 0));
		}

		public static SDBlockingObject ToSDModel(this BlockingObject obj) {
			var model = new SDBlockingObject();
			foreach (ClrThread waiter in obj.Waiters?.Where(t => t != null) ?? new ClrThread[0]) {
				model.WaiterOSThreadIds.Add(waiter.OSThreadId);
			}
			foreach (ClrThread owner in obj.Owners?.Where(t => t != null) ?? new ClrThread[0]) {
				model.OwnerOSThreadIds.Add(owner.OSThreadId);
			}

			// convert reason to int, so it can be case to our UnifiedBlockingReason
			model.Reason = (UnifiedBlockingReason)((int)obj.Reason);
			model.RecursionCount = obj.RecursionCount;
			model.ManagedObjectAddress = obj.Object;
			return model;
		}

		// ClrException.Message occasionally throws NullReferenceException even though the exception isn't null
		public static string GetExceptionMessageSafe(this ClrException exception) {
			try {
				return exception.Message;
			} catch (Exception) {
				return "<null>";
			}
		}


		public static SDAppDomain ToSDModel(this ClrAppDomain domain) {
			var model = new SDAppDomain();
			model.Address = domain.Address;
			model.ApplicationBase = domain.ApplicationBase;
			model.Id = domain.Id;
			model.Name = domain.Name;
			model.Modules = new List<SDClrModule>();
			foreach (ClrModule clrModule in domain.Modules) {
				model.Modules.Add(clrModule.ToSDModel());
			}
			model.Runtime = domain.Runtime.ClrInfo.ToSDModel();
			return model;
		}
		
		public static SDClrException ToSDModel(this ClrException clrException) {
			var model = new SDClrException();
			if (clrException != null) {
				model.Address = clrException.Address;
				model.Type = clrException.Type.Name;
				//this.HResult = clrException.HResult;

				if (model.InnerException != null) {
					model.InnerException = clrException.Inner.ToSDModel();
				}

				model.Message = clrException.GetExceptionMessageSafe();
				
				foreach (ClrStackFrame clrFrame in clrException.StackTrace) {
					model.StackTrace.Add(clrFrame.ToSDModel());
				}
			}
			return model;
		}
		
		public static SDClrVersion ToSDModel(this ClrInfo info) {
			var model = new SDClrVersion();
			if (info != null) {
				model.Version = info.Version.ToString();
				model.ClrFlavor = info.Flavor.ToString();

				// save DAC info 
				var dac = new SDModule();
				dac.FileSize = info.DacInfo.FileSize;
				dac.FilePath = info.DacInfo.FileName;
				dac.ImageBase = info.DacInfo.ImageBase;
				dac.IsManaged = info.DacInfo.IsManaged;
				dac.TimeStamp = info.DacInfo.TimeStamp;
				dac.Version = info.DacInfo.Version.ToString();

				// save PDB info, if avaliable
				if (info.DacInfo.Pdb != null) {
					dac.PdbInfo = new SDPdbInfo(info.DacInfo.Pdb.FileName,
						info.DacInfo.Pdb.Guid.ToString(),
						info.DacInfo.Pdb.Revision);
				}

				model.DacFile = dac;
			}
			return model;
		}

		public static SDClrModule ToSDModel(this ClrModule module) {
			var model = new SDClrModule();
			model.AssemblyId = module.AssemblyId;
			model.AssemblyName = module.AssemblyName;
			model.IsDynamic = module.IsDynamic;
			model.IsFile = module.IsFile;
			model.Name = module.Name;
			model.Pdb = new SDPdbInfo();
			if (module.Pdb != null) {
				model.Pdb.FileName = module.Pdb.FileName;
				model.Pdb.Guid = module.Pdb.Guid.ToString();
				model.Pdb.Revision = module.Pdb.Revision;
			}
			return model;
		}

		public static SDModule ToSDModel(this ModuleInfo info) {
			var model = new SDModule();
			model.FilePath = info.FileName;
			model.FileSize = info.FileSize;
			model.ImageBase = info.ImageBase;
			model.IsManaged = info.IsManaged;
			model.TimeStamp = info.TimeStamp;
			model.Version = info.Version.ToString();
			model.PdbInfo = new SDPdbInfo();
			if (info.Pdb != null) {
				model.PdbInfo.FileName = info.Pdb.FileName;
				model.PdbInfo.Guid = info.Pdb.Guid.ToString();
				model.PdbInfo.Revision = info.Pdb.Revision;
			}
			return model;
		}
	}
}

using Microsoft.Diagnostics.Runtime;
using System;

namespace SuperDump.Models {
	public static class ClrExceptionExtensions {
		// ClrException.Message occasionally throws NullReferenceException even though the exception isn't null
		public static string GetExceptionMessageSafe(this ClrException exception) {
			try {
				return exception.Message;
			} catch (Exception) {
				return "<null>";
			}
		}
	}
}

using DebugDiag.DotNet;
using DebugDiag.DotNet.AnalysisRules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SuperDump.DebugDiag {
	internal class DebugDiagHelper {
		private static string GetDefaultRegVal(string keyName) {
			return (string)Microsoft.Win32.Registry.GetValue(keyName, "", string.Empty);
		}

		internal static string GetInstallDir() {
			string installDir = string.Empty;
			string text = "HKEY_CLASSES_ROOT\\DbgLib.DbgControl\\CLSID";
			string defaultRegVal = GetDefaultRegVal(text);
			try {
				if (defaultRegVal != string.Empty && defaultRegVal != string.Empty) {
					text = string.Format("HKEY_CLASSES_ROOT\\CLSID\\{0}\\InprocServer32", defaultRegVal);
					defaultRegVal = GetDefaultRegVal(text);
					if (!string.IsNullOrEmpty(defaultRegVal)) {
						installDir = Path.GetDirectoryName(defaultRegVal);
					}
				}
			} catch (Exception ex) {
				string value = string.Format("An exception occurred while finding the v2 analysis runtime.\r\n\tKeyPath:  {0}\r\n\tValueName:  {1}\r\n\tMessage:  {2}\r\nStack Trace:\r\n{3}", new object[]
				{
					text,
					defaultRegVal,
					ex.Message,
					ex.StackTrace
				});
				Console.WriteLine(value);
			}

			if (string.IsNullOrEmpty(installDir) == false && installDir.EndsWith("x86Support")) {
				installDir = Path.GetDirectoryName(installDir);
			}

			return installDir;
		}

		internal static IEnumerable<Type> GetAnalysisRules(IEnumerable<string> analysis) {
			var assemblyFile = Path.Combine(GetInstallDir(), "AnalysisRules", "DebugDiag.AnalysisRules.dll");
			Assembly assembly = Assembly.LoadFile(assemblyFile);

			analysis = analysis.Select(s => s.ToLower()).ToList();

			Type ruleBaseType = typeof(IAnalysisRuleBase);
			Func<Type, bool> implementsRuleBase = t => t.GetInterfaces().Any(i => i == ruleBaseType);

			return assembly.GetTypes().Where(implementsRuleBase)
				.Where(t => analysis.Contains(t.Name.ToLower()));
		}
	}
}

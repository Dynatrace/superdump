using System;
using System.Collections.Generic;
using System.Text;

namespace SuperDump.Common {
	/// <summary>
	/// These names must match with the Dockerfile
	/// </summary>
	public sealed class EnvironmentName {
		public string Name { get; }

		public static readonly EnvironmentName REPOSITORY_URL = new EnvironmentName("REPOSITORY_URL");
		public static readonly EnvironmentName REPOSITORY_AUTHENTICATION = new EnvironmentName("REPOSITORY_AUTH");

		private EnvironmentName(string name) {
			Name = name;
		}
	}
}

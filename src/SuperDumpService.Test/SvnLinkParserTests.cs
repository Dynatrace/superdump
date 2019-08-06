using System;
using System.Collections.Generic;
using System.Text;
using SuperDump.Common;
using Xunit;

namespace SuperDumpService.Test {
	public class SvnLinkParserTests {
		[Fact]
		public void TestSvnLinkParser() {
			string inputLinux = "/path/to/src/oa-s160/linux-x86/src/folder/test.c:30";
			string expectedLinux = "branches/sprint_160/src/folder/test.c:30";
			Assert.Equal(expectedLinux, DynatraceSourceLink.GetRepoPathIfAvailable(inputLinux));

			string inputWindows = @"c:\path\to\src\oa-s160\linux-x86\src\folder\test.c:30";
			string expectedWindows = @"branches/sprint_160/src\folder\test.c:30";
			Assert.Equal(expectedWindows, DynatraceSourceLink.GetRepoPathIfAvailable(inputWindows));

			string inputTrunk = @"c:\path\to\src\trunk\src\folder\test.c:30";
			string expectedTrunk = @"trunk\src\folder\test.c:30";
			Assert.Equal(expectedTrunk, DynatraceSourceLink.GetRepoPathIfAvailable(inputTrunk));

			string inputSprint = @"c:\path\to\src\sprint_160\src\folder\test.c:30";
			string expectedSprint = @"branches/sprint_160\src\folder\test.c:30";
			Assert.Equal(expectedSprint, DynatraceSourceLink.GetRepoPathIfAvailable(inputSprint));

			string inputInvalid = @"c:\path\to\src\folder\test.c:30";
			Assert.Null(DynatraceSourceLink.GetRepoPathIfAvailable(inputInvalid));
		}
	}
}

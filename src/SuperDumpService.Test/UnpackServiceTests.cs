using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SuperDumpService.Services;
using Xunit;

namespace SuperDumpService.Test {
	public class UnpackServiceTests {
		private const string InvalidCharacterTestZipFile = @"samples\invalid_character_test.zip";
		private const string InvalidCharacterTestTarGzFile = @"samples\invalid_character_test.tar.gz";

		//The original filename was "invalid:character?test|.txt
		private const string InvalidFilename = @"invalid_character_test\invalid_character_test_.txt";

		[Fact]
		public void TestUnpackZipWithInvalidFilename() {
			var unpackService = new UnpackService();

			DirectoryInfo outDirectory = unpackService.ExtractArchive(new FileInfo(InvalidCharacterTestZipFile), ArchiveType.Zip);

			string expectedResultFile = Path.Combine(outDirectory.FullName, InvalidFilename);

			Assert.True(File.Exists(expectedResultFile));

			string[] fileContents = File.ReadAllLines(expectedResultFile);
			Assert.Single(fileContents);
			Assert.Equal("test", fileContents[0]);

			outDirectory.Delete(true);
		}

		[Fact]
		public void TestUnpackTarGzWithInvalidFilename() {
			var unpackService = new UnpackService();

			DirectoryInfo outDirectory = unpackService.ExtractArchive(new FileInfo(InvalidCharacterTestTarGzFile), ArchiveType.TarGz);

			string expectedResultFile = Path.Combine(outDirectory.FullName, InvalidFilename);

			Assert.True(File.Exists(expectedResultFile));

			string[] fileContents = File.ReadAllLines(expectedResultFile);
			Assert.Single(fileContents);
			Assert.Equal("test", fileContents[0]);

			outDirectory.Delete(true);
		}
	}
}

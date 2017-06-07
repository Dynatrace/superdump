using SuperDump.Analyzer.Linux;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SuperDump.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SuperDump.Analyzer.Linux.Test {
	[TestClass]
	public class SharedLibAdapterTest {
		private SharedLibAdapter adapter;
		private FilesystemDouble filesystem;

		[TestInitialize]
		public void Init() {
			filesystem = new FilesystemDouble();
			adapter = new SharedLibAdapter(filesystem);
		}

		[TestMethod]
		public void TestBlacklistedModule() {
			SharedLib lib = CreateLib("/dev/zero");
			Assert.IsNull(adapter.Adapt(lib));
		}

		[TestMethod]
		public void TestValidPath() {
			SharedLib lib = CreateLib("/some/lib/somelib.so");
			SDCDModule module = adapter.Adapt(lib);
			Assert.AreEqual("/some/lib/somelib.so", module.FilePath);
		}

		[TestMethod]
		public void TestAllFields() {
			SharedLib lib = CreateLib("/lib/somelib-42.so");
			filesystem.ExistingFiles.Add("/lib/somelib-42.so");
			filesystem.FileSizes["/lib/somelib-42.so"] = 1234;
			SDCDModule module = adapter.Adapt(lib);
			Assert.AreEqual("/lib/somelib-42.so", module.FilePath);

			Assert.AreEqual("somelib-42.so", module.FileName);
			Assert.AreEqual("/lib/somelib-42.so", module.LocalPath);
			Assert.IsNull(module.Version);
			Assert.AreEqual(1234L, module.FileSize);
			Assert.AreEqual(lib.BindingOffset, module.Offset);
			Assert.AreEqual(lib.StartAddress, module.StartAddress);
			Assert.AreEqual(lib.EndAddress, module.EndAddress);
		}

		private SharedLib CreateLib(string path) {
			SharedLib lib = new SharedLib() {
				Path = Encoding.UTF8.GetBytes(path),
				StartAddress = 1234,
				EndAddress = 4321,
				BindingOffset = 1111
			};
			return lib;
		}
	}
}

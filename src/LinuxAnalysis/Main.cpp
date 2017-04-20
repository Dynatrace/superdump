#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <vector>

#include <unistd.h>

#include "SharedLibFile.h"
#include "SharedLibResolver.h"
#include "unwind/UnwindContext.h"
#include "unwind/ThreadVector.h"

#include "extracting/ArchiveExtractor.h"

#include "helper/FileSystemHelper.h"

using namespace std;

void debug(string dumppath);

int main(int argc, char** argv) {
	char cwd[1024];
	getcwd(cwd, sizeof(cwd));

	if (argc >= 2) {
		ArchiveExtractor extractor;
		printf("Extracting \"%s\".\r\n", argv[1]);
		string coredump = extractor.extractArchiveAndReturnCoredump(argv[1]);
		if (coredump == "") {
			printf("Cannot locate core dump!\r\n");
			return 1;
		}
		printf("\r\nFound core dump file: %s\r\n", coredump.c_str());
		debug(coredump);
	}
	else {
		printf("Too few arguments! exe <dumpfile>\r\n");
	}
}

void debug(string dumppath) {
	string workingDir = FileSystemHelper::getParentDir(dumppath);
	printf("Working directory: %s\r\n", workingDir.c_str());
	SharedLibResolver resolver;
	vector<SharedLibFile> sharedLibs = resolver.findSharedLibs(dumppath);

	UnwindContext* unwindContext = new UnwindContext(dumppath, sharedLibs, workingDir);

	printf("\r\nDebugging core dump file: %s\r\n\r\n", dumppath.c_str());
	ThreadVector threads(unwindContext);
	threads.printAllStackTraces();
	printf("\r\n\r\nJSON:\r\n{%s}", threads.toJson().c_str());
	delete unwindContext;
}


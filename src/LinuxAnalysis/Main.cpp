#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <vector>

#include <iostream>
#include <fstream>

#include "model/SharedLibFile.h"
#include "SharedLibResolver.h"
#include "unwind/UnwindContext.h"
#include "model/ThreadVector.h"

#include "extracting/ArchiveExtractor.h"

#include "helper/FileSystemHelper.h"

#include "model/AnalysisResult.h"
#include "model/AnalysisInfo.h"
#include "model/SystemContext.h"
#include "model/BlockingObjects.h"
#include "model/DeadlockInformation.h"
#include "model/LastEvent.h"
#include "model/MemoryInformation.h"

using namespace std;

void debug(string dumppath);

int main(int argc, char** argv) {
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

	string timestamp = string("0001-01-01T00:00:00");
	AnalysisInfo analysisInfo = AnalysisInfo(&dumppath, &dumppath, NULL, NULL, &timestamp);
	SystemContext systemContext = SystemContext(NULL, NULL, NULL, NULL, NULL, NULL, 0, sharedLibs);
	LastEvent lastEvent = LastEvent("EXCEPTION", "Unknown", 0);
	DeadlockInformation deadlocks = DeadlockInformation();
	BlockingObjects blockingObjects = BlockingObjects();
	MemoryInformation memInfo = MemoryInformation();

	AnalysisResult result = AnalysisResult(false, 1, analysisInfo, systemContext, lastEvent, threads, deadlocks, blockingObjects, memInfo);
	ofstream outfile;
	outfile.open("superdump-result.json");
	outfile << result.toJson();
	outfile.close();
	delete unwindContext;
}


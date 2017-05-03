#include "LibunwindWrapper.h"
#include "Main.h"

#include <stdio.h>

LibunwindWrapper* wrapper;

int main(int argc, char** argv) {
	if (argc != 3) {
		printf("2 arguments required.\r\n");
		return 1;
	}
	printf("Launching with args %s,%s", argv[1], argv[2]);
	fflush(stdout);
	init(argv[1], argv[2]);
	return 0;
}

MYAPI void init(const char* filepath, const char* workingDir) {
	wrapper = new LibunwindWrapper(filepath, workingDir);
}

MYAPI int getNumberOfThreads()
{
	return wrapper->getNumberOfThreads();
}

MYAPI int getThreadId() {
	return wrapper->getThreadId();
}

MYAPI void selectThread(int threadNumber) {
	wrapper->selectThread(threadNumber);
}

MYAPI unsigned long getInstructionPointer() {
	return wrapper->getInstructionPointer();
}

MYAPI unsigned long getStackPointer() {
	return wrapper->getStackPointer();
}

MYAPI char* getProcedureName() {
	return wrapper->getProcedureName();
}

MYAPI unsigned long getProcedureOffset() {
	return wrapper->getProcedureOffset();
}

MYAPI bool step() {
	return wrapper->step();
}
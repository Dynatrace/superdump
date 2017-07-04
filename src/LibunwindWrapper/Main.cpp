#include "LibunwindWrapper.h"
#include "Main.h"

#include <stdio.h>
#include <vector>

using namespace std;

LibunwindWrapper* wrapper;

int main(int argc, char** argv) {
	if (argc != 3) {
		printf("2 arguments required.\r\n");
		return 1;
	}
	printf("Launching with args %s,%s\r\n", argv[1], argv[2]);
	fflush(stdout);
	init(argv[1], argv[2]);
	printf("Threads: %d\r\n", getNumberOfThreads());
	fflush(stdout);
	printf("Thread ID: %d\r\n", getThreadId());
	fflush(stdout);
	printf("Auxv 15 (PLAT): %s\r\n", getAuxvString(15));
	fflush(stdout);
	printf("Auxv 31 (EXEC): %s\r\n", getAuxvString(31));
	fflush(stdout);
	return 0;
}

MYAPI void init(const char* filepath, const char* workingDir) {
	wrapper = new LibunwindWrapper(filepath, workingDir);
}

MYAPI int getNumberOfThreads() {
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

MYAPI unsigned long getAuxvValue(int type) {
	return wrapper->getAuxvValue(type);
}

MYAPI const char* getAuxvString(int type) {
	return wrapper->getAuxvString(type);
}

MYAPI int getSignalNumber(int thread_no) {
	return wrapper->getSignalNo(thread_no);
}

MYAPI int getSignalErrorNo(int thread_no) {
	return wrapper->getSignalErrorNo(thread_no);
}

MYAPI unsigned long getSignalAddress(int thread_no) {
	return wrapper->getSignalAddress(thread_no);
}

MYAPI const char* getFileName() {
	return wrapper->getFileName();
}

MYAPI const char* getArgs() {
	return wrapper->getArgs();
}

MYAPI int is64Bit() {
	return wrapper->is64Bit();
}

MYAPI void destroy() {
	delete wrapper;
}
#pragma once

#include <vector>

#include <libunwind-coredump.h>
#include "SharedLibFile.h"

using namespace std;

class LibunwindWrapper
{
private:
	string filepath;

	unw_addr_space_t addressSpace;
	UCD_info* ucdInfo;
	pid_t pid;
	string workingDir;

	unw_cursor_t cursor;

public:
	LibunwindWrapper(string filepath, string workingDir);
	~LibunwindWrapper();

	string getFilepath();

	int getNumberOfThreads();
	int getThreadId();
	void selectThread(unsigned int threadNumber);

	unsigned long getInstructionPointer();
	unsigned long getStackPointer();
	char* getProcedureName();
	unsigned long getProcedureOffset();
	bool step();

	unsigned long getAuxvValue(int type_id);
	const char* getAuxvString(int type_id);

};


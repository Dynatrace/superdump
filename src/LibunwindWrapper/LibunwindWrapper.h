#pragma once

#include <vector>
#include <string>

#include <libunwind-coredump.h>

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

	void addBackingFilesFromNotes();
	void addBackingFileAtAddr(const char* filename, unsigned long address);

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
	int getSignalNo(int thread_no);
	int getSignalErrorNo(int thread_no);
	unsigned long getSignalAddress(int thread_no);
	const char* getFileName();
	const char* getArgs();
	const int is64Bit();
};


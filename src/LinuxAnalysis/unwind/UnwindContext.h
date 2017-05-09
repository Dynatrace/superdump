#pragma once

#include <vector>
#include <string>

#define UNW_REMOTE_ONLY

#include <libunwind-coredump.h>

#include "../model/SharedLibFile.h"

using namespace std;

class UnwindContext
{
	unw_addr_space_t addressSpace;
	UCD_info* ucdInfo;
	pid_t pid;
	vector<SharedLibFile> sharedLibs;
	string workingDir;

public:
	UnwindContext(string filepath, vector<SharedLibFile> sharedLibs, string workingDir);
	UnwindContext(unw_addr_space_t addressSpace, UCD_info* ucdInfo, pid_t pid, vector<SharedLibFile> sharedLibs, string workingDir);
	~UnwindContext();

	unw_addr_space_t getAddressSpace();
	UCD_info* getUcdInfo();
	int getPid();
	vector<SharedLibFile> getSharedLibs();

private:
	void addBackingFiles();
	bool fileExists(string path);
	void addSharedLib(SharedLibFile file);
};


#pragma once

#include <string>
#include <vector>

#include "../model/SharedLibFile.h"
#include "../model/UnwStackTrace.h"

using namespace std;

class StacktraceUnwinder
{
public:
	StacktraceUnwinder();
	~StacktraceUnwinder();

	UnwStackTrace unwind(unw_cursor_t cursor, vector<SharedLibFile> sharedLibs);

private:
	string findModule(vector<SharedLibFile> sharedLibs, unsigned long ip);
	char* demangle(char* procName);
};


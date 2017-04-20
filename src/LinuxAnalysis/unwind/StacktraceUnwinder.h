#include "UnwStackTrace.h"

#include <string>

using namespace std;

#pragma once
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


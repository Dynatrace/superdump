#include <libunwind.h>
#include <string>

using namespace std;

#pragma once
class StackFrame
{
	string type;
	unsigned long stackPtr;
	unsigned long instrPtr;
	unsigned long returnAddr;
	string procName;
	unsigned int offset;
	string library;

public:
	StackFrame(string type, unsigned long stackPtr, unsigned long instrPtr, unsigned long returnAddr, string procName, unsigned int offset, string library);
	StackFrame(string type, unw_word_t stackPtr, unw_word_t instrPtr, unw_word_t returnAddr, char* procName, unw_word_t offset, string library);
	~StackFrame();

	void print();

	void writeJson(ostream& os);
	string toJson();
};


#include <libunwind.h>
#include <string>

#include "JsonObject.h"
#include "TagVector.h"

using namespace std;

#pragma once
class StackFrame : public JsonObject
{
	string type;
	unsigned long stackPtr;
	unsigned long instrPtr;
	unsigned long returnAddr;
	string procName;
	unsigned long procOffset, stackPtrOffset;
	string library;
	TagVector tags;

	string* sourceFile;
	int lineNumber;

public:
	StackFrame(string type, unsigned long stackPtr, unsigned long instrPtr, unsigned long returnAddr, string procName, unsigned long procOffset, string library, 
		unsigned long stackPtrOffset, TagVector tags, string* sourceFile, int lineNumber);
	StackFrame(string type, unw_word_t stackPtr, unw_word_t instrPtr, unw_word_t returnAddr, char* procName, unw_word_t offset, string library, 
		unsigned long stackPtrOffset, TagVector tags, string* sourceFile, int lineNumber);
	~StackFrame();

	void print();

	string toJson();
};


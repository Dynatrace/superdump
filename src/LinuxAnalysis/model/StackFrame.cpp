#include "StackFrame.h"

#include <string.h>

using namespace std;

StackFrame::StackFrame(string type, unsigned long stackPtr, unsigned long instrPtr, unsigned long returnAddr, string procName, unsigned long procOffset, 
	string library, unsigned long stackPtrOffset, TagVector tags, string* sourceFile, int lineNumber)
	: type(type), stackPtr(stackPtr), instrPtr(instrPtr), returnAddr(returnAddr), procName(procName), procOffset(procOffset), library(library), 
	stackPtrOffset(stackPtrOffset), tags(tags), sourceFile(sourceFile), lineNumber(lineNumber) {
}

StackFrame::StackFrame(string type, unw_word_t stackPtr, unw_word_t instrPtr, unw_word_t returnAddr, char* procName, unw_word_t procOffset, string library,
	unsigned long stackPtrOffset, TagVector tags, string* sourceFile, int lineNumber)
	: type(type), stackPtr((unsigned long) stackPtr), instrPtr((unsigned long) instrPtr), returnAddr((unsigned long) returnAddr), procName(procName),
	procOffset((unsigned long)procOffset), library(library), stackPtrOffset(stackPtrOffset), tags(tags), sourceFile(sourceFile), lineNumber(lineNumber) {
}

StackFrame::~StackFrame() {
}

void StackFrame::print() {
	printf("%s\t0x%016lX\t0x%016lX\t0x%016lX\t%s!%s+%lX\r\n", type.c_str(), stackPtr, instrPtr, returnAddr, library.c_str(), procName.c_str(), procOffset);
}

string StackFrame::toJson() {
	string sourceJson = sourceFile ? "{ \"File\": " + fromString(sourceFile) + ", \"Line\": " + fromInt(lineNumber) + "}" : "null";
	return "\"Type\": " + fromString(type) + "," +
		"\"ModuleName\": " + fromString(library) + "," +
		"\"MethodName\": " + fromString(procName) + "," +
		"\"OffsetInMethod\": " + fromUnsLong(procOffset) + "," +
		"\"InstructionPointer\": " + fromUnsLong(instrPtr) + "," +
		"\"StackPointer\": " + fromUnsLong(stackPtr) + "," +
		"\"StackPointerOffset\": " + fromUnsLong(stackPtrOffset) + "," +
		"\"ReturnOffset\": " + fromUnsLong(returnAddr) + "," +
		"\"Tags\": " + tags.toJson() + "," +
		"\"LinkedStackFrame\": null," +
		"\"SourceInfo\": " + sourceJson;
}
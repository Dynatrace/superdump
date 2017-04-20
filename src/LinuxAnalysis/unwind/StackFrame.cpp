#include "StackFrame.h"

#include <string.h>

using namespace std;

StackFrame::StackFrame(string type, unsigned long stackPtr, unsigned long instrPtr, unsigned long returnAddr, string procName, unsigned int offset, string library)
	: type(type), stackPtr(stackPtr), instrPtr(instrPtr), returnAddr(returnAddr), procName(procName), offset(offset), library(library) {
}

StackFrame::StackFrame(string type, unw_word_t stackPtr, unw_word_t instrPtr, unw_word_t returnAddr, char* procName, unw_word_t offset, string library) 
	: type(type), stackPtr((unsigned long) stackPtr), instrPtr((unsigned long) instrPtr), returnAddr((unsigned long) returnAddr), procName(procName), offset((unsigned long) offset), library(library) {
}

StackFrame::~StackFrame() {
}

void StackFrame::print() {
	printf("%s\t0x%016X\t0x%016X\t0x%016X\t%s!%s+%X\r\n", type.c_str(), stackPtr, instrPtr, returnAddr, library.c_str(), procName.c_str(), offset);
}

void StackFrame::writeJson(std::ostream& os) {
}

string StackFrame::toJson() {
	return "frame: { type:\"" + type + "\", stackPtr:" + to_string(stackPtr) + ",instrPtr:" + to_string(instrPtr) + ",returnAddr:" + to_string(returnAddr) + 
		",procName:\"" + procName + "\", offset:" + to_string(offset) + ", library:\"" + library + "\"}";
}
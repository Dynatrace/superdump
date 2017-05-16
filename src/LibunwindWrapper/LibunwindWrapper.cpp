#include "LibunwindWrapper.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <fstream>
#include <cxxabi.h>

char* demangle(char* procName);
int getAuxvInt(int idx);

LibunwindWrapper::LibunwindWrapper(string filepath, string workingDir) 
	: filepath(filepath) {
	printf("Initializing libunwind wrapper...\n");
	fflush(stdout);
	this->addressSpace = unw_create_addr_space(&_UCD_accessors, 0);
	this->ucdInfo = _UCD_create(filepath.c_str());
	this->pid = _UCD_get_pid(ucdInfo);
	this->workingDir = workingDir;

	_UCD_set_backing_files_sysroot(this->ucdInfo, workingDir.c_str());
	_UCD_add_backing_files_from_file_note(this->ucdInfo);

	printf("Initialization success\n");
	fflush(stdout);
}


LibunwindWrapper::~LibunwindWrapper() {
}

string LibunwindWrapper::getFilepath() {
	return filepath;
}

int LibunwindWrapper::getNumberOfThreads()
{
	return _UCD_get_num_threads(this->ucdInfo);
}

int LibunwindWrapper::getThreadId() {
	return _UCD_get_pid(ucdInfo);
}

void LibunwindWrapper::selectThread(unsigned int threadNumber) {
	_UCD_select_thread(ucdInfo, threadNumber);
	int ret = unw_init_remote(&cursor, addressSpace, ucdInfo);
	if (ret != 0) {
		printf("Failed to initiate remote session for thread %d: %d\n", threadNumber, ret);
		return;
	}
}

unsigned long LibunwindWrapper::getInstructionPointer() {
	unw_word_t ip;
	if (unw_get_reg(&cursor, UNW_REG_IP, &ip) == 0) {
		return ip;
	}
	else {
		return 0;
	}
}

unsigned long LibunwindWrapper::getStackPointer() {
	unw_word_t sp;
	if (unw_get_reg(&cursor, UNW_REG_SP, &sp) == 0) {
		return sp;
	}
	else {
		return 0;
	}
}

char* LibunwindWrapper::getProcedureName() {
	char* procName = (char*)malloc(2048);
	unsigned long methodOffset;
	if (unw_get_proc_name(&cursor, procName, 2048, &methodOffset) == 0) {
		return demangle(procName);
	}
	return NULL;
}

unsigned long LibunwindWrapper::getProcedureOffset() {
	char* procName = (char*) malloc(2048);
	unsigned long methodOffset;
	if (unw_get_proc_name(&cursor, procName, 2048, &methodOffset) == 0) {
		return methodOffset;
	}
	return 0;
}

bool LibunwindWrapper::step() {
	return unw_step(&cursor) == 0;
}

unsigned long LibunwindWrapper::getAuxvValue(int type) {
	unw_word_t val;
	if (!_UCD_get_auxv_value(ucdInfo, type, &val)) {
		return 0;
	}
	return val;
}

const char* LibunwindWrapper::getAuxvString(int type) {
	unw_word_t val;
	if (!_UCD_get_auxv_value(ucdInfo, type, &val)) {
		printf("AUXV does not contain value for type %d.\n", type);
		return 0;
	}

	unw_word_t str;
	string result = "";
	for (;; val += sizeof(unw_word_t)) {
		_UCD_access_mem(addressSpace, val, &str, 0, ucdInfo);
		int shift = 0;
		for (unw_word_t bitmask = 0xFF; bitmask != 0; bitmask <<= 8, shift += 8) {
			if ((str & bitmask) == 0) {
				char* dyn = (char*) malloc(result.size());
				strncpy(dyn, result.c_str(), result.size());
				return dyn;
			}
			result = result + (char)((str & bitmask) >> shift);
		}
	}
}

char* demangle(char* procName) {
	int status;
	char* demangled = abi::__cxa_demangle(procName, NULL, 0, &status);
	if (status == 0 && demangled != NULL) {
		return demangled;
	}
	return procName;
}
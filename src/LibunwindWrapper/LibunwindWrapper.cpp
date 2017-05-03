#include "LibunwindWrapper.h"

#include <stdio.h>
#include <stdlib.h>
#include <fstream>
#include <cxxabi.h>

char* demangle(char* procName);

LibunwindWrapper::LibunwindWrapper(string filepath, string workingDir) {
	printf("Initializing libunwind wrapper...\r\n");
	fflush(stdout);
	this->addressSpace = unw_create_addr_space(&_UCD_accessors, 0);
	this->ucdInfo = _UCD_create(filepath.c_str());
	this->pid = _UCD_get_pid(ucdInfo);
	this->workingDir = workingDir;

	_UCD_set_backing_files_sysroot(this->ucdInfo, workingDir.c_str());
	_UCD_add_backing_files_from_file_note(this->ucdInfo);

	printf("Initialization success\r\n");
	fflush(stdout);
}


LibunwindWrapper::~LibunwindWrapper() {
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
		printf("Failed to initiate remote session for thread %d: %d", threadNumber, ret);
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

char* demangle(char* procName) {
	int status;
	char* demangled = abi::__cxa_demangle(procName, NULL, 0, &status);
	if (status == 0 && demangled != NULL) {
		return demangled;
	}
	return procName;
}
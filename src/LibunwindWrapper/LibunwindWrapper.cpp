#include "LibunwindWrapper.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <fstream>
#include <cxxabi.h>

void demangle(char* procName, char* dest, int length);

LibunwindWrapper::LibunwindWrapper(string filepath, string workingDir) 
	: filepath(filepath) {
	printf("Initializing libunwind wrapper...\n");
	fflush(stdout);
	this->addressSpace = unw_create_addr_space(&_UCD_accessors, 0);
	this->ucdInfo = _UCD_create(filepath.c_str());
	this->pid = _UCD_get_pid(ucdInfo);
	this->workingDir = workingDir;

	printf("Initialization success\n");
	fflush(stdout);
}


LibunwindWrapper::~LibunwindWrapper() {
	_UCD_destroy(ucdInfo);
}

void LibunwindWrapper::addBackingFilesFromNotes() {
	_UCD_set_backing_files_sysroot(this->ucdInfo, workingDir.c_str());
	_UCD_add_backing_files_from_file_note(this->ucdInfo);
}

string LibunwindWrapper::getFilepath() {
	return filepath;
}

int LibunwindWrapper::getNumberOfThreads() {
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
	if (!procName) {
		return 0;
	}

	char* demangled = (char*)malloc(2048);
	if (!demangled) {
		return 0;
	}
	if (unw_get_proc_name(&cursor, procName, 2048, 0) == 0) {
		demangle(procName, demangled, 2048);
	}
	free(procName);
	return demangled;
}

unsigned long LibunwindWrapper::getProcedureOffset() {
	char* procName = (char*)malloc(2048);
	if (!procName) {
		return 0;
	}
	unsigned long methodOffset = 0;
	unw_get_proc_name(&cursor, procName, 2048, &methodOffset);
	free(procName);
	return methodOffset;
}

bool LibunwindWrapper::step() {
	return unw_step(&cursor) == 0;
}

unsigned long LibunwindWrapper::getAuxvValue(int type) {
	unw_word_t val = 0;
	_UCD_get_auxv_value(ucdInfo, type, &val);
	return val;
}

const char* LibunwindWrapper::getAuxvString(int type) {
	unw_word_t val;
	if (!_UCD_get_auxv_value(ucdInfo, type, &val)) {
		printf("AUXV does not contain value for type %d.\n", type);
		fflush(stdout);
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
				if (!dyn) {
					return 0;
				}
				strncpy(dyn, result.c_str(), result.size());
				return dyn;
			}
			result = result + (char)((str & bitmask) >> shift);
		}
	}
}

int LibunwindWrapper::getSignalNo(int thread_no) {
	siginfo_t siginfo;
	if (_UCD_get_siginfo(ucdInfo, thread_no, &siginfo) != 0) {
		return -1;
	}
	return siginfo.si_signo;
}

int LibunwindWrapper::getSignalErrorNo(int thread_no) {
	siginfo_t siginfo;
	if (_UCD_get_siginfo(ucdInfo, thread_no, &siginfo) != 0) {
		return -1;
	}
	return siginfo.si_errno;
}

unsigned long LibunwindWrapper::getSignalAddress(int thread_no) {
	siginfo_t siginfo;
	if (_UCD_get_siginfo(ucdInfo, thread_no, &siginfo) != 0) {
		return -1;
	}
	return (unsigned long) siginfo.si_addr;
}

const char* LibunwindWrapper::getFileName() {
	elf_prpsinfo prpsinfo;
	if (_UCD_get_prpsinfo(ucdInfo, &prpsinfo) < 0) {
		return 0;
	}
	char* fn = prpsinfo.pr_fname;
	char* tmp = (char*)malloc(16);
	if (!tmp) {
		return 0;
	}
	strncpy(tmp, fn, 16);
	return tmp;
}

const char* LibunwindWrapper::getArgs() {
	elf_prpsinfo prpsinfo;
	if (_UCD_get_prpsinfo(ucdInfo, &prpsinfo) < 0) {
		return 0;
	}
	char* args = prpsinfo.pr_psargs;
	char* tmp = (char*)malloc(80);
	if (!tmp) {
		return 0;
	}
	strncpy(tmp, args, 80);
	return tmp;
}

const int LibunwindWrapper::is64Bit() {
	// read EI_CLASS from ELF identification
	ifstream input(filepath, ios::binary);
	char ei_class;
	// skip ELF identification
	input.ignore(4);
	// read 5th byte -> EI_CLASS
	input.read(&ei_class, 1);
	if (ei_class == 0) {
		printf("ei_class is set to invalid class! No platform type available!\n");
		return -1;
	}
	else if (ei_class == 1) {
		return 0;
	}
	else if (ei_class == 2) {
		return 1;
	}
	else {
		printf("Invalid ei_class value %d! Not an ELF file?\n", ei_class);
		return -1;
	}
}

void LibunwindWrapper::addBackingFileAtAddr(const char* filename, unsigned long address) {
	fflush(stdout);
	int ret = _UCD_add_backing_file_at_vaddr(this->ucdInfo, address, filename);
	printf("[%d] added backing file at %lu: %s\n", ret, address, filename);
	fflush(stdout);
}

void demangle(char* procName, char* dest, int length) {
	int status;
	char* demangled = abi::__cxa_demangle(procName, NULL, 0, &status);
	if (status == 0 && demangled != NULL) {
		strncpy(dest, demangled, length);
		free(demangled);
	}
	else {
		strncpy(dest, procName, length);
	}
}
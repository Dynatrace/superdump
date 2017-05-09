#include "StacktraceUnwinder.h"

#include <cxxabi.h>
#include <string.h>

StacktraceUnwinder::StacktraceUnwinder() {
}


StacktraceUnwinder::~StacktraceUnwinder() {
}

UnwStackTrace StacktraceUnwinder::unwind(unw_cursor_t cursor, vector<SharedLibFile> sharedLibs) {
	unw_word_t ip = 0;
	unw_word_t oldIp = 0;
	unw_word_t sp, oldSp, methodOffset, oldMethodOffset;
	char procName[2048];
	char oldProcName[2048];
	unw_proc_info procInfo;

	vector<StackFrame> stackFrames;

	do {
		int ret;
		ret = unw_get_reg(&cursor, UNW_REG_IP, &ip);
		if (ret != 0) {
			printf("get_ip: %d\r\n", ret);
		}
		ret = unw_get_reg(&cursor, UNW_REG_SP, &sp);
		if (ret != 0) {
			printf("get_sp: %d\r\n", ret);
		}
		ret = unw_get_proc_name(&cursor, procName, sizeof(procName), &methodOffset);
		if (ret >= 0) {
			strcpy(procName, demangle(procName));
		}
		ret = unw_get_proc_info(&cursor, &procInfo);
		if (ret != 0) {
			printf("get_proc_info: %d\r\n", ret);
		}

		if (ip == 0) {
			break;
		}

		if (oldIp != 0) {
			stackFrames.push_back(StackFrame("Native", oldSp, oldIp, ip, oldProcName, oldMethodOffset, findModule(sharedLibs, oldIp), 0, TagVector(), NULL, 0));
		}
		oldIp = ip;
		oldSp = sp;
		oldMethodOffset = methodOffset;
		strcpy(oldProcName, procName);
	} while (unw_step(&cursor) > 0 && stackFrames.size() < 256);
	stackFrames.push_back(StackFrame("Native", oldSp, oldIp, 0, oldProcName, oldMethodOffset, findModule(sharedLibs, oldIp), 0, TagVector(), NULL, 0));

	bool overflow = stackFrames.size() >= 256;

	return UnwStackTrace(stackFrames, overflow);
}

char* StacktraceUnwinder::demangle(char* procName) {
	int status;
	char* demangled = abi::__cxa_demangle(procName, NULL, 0, &status);
	if (status == 0 && demangled != NULL) {
		return demangled;
	}
	return procName;
}

string StacktraceUnwinder::findModule(vector<SharedLibFile> sharedLibs, unsigned long ip) {
	if (ip == 0 || sharedLibs.size() == 0) {
		return "<unknown>";
	}
	SharedLibFile* cur = NULL;
	for (int i = 0; i < sharedLibs.size(); i++) {
		if (ip >= sharedLibs.at(i).getAddress()) {
			if (cur == NULL || sharedLibs.at(i).getAddress() > cur->getAddress()) {
				cur = &sharedLibs.at(i);
			}
		}
	}
	if (cur == NULL) {
		return "<unknown>";
	}
	return cur->getName();
}
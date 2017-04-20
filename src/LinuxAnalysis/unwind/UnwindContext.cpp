#include "UnwindContext.h"

#include <libunwind-coredump.h>
#include <fstream>

UnwindContext::UnwindContext(unw_addr_space_t addressSpace, UCD_info* ucdInfo, pid_t pid, vector<SharedLibFile> sharedLibs, string workingDir)
	:addressSpace(addressSpace), ucdInfo(ucdInfo), pid(pid), sharedLibs(sharedLibs), workingDir(workingDir) {
}

UnwindContext::UnwindContext(string filepath, vector<SharedLibFile> sharedLibs, string workingDir) {
	this->addressSpace = unw_create_addr_space(&_UCD_accessors, 0);
	this->ucdInfo = _UCD_create(filepath.c_str());
	this->pid = _UCD_get_pid(ucdInfo);
	this->sharedLibs = sharedLibs;
	this->workingDir = workingDir;

	addBackingFiles();
}

UnwindContext::~UnwindContext() {
	unw_destroy_addr_space(this->addressSpace);
}

unw_addr_space_t UnwindContext::getAddressSpace() {
	return this->addressSpace;
}

UCD_info* UnwindContext::getUcdInfo() {
	return this->ucdInfo;
}

int UnwindContext::getPid() {
	return this->pid;
}

vector<SharedLibFile> UnwindContext::getSharedLibs() {
	return sharedLibs;
}

void UnwindContext::addBackingFiles() {
	for (SharedLibFile file : this->sharedLibs) {
		addSharedLib(file);
	}
}

void UnwindContext::addSharedLib(SharedLibFile file) {
	string realPath;
	string localPath;

	// First check whether the file exists in the working directory
	if (file.getPath().at(0) == '/') {
		localPath = workingDir + file.getPath();
	}
	else {
		localPath = workingDir + "/" + file.getPath();
	}
	if (fileExists(localPath)) {
		realPath = localPath;
	}
	else {
		// If it does not exist, check whether it exists in the filesystem
		if (fileExists(file.getPath())) {
			realPath = file.getPath();
		}
		else {
			printf("Cannot bind shared library %s on address 0x%lX! File not found.\r\n", file.getPath().c_str(), file.getAddress());
			return;
		}
	}

	printf("Binding %s on address range 0x%lX - 0x%lX with offset 0x%lX\r\n", realPath.c_str(), file.getAddress(), file.getEndAddress(), file.getOffset());
	int ret;
	if ((ret = _UCD_add_backing_file_at_vaddr(ucdInfo, file.getAddress(), realPath.c_str())) < 0) {
		printf("Failed (%d)\t%lX:%s\r\n", ret, file.getAddress(), realPath.c_str());
	}
}

bool UnwindContext::fileExists(string path) {
	std::ifstream infile(path);
	return infile.good();
}
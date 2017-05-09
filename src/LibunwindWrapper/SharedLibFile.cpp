#include "SharedLibFile.h"

#include <stdio.h>
#include <stdlib.h>

SharedLibFile::SharedLibFile(char* path, unsigned long offset, unsigned long startAddress, unsigned long endAddress, char* version)
	: path(path), offset(offset), startAddress(startAddress), endAddress(endAddress), version(version) {
}

SharedLibFile::~SharedLibFile() {
}

char* SharedLibFile::getPath() {
	return path;
}

unsigned long SharedLibFile::getOffset() {
	return offset;
}

unsigned long SharedLibFile::getStartAddress() {
	return startAddress;
}

unsigned long SharedLibFile::getEndAddress() {
	return endAddress;
}

char* SharedLibFile::getVersion() {
	return version;
}
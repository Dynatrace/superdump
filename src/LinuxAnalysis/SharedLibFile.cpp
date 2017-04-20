#include <stdio.h>
#include <stdlib.h>

#include "SharedLibFile.h"

using namespace std;

SharedLibFile::SharedLibFile(string path, unsigned long startAddress, unsigned long endAddress, unsigned long offset)
	: path(path), startAddress(startAddress), endAddress(endAddress), offset(offset) {
}

SharedLibFile::~SharedLibFile() {
}

string SharedLibFile::getPath() {
	return path;
}

unsigned long SharedLibFile::getAddress() {
	return startAddress;
}

unsigned long SharedLibFile::getEndAddress() {
	return endAddress;
}

unsigned long SharedLibFile::getOffset() {
	return offset;
}

string SharedLibFile::getName() {
	int lastSlash = this->path.find_last_of('/');
	if (lastSlash == -1) {
		return path;
	}
	string name = this->path.substr(lastSlash + 1);
	int firstDot = name.find(".so");
	if (firstDot > 0) {
		name = name.substr(0, firstDot);
	}
	return name;
}
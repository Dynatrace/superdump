#include "SharedLibFile.h"

#include <stdio.h>
#include <stdlib.h>

SharedLibFile::SharedLibFile(string path, unsigned long startAddress)
	: path(path), startAddress(startAddress) {
}

SharedLibFile::~SharedLibFile() {
}

string SharedLibFile::getPath() {
	return path;
}

unsigned long SharedLibFile::getAddress() {
	return startAddress;
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
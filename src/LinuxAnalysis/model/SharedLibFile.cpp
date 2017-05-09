#include "SharedLibFile.h"

#include <stdio.h>
#include <stdlib.h>

SharedLibFile::SharedLibFile(string path, unsigned long startAddress, unsigned long endAddress, unsigned long offset, string version,
	unsigned long imgBase, unsigned long size, bool managed, unsigned long timestamp)
	: path(path), startAddress(startAddress), endAddress(endAddress), offset(offset), version(version), imgBase(imgBase), size(size), managed(managed), timestamp(timestamp) {
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

string SharedLibFile::getVersion() {
	return version;
}

unsigned long SharedLibFile::getImgBase() {
	return imgBase;
}

unsigned long SharedLibFile::getSize() {
	return size;
}

bool SharedLibFile::isManaged() {
	return managed;
}

unsigned long SharedLibFile::getTimestamp() {
	return timestamp;
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

string SharedLibFile::getTagJson() {
	return "";
}
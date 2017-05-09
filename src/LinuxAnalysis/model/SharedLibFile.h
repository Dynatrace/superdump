#pragma once

#include <string>

using namespace std;

class SharedLibFile {
	string path;
	unsigned long startAddress, endAddress;
	unsigned long offset;
	string version;
	unsigned long imgBase;
	unsigned long size;
	bool managed;
	unsigned long timestamp;

public:
	SharedLibFile(string path, unsigned long startAddress, unsigned long endAddress, unsigned long offset, string version, 
		unsigned long imgBase, unsigned long size, bool managed, unsigned long timestamp);
	~SharedLibFile();

	string getPath();
	unsigned long getAddress();
	unsigned long getEndAddress();
	unsigned long getOffset();
	string getName();
	string getVersion();
	unsigned long getImgBase();
	unsigned long getSize();
	bool isManaged();
	unsigned long getTimestamp();

	string getTagJson();
};
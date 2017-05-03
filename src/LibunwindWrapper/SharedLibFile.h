#pragma once

#include <string>

using namespace std;

class SharedLibFile {
	string path;
	unsigned long startAddress;

public:
	SharedLibFile(string path, unsigned long startAddress);
	~SharedLibFile();

	string getPath();
	unsigned long getAddress();
	string getName();
};
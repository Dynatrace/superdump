#include <string>

#pragma once
class SharedLibFile
{
	std::string path;
	unsigned long startAddress, endAddress;
	unsigned long offset;

public:
	SharedLibFile(std::string path, unsigned long startAddress, unsigned long endAddress, unsigned long offset);
	~SharedLibFile();

	std::string getPath();
	unsigned long getAddress();
	unsigned long getEndAddress();
	unsigned long getOffset();
	std::string getName();
};


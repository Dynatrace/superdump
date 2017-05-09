#pragma once

#include <string>

using namespace std;

struct SharedLibFile {
	char path[512];
	unsigned long offset;
	unsigned long startAddress, endAddress;
};
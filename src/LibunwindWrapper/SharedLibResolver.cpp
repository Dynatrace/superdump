#include "SharedLibResolver.h"

#include <cstdlib>
#include <iostream>
#include <fstream>
#include <stdlib.h>
#include <string.h>

#include <sstream>
#include <algorithm>
#include <iterator>

SharedLibResolver::SharedLibResolver()
{
}


SharedLibResolver::~SharedLibResolver()
{
}

template<typename Out>
void split(const string &s, char delim, Out result) {
	stringstream ss;
	ss.str(s);
	string item;
	while (getline(ss, item, delim)) {
		*(result++) = item;
	}
}


vector<string> split(const string &s, char delim) {
	vector<string> elems;
	split(s, delim, back_inserter(elems));
	return elems;
}

vector<SharedLibFile>* SharedLibResolver::findSharedLibs(string corePath) {
	vector<SharedLibFile>* sharedLibs = new vector<SharedLibFile>();
	string unstripCmd = "readelf -n  " + corePath;
	printf("Executing readelf command: %s\r\n", unstripCmd.c_str());
	
	FILE *handle = popen(unstripCmd.c_str(), "r");
	if (handle == NULL) {
		printf("Failed to execute unstrip command!\r\n");
		return sharedLibs;
	}

	string unstripOut;
	char buf[64];
	size_t readn;
	while ((readn = fread(buf, 1, sizeof(buf), handle)) > 0) {
		unstripOut.append(buf, readn);
	}
	pclose(handle);

	vector<string> lines = split(unstripOut, '\n');
	uint i;
	for (i = 0; i + 1 < lines.size(); i++) 
	{
		if (lines.at(i).find("Page size:") != string::npos) {
			break;
		}
	}
	
	for (i+=2; i + 1 < lines.size(); i += 2) {
		string line = lines.at(i);
		line.append(" ").append(lines.at(i + 1));

		unsigned long startAddr, endAddr, offset;
		char* path = (char*) malloc(1024);
		if (4 == sscanf(line.c_str(), " 0x%lX 0x%lX 0x%lX %s", &startAddr, &endAddr, &offset, path)) {
			SharedLibFile* slf = new SharedLibFile();
			strncpy(slf->path, path, 512);
			slf->offset = offset;
			slf->startAddress = startAddr;
			slf->endAddress = endAddr;
			sharedLibs->push_back(*slf);
		}
	}

	fflush(stdout);
	return sharedLibs;
}

char* SharedLibResolver::chooseRightPath(char* first, char* second, char* third) {
	if (strlen(second) > 3) {
		return second;
	}
	else if (strlen(first) > 3) {
		return first;
	}
	else if (strcmp(".", first) == 0) {
		// Not an actual file (e.g. linux-vdso.so)
		return 0;
	}
	return third;
}
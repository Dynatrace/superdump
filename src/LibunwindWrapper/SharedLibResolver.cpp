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

vector<SharedLibFile> SharedLibResolver::findSharedLibs(string corePath) {
	vector<SharedLibFile> sharedLibs;
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
	bool aligned = false;
	for (int i = 0; i + 1 < lines.size(); i++) {
		string line = lines.at(i);
		if (!aligned) {
			if (line.find("Page size:") != string::npos) {
				aligned = true;
				i++;
				continue;
			}
			else {
				continue;
			}
		}
		line.append(" ").append(lines.at(i + 1));
		i++;

		unsigned long startAddr, endAddr, offset;
		char path[512];
		if (4 == sscanf(line.c_str(), " 0x%lX 0x%lX 0x%lX %s", &startAddr, &endAddr, &offset, path)) {
			sharedLibs.push_back(SharedLibFile(path, startAddr, endAddr, offset, "", 0, 0, false, 0));
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
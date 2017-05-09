#include <string>
#include <vector>

#include "model/SharedLibFile.h"

using namespace std;

#pragma once
class SharedLibResolver
{
public:
	SharedLibResolver();
	~SharedLibResolver();

	vector<SharedLibFile> findSharedLibs(string corePath);

private:
	char* chooseRightPath(char* first, char* second, char* third);
};


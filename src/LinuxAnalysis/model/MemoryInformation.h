#pragma once

#include <string>

using namespace std;

class MemoryInformation
{
public:
	MemoryInformation();
	~MemoryInformation();

	string toJson();
};


#include <string>

using namespace std;

#pragma once
class ArchiveExtractor
{
public:
	ArchiveExtractor();
	~ArchiveExtractor();

	string extractArchiveAndReturnCoredump(string dumppath);
};


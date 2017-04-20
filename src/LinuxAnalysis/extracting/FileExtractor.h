#include <string>

using namespace std;

#pragma once
class FileExtractor
{
public:
	FileExtractor();
	~FileExtractor();

	bool extractFile(string targetDirectory, string filepath);
};


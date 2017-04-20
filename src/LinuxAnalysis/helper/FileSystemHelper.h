#include <vector>
#include <string>

#include <dirent.h>

using namespace std;

#pragma once
class FileSystemHelper
{
private:
	FileSystemHelper();
	~FileSystemHelper();

	static string parentDirectory;

	static void addFilesFromDirectoryRecursively(vector<string>* files, string dir);
	static void processFileOrDirectory(vector<string>* files, struct dirent* entity, string parent);
public:
	static void addFilesFromDirectory(string parentDirectory, vector<string>* files, string directory);
	static void deleteFile(string path);
	static string getParentDir(string filepath);
};


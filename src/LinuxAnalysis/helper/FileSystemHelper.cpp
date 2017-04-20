#include "FileSystemHelper.h"

string FileSystemHelper::parentDirectory = "";

FileSystemHelper::FileSystemHelper()
{
}


FileSystemHelper::~FileSystemHelper()
{
}

void FileSystemHelper::addFilesFromDirectory(string parentDirectory, vector<string>* files, string directory) {
	FileSystemHelper::parentDirectory = parentDirectory;
	addFilesFromDirectoryRecursively(files, directory);
}

void FileSystemHelper::addFilesFromDirectoryRecursively(vector<string>* files, string directory) {
	string dirToOpen = parentDirectory + directory;
	auto dir = opendir(dirToOpen.c_str());

	FileSystemHelper::parentDirectory = dirToOpen + "/";

	if (NULL == dir) {
		return;
	}

	auto entity = readdir(dir);

	while (entity != NULL) {
		processFileOrDirectory(files, entity, parentDirectory);
		entity = readdir(dir);
	}

	// we finished with the directory so remove it from the path
	parentDirectory.resize(parentDirectory.length() - 1 - directory.length());
	closedir(dir);
}

void FileSystemHelper::processFileOrDirectory(vector<string>* files, struct dirent* entity, string parent) {
	if (entity->d_type == DT_DIR) {
		//don't process the  '..' and the '.' directories
		if (entity->d_name[0] == '.') {
			return;
		}
		addFilesFromDirectoryRecursively(files, std::string(entity->d_name));
	}

	if (entity->d_type == DT_REG) {
		files->push_back(parent + entity->d_name);
	}
}

void FileSystemHelper::deleteFile(string path) {
	remove(path.c_str());
}

string FileSystemHelper::getParentDir(string filepath) {
	int lastSlash = filepath.find_last_of("/");
	if (lastSlash == -1) {
		return filepath;
	}
	return filepath.substr(0, lastSlash);
}
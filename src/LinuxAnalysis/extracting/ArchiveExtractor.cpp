#include "ArchiveExtractor.h"

#include <string>
#include <vector>

#include "FileExtractor.h"
#include "../helper/StringHelper.h"
#include "../helper/FileSystemHelper.h"

ArchiveExtractor::ArchiveExtractor() {
}


ArchiveExtractor::~ArchiveExtractor() {
}

string ArchiveExtractor::extractArchiveAndReturnCoredump(string dumppath) {
	FileExtractor extractor;
	
	if (StringHelper::endsWith(dumppath, "/")) {
		dumppath = dumppath.substr(0, dumppath.length() - 1);
	}
	

	vector<string> files;

	bool extracted = false;
	do {
		files.clear();
		FileSystemHelper::addFilesFromDirectory(dumppath, &files, "");

		extracted = false;
		for (string file : files) {
			if (StringHelper::endsWith(file, ".zip") || StringHelper::endsWith(file, ".gz") || StringHelper::endsWith(file, ".tar")) {
				printf("Extracting %s.\r\n", file.c_str());
				string parentDir = FileSystemHelper::getParentDir(file);
				if (extractor.extractFile(parentDir, file.c_str())) {
					extracted = true;
					FileSystemHelper::deleteFile(file);
				}
			}
		}
	} while (extracted);

	// Then search for a core dump
	files.clear();
	FileSystemHelper::addFilesFromDirectory(dumppath, &files, "");
	for (string file : files) {
		if (StringHelper::endsWith(file, ".core")) {
			return file;
		}
	}
	return "";
}
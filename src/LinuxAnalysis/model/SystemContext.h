#include <string>
#include <vector>

#include "SharedLibFile.h"
#include "JsonObject.h"

using namespace std;

#pragma once
class SystemContext : public JsonObject
{
private:
	string* processArch, *systemArch;
	string* dumpTime, *systemUptime, *processUptime;
	string* osVersion;
	int nProcessors;
	// AppDomains
	// SharedDomain
	// SystemDomain
	vector<SharedLibFile> sharedLibs;
	// ClrVersions
public:
	SystemContext(string* processArch, string* systemArch, string* dumpTime, string* systemUptime, string* processUptime, string* osVersion, int nProcessors, vector<SharedLibFile> sharedLibs);
	~SystemContext();

	string toJson();
};


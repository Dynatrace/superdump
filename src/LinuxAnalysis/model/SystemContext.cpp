#include "SystemContext.h"

SystemContext::SystemContext(string* processArch, string* systemArch, string* dumpTime, string* systemUptime, string* processUptime, 
	string* osVersion, int nProcessors, vector<SharedLibFile> sharedLibs)
	: processArch(processArch), systemArch(systemArch), dumpTime(dumpTime), systemUptime(systemUptime), processUptime(processUptime),
	osVersion(osVersion), nProcessors(nProcessors), sharedLibs(sharedLibs) {
}


SystemContext::~SystemContext() {
}

string SystemContext::toJson() {
	string libJson;
	for (SharedLibFile lib : sharedLibs) {
		string tagJson = lib.getTagJson();
		libJson += "{\"Version\": " + fromString(lib.getVersion()) + "," +
			"\"ImageBase\": " + fromInt(lib.getImgBase()) + "," +
			"\"FilePath\": " + fromString(lib.getPath()) + "," +
			"\"FileName\": " + fromString(lib.getName()) + "," +
			"\"FileSize\": " + fromInt(lib.getSize()) + "," +
			"\"IsManaged\": " + fromBool(lib.isManaged()) + ", " +
			"\"TimeStamp\": " + fromInt(lib.getTimestamp()) + "," +
			"\"PdbInfo\": null," +
			"\"Tags\": [ " + tagJson + " ] },";
	}
	libJson = libJson.substr(0, libJson.length() - 1);
	return "\"ProcessArchitecture\": " + fromString(processArch, "N/A") + "," +
		"\"SystemArchitecture\": " + fromString(systemArch, "N/A") + "," +
		"\"DumpTime\": " + fromString(dumpTime, "N/A") + "," +
		"\"SystemUpTime\": " + fromString(systemUptime, "N/A") + "," +
		"\"ProcessUpTime\": " + fromString(processUptime, "N/A") + "," +
		"\"OSVersion\": " + fromString(osVersion, "N/A") + "," +
		"\"NumberOfProcessors\": " + fromInt(nProcessors) + "," +
		"\"AppDomains\": []," +
		"\"SharedDomain\": null," +
		"\"SystemDomain\": null," +
		"\"Modules\": [ " + libJson + " ]," +
		"\"ClrVersions\": []";
}
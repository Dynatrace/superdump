#include "AnalysisInfo.h"



AnalysisInfo::AnalysisInfo(string* filename, string* path, string* jiraIssue, string* friendly, string* serverTimestamp)
	: filename(filename), path(path), jiraIssue(jiraIssue), friendly(friendly), serverTimestamp(serverTimestamp) {
}


AnalysisInfo::~AnalysisInfo()
{
}

string AnalysisInfo::toJson() {
	return "\"FileName\": " + fromString(filename) + "," +
		"\"Path\": " + fromString(path) + "," +
		"\"JiraIssue\": " + fromString(jiraIssue) + "," +
		"\"FriendlyName\": " + fromString(friendly) + "," +
		"\"ServerTimeStamp\": " + fromString(serverTimestamp);
}
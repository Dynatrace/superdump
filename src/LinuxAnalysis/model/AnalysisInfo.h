#pragma once

#include <string>

#include "JsonObject.h"

using namespace std;

class AnalysisInfo : public JsonObject
{
private:
	string* filename, *path, *jiraIssue, *friendly, *serverTimestamp;

public:
	AnalysisInfo(string* filename, string* path, string* jiraIssue, string* friendly, string* serverTimestamp);
	~AnalysisInfo();

	string toJson();
};


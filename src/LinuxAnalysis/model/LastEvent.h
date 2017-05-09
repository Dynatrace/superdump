#include <string>

#include "JsonObject.h"

using namespace std;

#pragma once
class LastEvent : public JsonObject
{
private:
	string type;
	string description;
	int threadId;

public:
	LastEvent(string type, string description, int threadId);
	~LastEvent();

	string toJson();
};


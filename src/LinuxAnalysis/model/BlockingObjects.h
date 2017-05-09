#pragma once

#include <string>

#include "JsonObject.h"

using namespace std;

class BlockingObjects : public JsonObject
{
public:
	BlockingObjects();
	~BlockingObjects();

	string toJson();
};


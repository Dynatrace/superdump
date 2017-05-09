#pragma once

#include "JsonObject.h"
#include <string>

using namespace std;

class TagVector
{
public:
	TagVector();
	~TagVector();

	string toJson();
};


#pragma once

#include <string>

using namespace std;

class JsonObject
{
protected:
	string fromBool(bool b);
	string fromInt(int i);
	string fromUnsLong(unsigned long l);
	string fromString(string s);
	string fromString(string* s, string def = string("null"));
public:
	JsonObject();
	~JsonObject();
};


#include <string>

using namespace std;

#pragma once
class StringHelper
{
private:
	StringHelper();
	~StringHelper();
public:
	static bool endsWith(string str, string end);
};


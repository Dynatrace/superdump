#include "StringHelper.h"

#include <string>

using namespace std;

StringHelper::StringHelper()
{
}


StringHelper::~StringHelper()
{
}

bool StringHelper::endsWith(string str, string end) {
	if (str.length() >= end.length()) {
		return (0 == str.compare(str.length() - end.length(), end.length(), end));
	}
	else {
		return false;
	}
}
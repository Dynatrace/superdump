#include <vector>

#include "StackFrame.h"
#include "../SharedLibFile.h"

using namespace std;

#pragma once
class UnwStackTrace
{
protected:
	vector<StackFrame> stackFrames;
	bool overflow;

public:
	UnwStackTrace(vector<StackFrame> stackFrames, bool overflow);
	~UnwStackTrace();

	void print();

	void writeJson(std::ostream& os);
	string toJson();
};


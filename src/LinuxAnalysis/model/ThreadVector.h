#include <vector>

#include "JsonObject.h"
#include "Thread.h"
#include "../unwind/UnwindContext.h"

#pragma once
class ThreadVector : public JsonObject
{
	vector<Thread> threads;

public:
	ThreadVector(UnwindContext* unwindContext);
	~ThreadVector();

	void printAllStackTraces();

	void writeJson(std::ostream& os);
	string toJson();
};


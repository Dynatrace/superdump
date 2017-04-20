#include <vector>

#include "Thread.h"

#pragma once
class ThreadVector
{
	vector<Thread> threads;

public:
	ThreadVector(UnwindContext* unwindContext);
	~ThreadVector();

	void printAllStackTraces();

	void writeJson(std::ostream& os);
	string toJson();
};


#include <vector>
#include <libunwind-coredump.h>

#include "UnwindContext.h"
#include "UnwStackTrace.h"

#pragma once
class Thread
{
	UnwStackTrace stackTrace;
	int id;

public:
	Thread(int id, UnwStackTrace stackTrace);
	~Thread();

	void printStackTrace();

	int getId();

	void writeJson(std::ostream& os);
	string toJson();
};


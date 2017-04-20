#include "Thread.h"

#pragma once
class ThreadUnwinder
{
public:
	ThreadUnwinder();
	~ThreadUnwinder();

	Thread unwind(UnwindContext* unwindContext, int nThread);
};


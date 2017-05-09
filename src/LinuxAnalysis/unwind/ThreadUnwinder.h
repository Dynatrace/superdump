#include "../model/Thread.h"
#include "UnwindContext.h"

#pragma once
class ThreadUnwinder
{
public:
	ThreadUnwinder();
	~ThreadUnwinder();

	Thread unwind(UnwindContext* unwindContext, int nThread);
};


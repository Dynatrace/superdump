#include "ThreadUnwinder.h"

#include "StacktraceUnwinder.h"

#include <libunwind-coredump.h>

ThreadUnwinder::ThreadUnwinder()
{
}


ThreadUnwinder::~ThreadUnwinder()
{
}

Thread ThreadUnwinder::unwind(UnwindContext* unwindContext, int nThread) {
	unw_cursor_t cursor;
	_UCD_select_thread(unwindContext->getUcdInfo(), nThread);

	int ret = unw_init_remote(&cursor, unwindContext->getAddressSpace(), unwindContext->getUcdInfo());
	if (ret != 0) {
		printf("Failed to initiate remote session for thread %d: %d", nThread, ret);
		fflush(stdout);
	}

	StacktraceUnwinder unwinder;
	return Thread(nThread, unwinder.unwind(cursor, unwindContext->getSharedLibs()));
}
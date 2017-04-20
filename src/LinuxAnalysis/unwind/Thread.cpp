#include "Thread.h"

#include <libunwind-coredump.h>
#include "StacktraceUnwinder.h"

Thread::Thread(int id, UnwStackTrace stackTrace) 
	: id(id), stackTrace(stackTrace) {
}


Thread::~Thread() {
}

int Thread::getId() {
	return this->id;
}

void Thread::printStackTrace() {
	this->stackTrace.print();
}

void Thread::writeJson(std::ostream& os) {

}

string Thread::toJson() {
	return "thread: { id:" + to_string(id) + ", stacktrace: {" + stackTrace.toJson() + "}}";
}
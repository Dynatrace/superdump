#include "Thread.h"

#include <libunwind-coredump.h>
#include "../unwind/StacktraceUnwinder.h"

Thread::Thread(int id, int engineId, int osId, int managedThreadId, bool managed, string name, UnwStackTrace stackTrace, TagVector tags, BlockingObjects blockingObjects,
	unsigned long creationTime, unsigned long exitTime, unsigned long kernelTime, unsigned long userTime, unsigned long startOffset, int exitStatus, int priority,
	int priorityClass, string* state, bool threadPool)
	: id(id), engineId(engineId), osId(osId), managedThreadId(managedThreadId), managed(managed), name(name), stackTrace(stackTrace), tags(tags), blockingObjects(blockingObjects),
	creationTime(creationTime), exitTime(exitTime), kernelTime(kernelTime), userTime(userTime), startOffset(startOffset), exitStatus(exitStatus), priority(priority),
	priorityClass(priorityClass), state(state), threadPool(threadPool) {
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
	return "\"Index\":" + fromInt(id) + "," +
		"\"EngineId\": " + fromInt(engineId) + "," +
		"\"OsId\": " + fromInt(osId) + "," +
		"\"ManagedThreadId\": " + fromInt(managedThreadId) + "," +
		"\"IsManagedThread\": " + fromBool(managed) + "," +
		"\"ThreadName\": " + fromString(name) + "," +
		"\"StackTrace\": " + stackTrace.toJson() + "," +
		"\"Tags\": " + tags.toJson() + "," +
		"\"LastException\": null," +
		"\"BlockingObjects\": " + blockingObjects.toJson() + "," +
		"\"CreationTime\": " + fromUnsLong(creationTime) + "," +
		"\"ExitTime\": " + fromUnsLong(exitTime) + "," +
		"\"KernelTime\": " + fromUnsLong(kernelTime) + "," +
		"\"UserTime\": " + fromUnsLong(userTime) + "," +
		"\"StartOffset\": " + fromInt(startOffset) + "," +
		"\"ExitStatus\": " + fromInt(exitStatus) + "," +
		"\"Priority\": " + fromInt(priority) + "," +
		"\"PriorityClass\": " + fromInt(startOffset) + "," +
		"\"State\": " + fromString(state) + "," +
		"\"IsThreadPoolThread\": " + fromBool(threadPool);
}
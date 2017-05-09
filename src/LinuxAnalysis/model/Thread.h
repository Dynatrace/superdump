#include <vector>
#include <libunwind-coredump.h>

#include "JsonObject.h"
#include "UnwStackTrace.h"
#include "TagVector.h"
#include "BlockingObjects.h"

#pragma once
class Thread : public JsonObject
{
	int id;
	UnwStackTrace stackTrace;
	int engineId, osId, managedThreadId;
	bool managed;
	string name;
	TagVector tags;
	BlockingObjects blockingObjects;
	unsigned long creationTime, exitTime, kernelTime, userTime, startOffset;
	int exitStatus, priority, priorityClass;
	string* state;
	bool threadPool;

public:
	Thread(int id, int engineId, int osId, int managedThreadId, bool managed, string name, UnwStackTrace stackTrace, TagVector tags, BlockingObjects blockingObjects, 
		unsigned long creationTime, unsigned long exitTime, unsigned long kernelTime, unsigned long userTime, unsigned long startOffset, int exitStatus, int priority, 
		int priorityClass, string* state, bool threadPool);
	~Thread();

	void printStackTrace();

	int getId();

	void writeJson(std::ostream& os);
	string toJson();
};


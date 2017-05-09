#pragma once

#include "JsonObject.h"
#include "AnalysisInfo.h"
#include "ThreadVector.h"
#include "SystemContext.h"
#include "LastEvent.h"
#include "DeadlockInformation.h"
#include "BlockingObjects.h"
#include "MemoryInformation.h"

class AnalysisResult : public JsonObject
{
private:
	bool isManagedProcess;
	int lastExecutedThreadId;
	AnalysisInfo analysisInfo;
	SystemContext systemContext;
	LastEvent lastEvent;
	// ExceptionRecord
	ThreadVector threadVector;
	DeadlockInformation deadlockInformation;
	BlockingObjects blockingObjects;
	MemoryInformation memInfo;
public:
	AnalysisResult(bool isManagedProcess, int lastExecutedThreadId, AnalysisInfo analysisInfo, SystemContext systemContext, LastEvent lastEvent, 
		ThreadVector threads, DeadlockInformation deadlockInformation, BlockingObjects blockingObjects, MemoryInformation memInfo);
	~AnalysisResult();

	string toJson();
};


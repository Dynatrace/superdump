#include "AnalysisResult.h"

#include <string>

using namespace std;

AnalysisResult::AnalysisResult(bool isManagedProcess, int lastExecutedThreadId, AnalysisInfo analysisInfo, SystemContext systemContext, LastEvent lastEvent, 
	ThreadVector threads, DeadlockInformation deadlockInformation, BlockingObjects blockingObjects, MemoryInformation memInfo)
	: isManagedProcess(isManagedProcess), lastExecutedThreadId(lastExecutedThreadId), analysisInfo(analysisInfo), systemContext(systemContext), 
		lastEvent(lastEvent), threadVector(threads), deadlockInformation(deadlockInformation),
		blockingObjects(blockingObjects), memInfo(memInfo)
{
}


AnalysisResult::~AnalysisResult()
{
}

string AnalysisResult::toJson() {
	return "{ \"IsManagedProcess\": " + fromBool(isManagedProcess) + "," +
		"\"LastExecutedThread\": " + fromInt(lastExecutedThreadId) + "," +
		"\"AnalysisInfo\": { " + analysisInfo.toJson() + " }," +
		"\"SystemContext\": { " + systemContext.toJson() + " }," +
		"\"LastEvent\": { " + lastEvent.toJson() + " }," +
		"\"ExceptionRecord\": []," +
		"\"ThreadInformation\": { " + threadVector.toJson() + " }," +
		"\"DeadlockInformation\": []," +
		"\"BlockingObjects\": []," +
		"\"MemoryInformation\": { " + memInfo.toJson() + " }," +
		"\"NotLaodedSymbols\": []" +
		" }";
}
#include "LastEvent.h"



LastEvent::LastEvent(string type, string description, int threadId)
	: type(type), description(description), threadId(threadId) {
}


LastEvent::~LastEvent()
{
}

string LastEvent::toJson() {
	return "\"Type\": " + fromString(type) + "," +
		"\"Description\": " + fromString(description) + ", " +
		"\"ThreadId\": " + fromInt(threadId);
}
#include "UnwStackTrace.h"

UnwStackTrace::UnwStackTrace(vector<StackFrame> stackFrames, bool overflow)
	:stackFrames(stackFrames), overflow(overflow) {
}

UnwStackTrace::~UnwStackTrace() {
}

void UnwStackTrace::print() {
	printf("Type\tStack Ptr\t\tInstruction Ptr\t\tReturn Address\t\tMethod Name + Offset\r\n");
	for (StackFrame frame : this->stackFrames) {
		frame.print();
	}
	if (overflow) {
		printf("-\t-\t\t\t-\t\t\t-\t\t\tOverflow (depth exceeded)");
	}
}

void UnwStackTrace::writeJson(std::ostream& os) {
	
}

string UnwStackTrace::toJson() {
	string json = "trace: { frames: [";
	for (StackFrame stackFrame : stackFrames) {
		json += "{" + stackFrame.toJson() + "},";
	}
	json = json.substr(0, json.length() - 1);
	json += "], overflow:";
	json += (overflow ? "true" : "false");
	json += "}";
	return json;
}
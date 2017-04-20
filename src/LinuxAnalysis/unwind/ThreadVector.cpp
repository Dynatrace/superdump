#include "ThreadVector.h"

#include "ThreadUnwinder.h"

ThreadVector::ThreadVector(UnwindContext* unwindContext) {
	int threadCount = _UCD_get_num_threads(unwindContext->getUcdInfo());

	ThreadUnwinder unwinder;
	for (int nThread = 0; nThread < threadCount; nThread++) {
		this->threads.push_back(unwinder.unwind(unwindContext, nThread));
	}
}

ThreadVector::~ThreadVector() {
}

void ThreadVector::printAllStackTraces() {
	for (Thread thread : threads) {
		printf("Thread %d\r\n", thread.getId());
		thread.printStackTrace();
		printf("\r\n\r\n");
	}
}

void ThreadVector::writeJson(std::ostream& os) {

}

string ThreadVector::toJson() {
	string json = "threads: [ ";

	for (Thread thread : threads) {
		json += "{ " + thread.toJson() + "},";
	}

	return json.substr(0, json.length() - 1) + "]";
}
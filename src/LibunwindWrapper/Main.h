#pragma once

#include "SharedLibFile.h"

#ifndef MYAPI
#define MYAPI 
#endif

#ifdef __cplusplus
extern "C" {
#endif

MYAPI void init(const char* filepath, const char* workingDir);

MYAPI int getNumberOfThreads();

MYAPI int getThreadId();

MYAPI void selectThread(int threadNumber);

MYAPI unsigned long getInstructionPointer();

MYAPI unsigned long getStackPointer();

MYAPI char* getProcedureName();

MYAPI unsigned long getProcedureOffset();

MYAPI bool step();

MYAPI unsigned long getAuxvValue(int type);

MYAPI const char* getAuxvString(int type);

MYAPI bool getSharedLibs(int* size, SharedLibFile** libs);

#ifdef __cplusplus
}
#endif
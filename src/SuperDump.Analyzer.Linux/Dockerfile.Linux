FROM dotstone/sdlinux:base

COPY LibunwindWrapper /wrappersrc
COPY SuperDump.Analyzer.Linux /opt/SuperDump.Analyzer.Linux

RUN /bin/bash -c "g++ -c -x c++ /wrappersrc/LibunwindWrapper.cpp -I /usr/local/include -g2 -gdwarf-2 -o "/opt/LibunwindWrapper.o" -Wall -Wswitch -W"no-deprecated-declarations" -W"empty-body" -Wconversion -W"return-type" -Wparentheses -W"no-format" -Wuninitialized -W"unreachable-code" -W"unused-function" -W"unused-value" -W"unused-variable" -O0 -fno-strict-aliasing -fno-omit-frame-pointer -fpic -fthreadsafe-statics -fexceptions -frtti -std=c++11; g++ -c -x c++ /wrappersrc/Main.cpp -I /usr/local/include -g2 -gdwarf-2 -o "/opt/Main.o" -Wall -Wswitch -W"no-deprecated-declarations" -W"empty-body" -Wconversion -W"return-type" -Wparentheses -W"no-format" -Wuninitialized -W"unreachable-code" -W"unused-function" -W"unused-value" -W"unused-variable" -O0 -fno-strict-aliasing -fno-omit-frame-pointer -fpic -fthreadsafe-statics -fexceptions -frtti -std=c++11; g++ -o "/usr/lib/unwindwrapper.so" -Wl,--no-undefined -Wl,-L"/usr/lib/x86_64-linux-gnu" -Wl,-L"/lib/x86_64-linux-gnu" -Wl,-z,relro -Wl,-z,now -Wl,-z,noexecstack -shared /opt/LibunwindWrapper.o /opt/Main.o /usr/local/lib/libunwind-coredump.a -l"unwind-x86_64";mkdir /opt/dump"

# rsync returns exitcode 24 if a file that should be copied vanishes while the copy is in progress. 
# This can happen quite often, since e.g. symbol resolving creates temporary files.
# "|| [ $? == 24" checks if the exit code of rsync is 24 and continues with the command in that case.
CMD ( rsync -a /dump /opt/ || [ "$?" = "24" ] ) && cd /opt/dump/ && dotnet /opt/SuperDump.Analyzer.Linux/SuperDump.Analyzer.Linux.dll /opt/dump/ /dump/superdump-result.json && cp /opt/dump/*.log /dump/
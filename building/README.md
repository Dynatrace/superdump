Building SuperDump
==================
There are different scripts that can be used to build SuperDump:
 * build-windows-all.cmd
   This script builds the SuperDump Service and the Windows Analysis components. For this all required Programs have to be installed on the system. 
   The basic requirements are:
	* Visual Studio 2017
	* .Net Core SDK 2.2
	* .Net Framework 4.6
    * DebugDiag

 * build-docker-linux-analysis.cmd
   This script builds the Linux Analyzer and creates a docker container containing it. Docker has to be installed for this.

 * build-all.cmd
   This script builds the Windows and Linux components.

 * build-docker-windows-build-image.cmd
   This creates a docker image that contains all requirements for building the Windows components.

 * build-windows-all-with-docker.cmd
   Builds the Windows comnponents using the image created by the previous script

 * build-docker-windows-superdumpservice.cmd
   This script creates a docker image containing the SuperDumpService and the Windows Analysis Components.
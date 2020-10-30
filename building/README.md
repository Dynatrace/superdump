Building SuperDump
==================
There are different scripts that can be used to build SuperDump:
 * build-windows-all.cmd
   This script builds the SuperDump Service and the Windows Analysis components. For this all required Programs have to be installed on the system. 
   The basic requirements are:
	* Visual Studio 2019
	* .Net Core SDK 5.0
	* .Net Framework 4.8
  * DebugDiag

 * build-docker-linux-analysis.cmd
   This script builds the Linux Analyzer and creates a docker image containing it. Docker has to be installed for this.

 * build-all.cmd
   This script builds the Windows and Linux components.

 * build-docker-windows-build-image.cmd
   This creates a docker image that contains all requirements for building the Windows components.

 * build-windows-all-with-docker.cmd
   Builds the Windows components using the image created by the previous script

 * build-docker-windows-superdumpservice.cmd
   This script creates a docker image containing the SuperDumpService and the Windows Analysis Components.

 * build.cmd
   Searches for a Visual Studio installation and builds the Project as defined in the msbuild.targets file.
   This is used by the other build scripts.
SuperDump
=========

*SuperDump* is a service for **_automated Windows crash-dump analysis_**. 

SuperDump was made with these goals in mind: 
 * Make crash-dump analysis easy for people who are unexperienced with it, or don't have the necessary tools installed.
 * Speed up first assessment of a crash-dump, by automatically preparing crash-dump analysis up-front. A developer should be quicker in determining if it's an already known crash.

What SuperDump is not: 
  * A replacement for in-depth analysis tools such as WinDbg.

Features
========
 * Dump analysis can be triggered via web-frontend (HTTP-upload) or via REST-API.
 * Any windows-crash-dump (Fulldump or Minidump) can be analyzed (`*.dmp` files).
 * `.zip` files containing multiple crash-dumps are also supported.
 * Report results are stored as `.json` files and can be queried via REST-API. But they can also be viewed in SuperDump directly.
 * SuperDump report shows: 
   * Basic information (bitness, system/process uptime, lastevent, ...)
   * Loaded modules and versions
   * Stacktraces of all threads (native and .NET frames)
   * AppDomains
   * Basic memory analyis (number of bytes used by .NET types)
 * SuperDump detects exceptions (native and managed) and marks the responsible threads.
 * Deadlock detection.
 * SuperDump also invokes a number of `WinDbg` commands and logs them to a separate log-file.

<a href="doc/img/mainpage.png"><img src="doc/img/mainpage.png" title="main page" width="200"/></a>
<a href="doc/img/managednativestacktrace.png"><img src="doc/img/managednativestacktrace.png" title="native managed"  width="200"/></a>
<a href="doc/img/nativeexception.png"><img src="doc/img/nativeexception.png" title="native exception" width="200"/></a>
<a href="doc/img/managedexception.png"><img src="doc/img/managedexception.png" title="managed exception" width="200"/></a>

Technologies
============
 * [CLRMD] for analysis.
 * [ASP.NET Core] and [Razor] for web-frontend and api.
 * [Hangfire] for task scheduling.
 
 [CLRMD]: https://github.com/Microsoft/clrmd
 [ASP.NET Core]: https://github.com/aspnet/Home
 [Razor]: https://github.com/aspnet/Razor
 [Hangfire]: https://github.com/HangfireIO/Hangfire

Build
=====

 * Prerequisites:
   * Visual Studio 2017 RC3
   * .NET Core Tooling RC3 (1.0.0-rc3-004530)
   * .NET Core 1.1
   * .NET Framework 4.6
   * LocalDB
 * Build via `building/build.cmd`
 * Run via `build/runsuperdump.cmd` (defaults to port 5000)

State of the project
====================
SuperDump has been created at [Dynatrace] as an internship project in 2016. It turned out to be pretty useful so we thought it might be useful for others too. Thus we decided to opensource it.

Though it currently works great for us at Dynatrace, there are areas that need to be improved to make it a high-quality and generally useful tool:

 * Test-Coverage: A couple of unit tests are there, but there is currently no CI to automatically run them. The tests partially depend on actual dump-files being available, which obviously are not in source control. We'd need some binary-store, a prepare/download step, etc to make those run.
 * Some stuff is tailored for our needs at Dynatrace. E.g. There is a field to link analysis to Jira-issues, which obviously will not fit for everyone. Also, we have special detection for Dynatrace Agent stackframes. While this feature probably won't hurt anyone else, it is kind of unclean to have such special detection in place.
 * There is currently no data retention stuff implemented. Every crash-dump is stored forever. Cleanup needs to be done manually.
 * There is no authentication/authorization implemented. Every crash-dump is visible to everyone and can be downloaded by everyone. This is an important fact, because crash-dump contents can be highly security critical.

Future
======
We've open sourced SuperDump, because we believe it can be helpful for others. Anyone is welcome to contribute to SuperDump. In small ways, or in ways we have not thought about yet. Feedback, github tickets, as well as PR's are welcome.

Some high-level ideas we've been poking around: 

 * _Pluggable analyzers:_ Possibility to write your own analyzers, detached from the main project and pluggable.
 * _Linux coredumps:_ Use Linux/GDB to automatically analyze linux coredumps, and use SuperDumpService as frontend (for file upload, REST-API, view reports).
 * _Descriptive summaries:_ The idea is to put the most likely crash-reason in a short descriptive summary text. It can be used to find duplicate crashes and cluster similar crashes.
 * _Integrate DebugDiag:_ DebugDiag can do some great analysis. It would be great if SuperDump would invoke DebugDiag analysis automatically and would just offer the result (`.mht`) file directly.

Credit
======
Most of the initial code base was written by [Andreas Lobmaier] in his summer internship of 2016. It's been maintained and further developed since then by [Christoph Neumüller] and other folks at [Dynatrace].

[Andreas Lobmaier]: https://github.com/alobmaier
[Christoph Neumüller]: https://github.com/discostu105
[Dynatrace]: https://www.dynatrace.com

License
=======
[MIT]

[MIT]: https://github.com/Dynatrace/superdump/blob/master/LICENSE

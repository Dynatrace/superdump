Although you can analyze plain core dumps, the output will be scrambled and you will not get much useful information from the backtrace due to missing symbol information. This symbol information must be submitted together with the core dump in an archive.

The structure of such an archive must adhere to the following pattern:
* \<corefile\>.core
* libs.tar.gz
  * /usr/lib/shared-lib-1.so
  * /home/user/shared-lib-2.so
  * ...
* summary.txt (optional)
* \<corefile\>.log (optional)

Note that the libraries must be stored in the full absolute path that was used in the target system. The `summary.txt` and `<corefile>.log` are optional. If you want to have further information, please checkout the master thesis: http://epub.jku.at/obvulihs/download/pdf/2581999 

bundleid=$1
dumpid=$2
exec_file=$3
command=$4

mkdir -p /opt/dump/$bundleid/$dumpid
rsync -a /dumps/$bundleid/$dumpid/ /opt/dump/$bundleid/$dumpid
cd /opt/dump/$bundleid/$dumpid
dotnet /opt/SuperDump.Analyzer.Linux/SuperDump.Analyzer.Linux.dll -prepare /opt/dump/$bundleid/$dumpid
dump_file=$(find /opt/dump/$bundleid/$dumpid -name '*.core' -print -quit)

export TERM=xterm
gdb -ex 'set solib-absolute-prefix .' -ex 'file '"$exec_file" -ex 'core-file '"$dump_file" -ex 'directory /opt/dump/'"$bundleid"'/'"$dumpid"'/sources/' -ex "$command"
rm -R /opt/dump/$bundleid/$dumpid

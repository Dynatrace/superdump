FROM ubuntu

COPY . /libunwind

RUN apt-get update && \
	apt-get install -y apt-transport-https && \
	echo "deb [arch=amd64] https://apt-mo.trafficmanager.net/repos/dotnet-release/ xenial main" > /etc/apt/sources.list.d/dotnetdev.list && \
	apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 417A0893 && \
	apt-get update && \
	apt-get upgrade -y && \
	apt-get install -y build-essential gdb dotnet-dev-1.0.4 rsync openssh-server && \
	apt-get clean

RUN cd /libunwind && \
	./configure CFLAGS="-fPIC" && \
	make && \
	make install && \
	rm -R /libunwind
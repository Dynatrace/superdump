FROM ubuntu

RUN apt-get update; apt-get install -y wget openssh-server sshpass
RUN wget -O /root/gotty.tar.gz https://github.com/yudai/gotty/releases/download/v1.0.0/gotty_linux_amd64.tar.gz; gunzip /root/gotty.tar.gz; tar xf /root/gotty.tar -C /root/; echo SendEnv REPOSITORY_URL >> /etc/ssh/ssh_config; echo SendEnv REPOSITORY_AUTH >> /etc/ssh/ssh_config

CMD /root/gotty -p "3000" --title-format "GDB Interactive Session ({{ .Hostname }})" -w --permit-arguments sshpass -p $SSH_PASSWD ssh -t -o StrictHostKeyChecking=no $SSH_USER@$SSH_HOST /opt/SuperDump.Analyzer.Linux/gdb.sh
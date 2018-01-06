FROM mono:5.2
EXPOSE 5000 6000 7000 8000 9000
COPY bin/ /disco
RUN apt-get update
RUN apt-get install -y git-core
CMD mono disco/Disco/disco.exe setup -y && mono disco/Disco/disco.exe start
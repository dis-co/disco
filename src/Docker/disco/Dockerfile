FROM disco:base

ENV disco /disco
ENV project /project
ENV start $disco/start.sh

RUN mkdir -p $disco
RUN mkdir -p $project

VOLUME $project

ADD . $disco

RUN cp /usr/lib/libgit2.so.24 $disco/lib/linux/x86_64/libgit2-baa87df.so

RUN chmod +x $disco/start.sh

CMD [ "/disco/start.sh" ]
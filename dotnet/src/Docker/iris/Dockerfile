FROM iris:base

ENV iris /iris
ENV project /project
ENV start $iris/start.sh

RUN mkdir -p $iris
RUN mkdir -p $project

VOLUME $project

ADD . $iris

RUN cp /usr/lib/libgit2.so.24 $iris/lib/linux/x86_64/libgit2-baa87df.so

RUN chmod +x $iris/start.sh

CMD [ "/iris/start.sh" ]
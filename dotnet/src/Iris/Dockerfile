FROM alpine:latest

ENV iris /iris
ENV project /project
ENV start $iris/start.sh

RUN echo "http://dl-3.alpinelinux.org/alpine/edge/testing/" >> /etc/apk/repositories
RUN echo "http://dl-3.alpinelinux.org/alpine/edge/main/" >> /etc/apk/repositories
RUN apk update
RUN apk add --no-cache python2 mono libzmq libgit2 libgit2-libs

RUN mkdir -p $iris
RUN mkdir -p $project

VOLUME $project

ADD . $iris

RUN cp /usr/lib/libzmq.so.5 $iris/amd64/libzmq.so
RUN cp /usr/lib/libsodium.so.18 $iris/amd64/libsodium.so

RUN chmod +x $iris/start.sh

CMD [ "/iris/start.sh" ]
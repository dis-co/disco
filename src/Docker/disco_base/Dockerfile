FROM alpine:latest

RUN echo "http://dl-3.alpinelinux.org/alpine/edge/testing/" >> /etc/apk/repositories
RUN echo "http://dl-3.alpinelinux.org/alpine/edge/main/" >> /etc/apk/repositories
RUN apk update
RUN apk add --no-cache python2 git git-daemon mono libzmq libgit2 libgit2-libs

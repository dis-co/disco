## Quick Start

### Introduction

In this quick start guide, we'll demonstratet how to start a 2-node
cluster on a single machine. This setup is of course somewhat
contrived, but serves to demonstrate the workflow.

### Getting Iris

Use
[this link to download](https://ci.appveyor.com/api/projects/nsynk/iris/artifacts/Iris-latest.zip) the
latest Iris build. This always contains the latest build of the git
master branch that has passed all automated tests.

### Setup

Unzip the archive to `C:\Users\youruser\Desktop`, and rename it to
`iris-1`. Unzip the archive once more, this time to `iris-2`.

Open two `cmd.exe` windows, and navigate each of them to `iris-1` and
`iris-2`, respectively.

To prepare and instance of the _Iris_ daemon for operation, a machine
ID for that instance needs to be generated. This ID is saved in a
configuration file called `machineconfig.yaml` in the `etc/` folder
next to the `iris.exe` binary. To achieve this you can use the
`runiris.cmd` script:

```
.\runiris.cmd setup
```

Repeat this command in the second `cmd.exe` window for `iris-2`.

Next, we will create a new project! To do this, we will use `iris-1`
to run the following command:


```
.\runiris.exe create --dir=..\cool-project-1 --bind=127.0.0.1 --raft=6000 --web=7000 --ws=8000 --git=5000
```

This will create the project in
`C:\Users\youruser\Desktop\cool-project-1`. Next, we will need to add
the other node as part of the project:

```
.\runiris.exe add-member --dir=..\cool-project-1
```

This will prompt you to enter the machine ID of the second Iris
instance, which you can find in
`C:\Users\youruser\Desktop\iris-2\Iris\etc\machineconfig.yaml`. Paste
the ID, and add the following information:

- Raft: 6001
- Web: 7001
- Sockets: 8001
- Git: 5001

Now, copy `C:\Users\youruser\Destkop\cool-project-1` to
`C:\Users\youruser\Destkop\cool-project-2` and use the following
commands in the respective `cmd.exe` windows to start the cluster:

```
:: in C:\Users\youruser\Desktop\iris-1
.\runiris.cmd start --dir=..\cool-project-1

:: in C:\Users\youruser\Desktop\iris-2
.\runiris.cmd start --dir=..\cool-project-2
```

After a while, a leader should be elected and you should be able to
navigate your browser either to `http://localhost:7000` or
`http://localhost:7001` to log in to Iris.

Note: currently we don't have redirection implemented, so if you see a
blank page on either URL's, please try the other one.

### Logging in

The current user is always `admin` with `Nsynk` as password.


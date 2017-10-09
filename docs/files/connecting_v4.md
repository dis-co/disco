# Connecting with VVVV

> These instructions are intended for Windows, as VVVV is only compatible with this operation system.

## Requirements

- Windows
- [Git](https://git-scm.com/)
- [VVVV](https://vvvv.org/)
- Iris build

If you don't have an iris build you can create it from source if you have access to the [repository](https://bitbucket.org/nsynk/iris).

- Clone the repository (check Bitbucket website for instructions)
- Go to the cloned repository directory (e.g. `cd iris`)
- If you had already done the two previous steps, you may need to go to master folder (`git checkout master`) and clean the directory (`git clean -xfd`)
- Create a build by typing `.\build.cmd`. After a few moments, the build will be created in the `bin` subdirectory (there will be also a zip file `Iris-latest.zip` for distribution)

## Configuring VVVV

You need to tell VVVV where to find Iris special nodes. For this, edit the file named `root.v4p` next to `vvvv.exe` to add the path to the `Nodes` folder in the Iris build. See image below.

> Attention, VVVV doesn't allow modifications to `root.v4p` file while it's next to `vvvv.exe`, so first you will need to **move the file to a different folder, edit it and then move it back**.

![root.v4p](../img/rootv4p.png)

Once you have this, you need a VVVV patch to test Iris connection. You can get it from [this Trello card](https://trello.com/c/ZTYxYgme/108-iris-v4-patch). When you open the patch, you should see a couple of nodes to introduce the Iris server IP and Port info. If you are running Iris on the same machine, you can check the **IPv4 Address** of your system on Windows with the command `ipconfig`.

![Iris server nodes](../img/iris_server_nodes.png)

## Running Iris

Assumming you're in the directory of the Iris build (e.g. `cd iris\bin`), you can run Iris by typing the following command:

```cmd
.\Iris\iris.exe start --machine=C:\Iris\etc --bind=192.168.8.11
```

This means Iris will save configuration, logs, project info... in the `C:\Iris` directory. In this example, you can check the Iris machine configuration (including the IP address and the ports used) in the `C:\Iris\etc\machinecfg.yaml` file. The second parameter `--bind` tells Iris the IP Address of the server and it's only needed the first time you run it, please replace the value in the example with the IP address of your system (check `ipconfig`).

> If you've run a previous Iris build before, you may want to delete the `C:\Iris` directory beforehand to avoid conflicts.

Once the Iris server is running, you can navigate to the `http://192.168.8.11:7000/` in your browser (again, please replace the IP address with the appropriate one in your system). If no project is available, Iris will ask you to type a name to create a new one. After that, you will be asked to log in, for testing purposes use: `admin` & `Nsynk`.

> In this tutorial we're using the default Iris ports (e.g. 7000 for the web port), you can check the ports used in the `machinecfg.yaml` file.

If everything has gone without problems, you should be able to see the exposed VVVV nodes by opening the Graph View (you may need to reopen the VVVV patch or right-click a `Reconnect` bang if available).

![Iris & VVVV working together](../img/connected_patch.png)

> To stop the iris service you must use Ctrl+C, don't close the command window. This is because Iris needs to kill the git daemon. If it's not killed the port will be occupied and you won't be able to run iris again.

## Troubleshooting

- At the time of writing, Iris error messages are not very informative when there's a typo in the IP address. So please make sure **the IP address is written correctly both in VVVV patch and Iris configuration** (e.g. `C:\Iris\etc\machinecfg.yaml`).

- Make sure no other application is occupying Iris ports. Again, you can check the ports used by Iris in the `machinecfg.yaml` file. In Windows, you can see the list of ports in use with Task Manager > Performance > Open Resource Monitor > Network > Listening Ports.

![Listening Ports](../img/listening_ports.png)
# Connecting with VVVV

> These instructions are intended for Windows, as VVVV is only compatible with this operation system.

## Requirements

- Windows
- [Git](https://git-scm.com/)
- [VVVV](https://vvvv.org/)
- Disco build

If you don't have an disco build you can create it from source if you have access to the [repository](https://bitbucket.org/nsynk/disco).

- Clone the repository (check Bitbucket website for instructions)
- Go to the cloned repository directory (e.g. `cd disco`)
- If you had already done the two previous steps, you may need to go to master folder (`git checkout master`) and clean the directory (`git clean -xfd`)
- Create a build by typing `.\build.cmd`. After a few moments, the build will be created in the `bin` subdirectory (there will be also a zip file `Disco-latest.zip` for distribution)

## Configuring VVVV

You need to tell VVVV where to find Disco special nodes. For this, edit the file named `root.v4p` next to `vvvv.exe` to add the path to the `Nodes` folder in the Disco build. See image below.

> Attention, VVVV doesn't allow modifications to `root.v4p` file while it's next to `vvvv.exe`, so first you will need to **move the file to a different folder, edit it and then move it back**.

![root.v4p](../img/rootv4p.png)

Once you have this, you need a VVVV patch to test Disco connection. You can get it from [this Trello card](https://trello.com/c/ZTYxYgme/108-disco-v4-patch). When you open the patch, you should see a couple of nodes to introduce the Disco server IP and Port info. If you are running Disco on the same machine, you can check the **IPv4 Address** of your system on Windows with the command `ipconfig`.

![Disco server nodes](../img/disco_server_nodes.png)

## Running Disco

Assumming you're in the directory of the Disco build (e.g. `cd disco\bin`), you can run Disco by typing the following command:

```cmd
.\Disco\disco.exe start --machine=C:\Disco\etc --bind=192.168.8.11
```

This means Disco will save configuration, logs, project info... in the `C:\Disco` directory. In this example, you can check the Disco machine configuration (including the IP address and the ports used) in the `C:\Disco\etc\machinecfg.yaml` file. The second parameter `--bind` tells Disco the IP Address of the server and it's only needed the first time you run it, please replace the value in the example with the IP address of your system (check `ipconfig`).

> If you've run a previous Disco build before, you may want to delete the `C:\Disco` directory beforehand to avoid conflicts.

Once the Disco server is running, you can navigate to the `http://192.168.8.11:7000/` in your browser (again, please replace the IP address with the appropriate one in your system). If no project is available, Disco will ask you to type a name to create a new one. After that, you will be asked to log in, for testing purposes use: `admin` & `Nsynk`.

> In this tutorial we're using the default Disco ports (e.g. 7000 for the web port), you can check the ports used in the `machinecfg.yaml` file.

If everything has gone without problems, you should be able to see the exposed VVVV nodes by opening the Graph View (you may need to reopen the VVVV patch or right-click a `Reconnect` bang if available).

![Disco & VVVV working together](../img/connected_patch.png)

> To stop the disco service you must use Ctrl+C, don't close the command window. This is because Disco needs to kill the git daemon. If it's not killed the port will be occupied and you won't be able to run disco again.

## Troubleshooting

- At the time of writing, Disco error messages are not very informative when there's a typo in the IP address. So please make sure **the IP address is written correctly both in VVVV patch and Disco configuration** (e.g. `C:\Disco\etc\machinecfg.yaml`).

- Make sure no other application is occupying Disco ports. Again, you can check the ports used by Disco in the `machinecfg.yaml` file. In Windows, you can see the list of ports in use with Task Manager > Performance > Open Resource Monitor > Network > Listening Ports.

![Listening Ports](../img/listening_ports.png)

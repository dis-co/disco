# Instructions to run the test package

- Run `createproject.cmd [IP_ADDRESS]` to create a project, replacing `[IP_ADDRESS]` with your machine IP address (e.g. `192.168.8.11`). The project will be named "iris_sample" and will be placed in the default project folder (C:\Iris). You only need to do this step once.

- Now run `runiris.cmd` to start the iris service loading the project just created. Open `http://localhost:7000/` in your browser to interact with Iris frontend.

> **ATTENTION**: To stop the iris service you must use Ctrl+C, don't close the command window. This is because Iris needs to kill the git daemon. If it's not killed the port will be occupied and you won't be able to run iris again.

- In the same directory but in a different command window, run `mockclient.cmd [IP_ADDRESS]` also replacing `[IP_ADDRESS]` with your machine IP address. This will start a REPL acting as an Iris mock client. Type `help` in the REPL to see the available commands. 
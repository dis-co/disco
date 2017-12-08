# Instructions to run the test package

- Run `createproject.cmd [IP_ADDRESS]` to create a project, replacing `[IP_ADDRESS]` with your machine IP address (e.g. `192.168.8.11`). The project will be named "disco_sample" and will be placed in the default project folder (C:\Disco). You only need to do this step once.

- Now run `rundisco.cmd [IP_ADDRESS]` to start the disco service loading the project just created. Open `http://[IP_ADDRESS]:7000/` in your browser to interact with Disco frontend.

> **ATTENTION**: To stop the disco service you must use Ctrl+C, don't close the command window. This is because Disco needs to kill the git daemon. If it's not killed the port will be occupied and you won't be able to run disco again.

- In the same directory but in a different command window, run `mockclient.cmd [IP_ADDRESS]` also replacing `[IP_ADDRESS]` with your machine IP address. This will start a REPL acting as an Disco mock client. Type `help` in the REPL to see the available commands.

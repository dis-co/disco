###ide installation instruction 
####for mac 

- install [visual studio code] (https://code.visualstudio.com/Download)

- open visual studio code and under add-ons search for „ionide“. install all 3 packages

[install ionide] (img/install_ionide.png)

- install [fsharp] (http://fsharp.org/use/mac/) using option #5:
  
  - install [homebrew] (https://brew.sh/index_de.html) by pasting the following command at a terminal prompt: 
**/usr/bin/ruby -e "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/	install)“**
  
  - the script explains what it will do and then pauses before it does it. there are more installation options.

  - after installation run: **brew install mono** in the terminal to install mono and f#

- install node.js:	**brew install node.js**
- install npm:		**brew install npm**
- install git:		**brew install git**
- install flatbuffers:	**brew install flatbuffers**

- download and install [.NET Core SDK] (https://www.microsoft.com/net/core#macos)

####for windows

- install [chocolatey] (https://chocolatey.org/install)

- via chocolatey install:
 
  - visual studio code: **choco install visualstudiocode**
  - microsoft build tools: **choco install microsoft-build-tools**
  - windows sdk: **choco install windows-sdk-8.0 -y**
  - visual fsharp tools: **choco install visualfsharptools -y**
  - node.js: **choco install nodejs**
  - git: **choco install git**
  - dotnet: **choco install dotnetcore-sdk**

- download and install [.NET Core 2.0] (https://www.microsoft.com/net/download/core)
- install „build tools for visual studio 2017“ [from here] ( https://www.visualstudio.com/de/downloads/)

[build tools] (img/build_tools.png)

- pick the following selection (summary)

[checkbox summary] (img/summary.png)
[![AppVeyor](https://ci.appveyor.com/api/projects/status/u9l11pqpdx2u0pnq?svg=true)](https://ci.appveyor.com/project/disco/disco)
[![Travis](https://travis-ci.org/dis-co/disco.svg?branch=master)](https://travis-ci.org/dis-co/disco)


# DISCO
**Distributed Show Control**  

DISCO is a next generation live performance framework to interconnect and control software systems. DISCO is designed to meet the rapidly evolving needs of live media productions with a focus on realtime content creation. With its roots as a vj-software it is designed to provide the most versatile way to control and operate even complex techincal setups.

## Download

You can [download](https://ci.appveyor.com/project/disco/disco/build/artifacts) current builds from AppVeyor. Note: these builds are from HEAD of master branch.

## Installation (Windows)

### Prerequisites

* GIT
* Download and install [Bonjour](http://support.apple.com/downloads/DL999/de_DE/BonjourPSSetup.exe) (optional)
* Download and extract the latest build from the Github Repository [https://github.com/dis-co/disco](https://github.com/dis-co/disco)

### initial installation sequence 

* create empty workspace folder at "c:\disco"
* run disco\disco.exe setup
* select network adapter (make sure that it is set to fixed ip adress)
* enter multicast group (press *enter* for default)
* enter machine name (press *enter* for default)
* enter path to DISCO Asset folder (if your workspace is located at "c:\disco" you could just press *enter* for default)
* enter path to DISCO log folder (if your workspace is located at "c:\disco" you could just press *enter* for default)
* enter path to DISCO workspace folder (the fault location is "c:\disco" press *enter* for default)
* check setting and confirm settings

you only need to do this once on intitial installation or if your ip has changed! 

### start DISCO

First we have to make sure that at an instance of the DISCO DSM Service is running.

* run disco\disco.exe start
* open a browser and point to YOUR-IP:7000 (localhost wont work here!)

## Documentation

Please find the ongoing Documentation [here](https://www.gitbook.com/book/dis-co/disco/details)

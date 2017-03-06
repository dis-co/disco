[![Build status](https://ci.appveyor.com/api/projects/status/1ouixkub5yqwb1b5/branch/master?svg=true)](https://ci.appveyor.com/project/NSYNK/iris/branch/master)

[Download Latest Release](https://ci.appveyor.com/api/projects/nsynk/iris/artifacts/Iris-latest.zip)

[Download Release Checksums](https://ci.appveyor.com/api/projects/nsynk/iris/artifacts/Iris.sha256sum)

# Iris

## Data Definition

### RAFT

### IRIS Service


### Client-API
The Iris Client-API ensures the communication between the Iris-Service and a Iris-Client.

#### Functions

#### Value-Types
This is the value without visual representation.
##### Value


#### Subtypes
The Subtypes which were previuosly set by the client are now set by the iris-front end and will stored inside the project.

##### Value
##### String
##### Enum
##### Color
##### Raw


### IRIS Project

| Name | Type  | mandatory | description
| :------- | :------: |  :------: | -------: |
| name | string | yes |
| id | uint32 | yes | guid

#### Cuelist

| Name | Type  | mandatory | description
| :------- | :------: |  :------: | -------: |
| name | string | yes |
| id | uint32 | yes | guid
| cues| [cue] | yes | array of cues

#### Cue

| Name | Type  | mandatory | description
| :------- | :------: |  :------: | -------: |
| name | string | yes |
| id | uint32 | yes | guid
| parameters | [parameter ] | no| array of parameters

#### Parameter

A parameter defines a single exposed value of a client

| Name | Type  | mandatory | description
| :------- | :------: |  :------: | -------: |
| name | string | yes |
| id | uint32 | yes | guid
| adress | uint32 | no | id of client parameter
| Datatype |||

#### Host-Group

| Name | Type  | mandatory | description
| :------- | :------: |  :------: | -------: |
| name | string | yes |
| id | uint32 | yes | guid
|host | [uint32]| no| array of host-id´s

#### Users

| Name | Type  | mandatory | description
| :------- | :------: |  :------: | -------: |
| name | string | yes |
| id | uint32 | yes | guid
| groups | [uint32] | no | array of group ids the user is assigned to

#### User-Group

| Name | Type  | mandatory | description
| :------- | :------: |  :------: | -------: |
| name | string | yes |
| id | uint32 | yes | guid

# Building

## Windows

A setup without `VisualStudio` is best bootstrapped with [chocolatey](https://chocolatey.org). The following packages are required to build Iris:

```
7zip.commandline
chocolatey 
cmake.install 
DotNet4.0 
DotNet4.5 
DotNet4.5.2 
git.install 
microsoft-build-tools 
nodejs.install 
vcredist2015 
VisualCppBuildTools 
visualfsharptools
Wget 
windows-sdk-8.0
```
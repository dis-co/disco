[![Build status](https://ci.appveyor.com/api/projects/status/1ouixkub5yqwb1b5/branch/master?svg=true)](https://ci.appveyor.com/project/NSYNK/iris/branch/master)

[Download Latest Release](https://ci.appveyor.com/api/projects/nsynk/iris/artifacts/Iris-latest.zip)

[Download Release Checksums](https://ci.appveyor.com/api/projects/nsynk/iris/artifacts/Iris.sha256sum)

# Iris

## Data Definition

     ____  _ 
    |  _ \(_)_ __ 
    | |_) | | '_ \ 
    |  __/| | | | |
    |_|   |_|_| |_|

### VecSize

`VecSize` is either **_dynamic_**, i.e. user can add/update/delete
values to a `Pin` in any way (with the exception that there **has** to
be at least one value per `Pin`), or it is **_fixed_** by a number of
values a `Pin` always **needs** to have at the very least. On a
**_fixed_** `Pin`, values always need to be a multiple of the provided
value. The corresponding `FSharp` type is:

```
type VecSize =
  | Dynamic
  | Fixed of uint32
```

### Number 

Fields:

| Name      |       Type                                   |  Description                      |
|----------:|:--------------------------------------------:|----------------------------------:|
| Id        | string                                       | per-client unique identifier      |
| Name      | string                                       | descriptive name                  |
| Tags      | string array                                 | metadata for grouping and search  |
| Unit      | string                                       | unit of measure                   |
| VecSize   | **_dynamic_** or **_fixed_** with int count  | slice behavior                    |
| Precision | integer                                      | number of post-comma positions    |
| Min       | integer                                      | minimum allowed value             |
| Max       | integer                                      | maximum allowed value             |
| Values    | array                                        | actual number values              |

----

### String

Behavior:

String `Pin` behavior is either one of the following values to
indicate validation requirements to the front-end:

- `Simple`
- `MultiLine`
- `FileName`
- `Directory`
- `Url`
- `IP`

The corresponding `FSharp` discriminated union:

``` 
type StringBehavior =
  | Simple
  | MultiLine
  | FileName
  | Directory
  | Url
  | IP
``` 

Fields:


| Name      |       Type                                   |  Description                      |
|----------:|:--------------------------------------------:|----------------------------------:|
| Id        | string                                       | per-client unique identifier      |
| Name      | string                                       | descriptive name                  |
| Tags      | string array                                 | metadata for grouping and search  |
| Behavior  | Behavior                                     | indicate validation constraints   |
| MaxChars  | integer                                      | maximum number of allowed chars   |
| VecSize   | **_dynamic_** or **_fixed_** with int count  | slice behavior                    |
| Values    | string array                                 | actual values of this pin         |

### Bool

Fields:

| Name      |       Type                                   |  Description                      |
|----------:|:--------------------------------------------:|----------------------------------:|
| Id        | string                                       | per-client unique identifier      |
| Name      | string                                       | descriptive name                  |
| Tags      | string array                                 | metadata for grouping and search  |
| IsTrigger | boolean                                      | should the value be reset         |
| VecSize   | **_dynamic_** or **_fixed_** with int count  | slice behavior                    |
| Values    | string array                                 | actual values of this pin         |

### Byte

Fields:

| Name      |       Type                                   |  Description                      |
|----------:|:--------------------------------------------:|----------------------------------:|
| Id        | string                                       | per-client unique identifier      |
| Name      | string                                       | descriptive name                  |
| Tags      | string array                                 | metadata for grouping and search  |
| VecSize   | **_dynamic_** or **_fixed_** with int count  | slice behavior                    |
| Values    | byte array array                             | actual values of this pin         |

### Enum

Fields:

| Name      |       Type                                   |  Description                      |
|----------:|:--------------------------------------------:|----------------------------------:|
| Id        | string                                       | per-client unique identifier      |
| Name      | string                                       | descriptive name                  |
| Tags      | string array                                 | metadata for grouping and search  |
| VecSize   | **_dynamic_** or **_fixed_** with int count  | slice behavior                    |
| Properties| key/value pairs (string * string)            | properties                        |
| Values    | byte array array                             | actual values of this pin         |

### Color

Fields:

| Name      |       Type                                   |  Description                      |
|----------:|:--------------------------------------------:|----------------------------------:|
| Id        | string                                       | per-client unique identifier      |
| Name      | string                                       | descriptive name                  |
| Tags      | string array                                 | metadata for grouping and search  |
| VecSize   | **_dynamic_** or **_fixed_** with int count  | slice behavior                    |
| Values    | byte array array                             | actual values of this pin         |

----

     ____        __ _ 
    |  _ \ __ _ / _| |_ 
    | |_) / _` | |_| __| 
    |  _ < (_| |  _| |_ 
    |_| \_\__,_|_|  \__| 


### IRIS Service

### Client-API
The Iris Client-API ensures the communication between the Iris-Service and a Iris-Client.


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

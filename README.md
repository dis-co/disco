[![Build status](https://ci.appveyor.com/api/projects/status/1ouixkub5yqwb1b5?svg=true)](https://ci.appveyor.com/project/NSYNK/iris)

# Iris

## Data Definition

### RAFT

### IRIS Service


### IRIS Client


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
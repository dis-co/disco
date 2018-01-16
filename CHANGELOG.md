## New in 0.1.3 (16/01/2018)

* fixed hamburger menu
* added automatic setup mode command line flag for creating Docker containers
* updated Dockerfile and added targets to build.fsx
* changed default multicast address to be outside of reserved address space
* fixed port ordering in cluster widget
* added infrastructure to export system metrics to influxdb/grafana
* fixed cueplayer next/previous issue
* added a licence (GPL v3.0)
* added IActor interface layer
* added simple thread-based actor abstraction

## New in 0.1.2 (05/01/2018)

* fix vulnerable dependency in docs target
* number pin min/max constraint 
* reset trigger bool pins after 10ms in service
* fixed double click string editing (fixes #50)
* filter VVVV core dlls during copying to zipped dir
* vvvv client modules added
* change VVVV client folder structure in release (fixes #44)

## New in 0.1.1 (20/12/2017)

* properly package vvvv client nodes (PR #40)
* improve daemon error message formatting on command line (PR #41)
* prevent message loops when publishing on pub/sub API (PR #42)
* add tooltips to frontend (PR #36)

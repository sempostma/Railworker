Railworker
===

A program/library for Train Simulator (RailWorks)

This project is not being actively worked on, it will probably never reach production and should be for personal use only. Feel free to copy any code from my project to your own project.

Features:
- Custom serz.exe implementation in C# (extremely fast)
- Can create GeoJSON maps from TS routes
- Preload creator, can create preloads from scenario consists (Go to the scenario overview, click on a consist, go to "Edit", and Add as Preload should appear in the menu)
- RailDriver implementation (For getting and setting control values externally)
- UIC wagon number generator with valid checksum
- Repaint updater, can update repaints after the main developer updated the .bin files (it patches the old repaint .bin files to the new .bin file) (works well for the RWAustria Taurus)

The Build<xyz> projects are projects containing build scripts for programatically generating repaints

The RailworkerMegaFreightPack1 project/folder also includes various containers, 45ft, 40ft and 20ft containers. Including blueprint XML and blender files. You may use these examples freely. There is also an Sggmrss folder under "Source". This includes only a single textured bogie.

License:

Public Domain


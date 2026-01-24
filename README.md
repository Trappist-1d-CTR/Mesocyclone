# Mesocyclone
Mesocyclone is a planned 3D game that combines Climate and Weather Simulations with Flight Physics and Exoplanetary Models.
I'm currently developing it solo, system by system, through the months. My goal is to hopefully create a good demo to present to the outside world and prove its potential.

But I don't want to focus nearly much on the gameplay or hook aspect.

As I develop the exoplanet, Gale, its environment, with sheer passive hostility and alien conditions, I want to see what happens when multiple deterministic systems collide. What happens when chaos takes place and the most miniscule of variations lead to wildly different outcomes and non-scripted phenomena arise.

## Development
You can take a look at the game's development through my TigSource topic: https://forums.tigsource.com/index.php?topic=76539.0
It contains lots of physics, explanations, and nerdy af jokes (all of which will certainly contain some mistakes in grammar/spelling/reasoning/factuality). Enjoy! :D

## The Code
The codeitself is full of third-party libraries, both from Unity and ones I intalled (all of which, to my knowledge, are free to use and distribute; but if I'm wrong, please tell me so).
You can find the ones I coded myself, though, by peeking into the Assets Folder and locating all the ones marked with the following [Dev#] (# being a number) and then opening the folder named "Script(s)".

Everything was made in Unity, Blender, Visual Studio, and C#

- [Dev1] was my attempt, as one of my school teachers suggested, to analyze how gas particles would behave as singular particles rather than how they're actually dealt with in the actual project. Useful for physics demonstration or if you need a foundation to base yourself on.
- [Dev2] was my initial attempt at simulating gas physics, with fixed grids made out of hexagons which would exchange particles, heat, etc. Because of performance issues, however, this idea was discarded. I don't remember too much about it, so feel free to explore it yourself.
- [Dev3] is my current system for simulating gas physics, with dynamic Air Cells treated as singular non-rigid bodies (cylinder shaped). The specific physics behind their functioning include gravity, buoyancy, Cell collisions and repulsion, PV = nRT (it's freaking EVERYWHERE), static pressure, insolation (da Sun), diffusion of heat, adiabatic temperature changes, and some more I can't remember.
- [Dev4] is the fight engine and simulation. It also contains my main drone's data (which is separated into multiple parts, some of which can behave like control surfaces). The engines works well, is quite realistic (you could even pull off a helicopter move if you spun the drone just right), and has a very good performance (1000 fps on my high-mid performance Laptop). Though I still am to enable the realistic torque section as I'm struggling with some more issues. For now, there's fixed Torque as a temporary (for real this time) substitute.
- [Dev5] is the environment: the terrain. Not much more to it, but it's useful to get some points of reference of the drone's speed and controls.
- [Dev6] is the UI. The data the player needs in order to fly the drone in a way to avoid experiencing unscheduled, very rapid disassembly.
- [Dev7] is an experimental struct to store values in a more reliable way that bypasses the isses of floating point error (not actually implemented in the build, I plan to make its own public repository in the future).
- [Dev0] is not my code. It's a code I got from someone else to get fullscreen during testing without having to make 1000 builds.

## Gale - The Exoplanet
More information on it here (props to Artifexian for the original WorldSmith template which I slightly modified for my own needs):
https://docs.google.com/spreadsheets/d/1iQJklqcxE3pQBaImazHuIJsGAS6dR0NKBTK6BrtrpfA/edit?usp=sharing

## Questions
If you got any questions regarding the code (I make little to no comments-) and/or project feel free to ask.

# Installation
## Project
Major Software Requirements:
- Unity Hub
- Unity
- Code editor (Visual Studio, etc.)
Steps:
- Download the repository: Go to Mesocyclone's main page, Click on the green "Code" button, Select the option to download zip, Extract the zip file to wherever you want (but keep in mind where).
- Open with Unity Hub: Open Unity Hub, Click on "Add" and select "Add from disk", Click on the project when it appears.
- Run the project: Standard scene should appear, Switch between scenes located at [Dev3] and [Dev5] ([Dev1] and [Dev2] for older, unused stuff), Click on Play button on the viewport.
Playing:
Once you click on the Play button on the scene either in the [Dev3] or [Dev5] folders, you'll immediately become the drone. Press Ctrl + G to toggle fullscreen. You can click on the GameObject FlightDrone in the left side of the screen and then look right at the Inspector. Scrolling down, the very last two values are development functionalities:
- VisualizationMode can be set to any number from 0 to 3. At 0 nothing's done, at 1 there will be drawn vectors showing the forces at play (lift, gravity, thrust, drag), at 2 the forces of each wing part (lift and induced drag) will be shown, at 3 torque forces from each wing part (represented as force vectors instead of torques: as the forces that push up or down certain parts of the plane) will be shown.
- AirChamberTest can be set to false (unchecked) or true (checked). When true, the drone will not be able to move but will rotate freely and winds from Air Cells will be ignored. Instead, you can modify that value manually above by changing the "Wind" Vector3, as if the drone were in a controlled wind chamber. To simulate the drone flying straight ahead, a negative X value must be put. Flying straight backwards requires a positive X value. Flying upwards requires negative Y value, while for downwards a positive Y value. Flying rightwards requires a negative Z value, while for leftwards a negative Z value. You can combine these to get something like flying ahead but skewed to the left and downwards a bit: negative X, positive Y, positive Z.
## Build
Flight Drone Test: On the right side of the repository's home page, click on "1 tags" within "Releases" to get to the build. Download the zip and extract it, you'll find installation instructions within it.

# Copyright
Technically: Look up MIT copyright license.
Practically: Feel free to browse around the project in general and to download and/or copy any of the files, both in the project's entirety and in parts of it, as long as it's done either non-commercially or (in the case of a YouTube video) within the criteria of fair use. Moreover, while you can take parts of it (code snippets/files and unity scenes) and implement them into your own projects (with or without attribution), you cannot take the whole project and use it commercially (like selling). And whatever you end up doing with it, don't pretend to be the one to have made what you took from someone else (in general, not just regarding this project).

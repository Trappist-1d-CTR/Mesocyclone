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
You can find the ones I coded myself, though, by peeking into the Assets Folder and locating all the ones marked with the following [Dev#] (# being a number) and then opening the folder named "Scripts".

Everything was made in Unity, Visual Studio, and C#
The project is separated into two branches (main and UI) because I never worked with them and mean to learn how they work. Here's all the code in relation to their [Dev#] marking:

- [Dev1] was my attempt, as one of my school teachers suggested, to analyze how gas particles would behave as singular particles rather than how they're actually dealt with in the actual project. Useful for physics demonstration or if you need a foundation to base yourself on.
- [Dev2] was my initial attempt at simulating gas physics, with fixed grids made out of hexagons which would exchange particles, heat, etc. Because of performance issues, however, this idea was discarded. I don't remember too much about it, so feel free to explore it yourself.
- [Dev3] is my current system for simulating gas physics, with dynamic Air Cells treated as singular non-rigid bodies (cylinder shaped). The specific physics behind their functioning include gravity, buoyancy, Cell collisions and repulsion, PV = nRT (it's freaking EVERYWHERE), static pressure, insolation (da Sun), diffusion of heat, adiabatic temperature changes, and some more I can't remember.
- [Dev4] is the fight engine and simulation. It also contains my main drone's data (which is separated into multiple parts, some of which can behave like control surfaces. The engines works well, is quite realistic (you could even pull off a helicopter move if you spun the drone just right), and has a very good performance (1000 fps on my high-mid performance Laptop). Though I still am to enable the realistic torque section as I'm struggling with some more issues. For now, there's fixed Torque as a temporary (for real this time) substitute.
- [Dev5] is the environment: the terrain. Not much more to it, but it's useful to get some points of reference of the drone's speed and controls.
- (UI branch) [Dev6] is the UI. The data the player needs in order to fly the drone in a way to avoid experiencing unscheduled, very rapid disassembly.
- (UI branch) [Dev0] is not my code. It's a code I got from someone else to get fullscreen during testing without having to make 1000 builds.

## Gale - The Exoplanet
More information on it here (props to Artifexian for the original WorldSmith template which I slightly modified for my own needs):
https://docs.google.com/spreadsheets/d/1iQJklqcxE3pQBaImazHuIJsGAS6dR0NKBTK6BrtrpfA/edit?usp=sharing

## Questions
If you got any questions regarding the code (I make little to no comments-) and/or project feel free to ask.

# Copyright
Feel free to download and/or copy snippets of code and browse around the project in general. Do not, however, think about taking it (all or even chunks of it) and saying it is your own work and/or idea. You can use my code files for your own projects, of course (as I had to re-think the coding behind several systems like 3D data interpolation, Air Cell physics, flight physics, etc.) but always make sure to attribute me (name of project, link of project, and username will be enough) and actually expand on it with stuff of your own creation.

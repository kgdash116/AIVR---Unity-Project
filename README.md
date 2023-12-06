# AIVR---Unity-Project
An open-source Unity based repository for the AIVR project . This repository serves as the Unity module that connects with the AIVR open-source Python Package.It has the essenttial scripts that creates Socket based connections with the AIVR Python package, as well as a contoller module that allows the user to shift control of the Unity environment to the Python environment. It processes commands recieved from the AIVR package and renders out results as Game objects on the Unity Game scene.


## Table of Contents

- [Team](#team)
- [Package](#package)
- [Installation](#installation)
- [Functionality](#functionality)


## Team
Meet the team mebers involved in conceptualising and fabricating the AIVR open-source package
Name | Photo | Role | Email
---- | ----- | ---- | ----
Harry Li | <img src="https://user-images.githubusercontent.com/38079632/227462713-9f9a5f60-e869-4c92-a653-98c1e6af724f.jpg" width="100" height="100"> | Project Supervisor & Architect | hua.li@sjsu.edu
Yusuke Yakuwa | <img src="https://user-images.githubusercontent.com/38079632/227462162-c2182a3b-e310-4b65-8d48-9ce06d7f87dd.jpg" width="100" height="100"> | Industrial Advisor | yusuke.yakuwa@ctione.com
Waqas Kureshy | <img src="https://github.com/kgdash116.png" width="100" height="100">| Lead Developer | waqas.kureshy@sjsu.edu
Prabjyot Obhi |  | Team Member | 

## Package
The AIVR Unity project is made up of only the essential components required by the User to establish communication with the AIVR Python package. It features a `UnityController` class and a `UnityMainThreadDispatcher` class. New methods can be adopted in, and the project existing resources can easily be added to other existing projects to make use of the AIVR Python Package. Present below is a highlevel architecture of the Unity AIVR project, which lists its building blocks. 



## Installation

- Download the Unity code.
- Open Unity Hub, click on the `Add` button to add the project.
- Locate the folder for the project and click on open.
- Let the Assets load and the C# scripts compile.
- Press Play.


## Functionality

- The user can create all sorts of custome 3-D game objects like Cubes,Cylinders,Capsules,Spheres and Planes.
- The attributes for the objects and commands for rendering are passed from the AIVR package.
- This Unity module has the ability to recieve and render Camera output.
- The user can also create and render video objects received from the AIVR Package. 

# AUV Simulation 2.0
Okoń AUV simulation 2.0 built in [Unity](https://unity.com/) 2020.1.3f1. Simulates behaviour of the Okon. It's an AUV built by [KNR](http://knr.meil.pw.edu.pl/). Simulation includes:
- underwater shaders
    - color correction
    - physical camera setup
    - motion blur
- simple fluid dynamics
    - laminar and dynamic flow
    - buoyancy
	
## Table of contents

- [Usage](https://github.com/knr-auv/simulation/wiki/Usage)
- [Networking](https://github.com/knr-auv/simulation/wiki/Networking)
- [Simulation](#simulation)
	- [Physics](#physics)
		- [Thrusters](#thruster)
		- [Fluid dynamics](#fluid-dynamics)
    - [Graphics](#graphics)
    	- [Shaders](#shaders)
    	- [Camera](#camera)
    	- [Streaming](#Streaming)

---

## Simulation

Simulation uses Unity physics engine with a script that implements fluid dynamics. Also 3d model of the robot is added for correct collisions with objects.

### Physics

Physics in Unity by default is accurate enough for games. For this simulation accuracy was improved by decreasing physics time step.

#### Thrusters

Thrusters in simulation have approximated thrust based on [producer charts](https://bluerobotics.com/store/thrusters/t100-t200-thrusters/t200-thruster/). Thrust is approximated with this equation  
`ax^3 + bx^2 + cx^2 + d`  
where parameters are the solution of  
```
a*1100^3 + b*1100^2 + c*1100 + d = 4.5
a*1472^3 + b*1472^2 + c*1472 + d = 0
a*1528^3 + b*1528^2 + c*1528 + d = 0
a*1852^3 + b*1852^2 + c*1852 + d = 5.02

a = 1.66352902e-8f
b = -0.00003994119f
c = 0.00752234546f
d = 22.4126993334f
```

The final function to set the force of the thruster:   
```cs
float f(float fill) {
	float x = Map(fill, -1f, 1f, 1100f, 1900f);
	if(fill > 0) return Mathf.Max(0f, a*x*x*x + b*x*x + c*x + d);
	else return -Mathf.Max(0f, a*x*x*x + b*x*x + c*x + d);
}
```

#### Fluid dynamics

Every object that will be dynamic and is in the water has defined *mass*, *volume*, *fluid dynamics constants*, *center of mass* and *center of volume*. Those variables are needed to calculate correct dynamics in the water.


### Graphics

#### Shaders

Shaders are used to give underwater effect to image. This helps testing and training YOLO. It is neural net that allows Okoń to see object in front of it. The most important ones are color correction that reduces reds in the image and fog which is responsible for limiting distance of seeing.

#### Camera

Camera settings in Unity are based on real values of the camera like sensor size and field of view.

#### Streaming

Camera renders to a Render texture, which is than copied to Texture2D and encoded to a JPG bytes.

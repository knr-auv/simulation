# AUV Simulation
Okoń AUV simulation 2.0 built in [Unity](https://unity.com/) 2020.1.3f1. Simulates behaviour of the Okon. It's an AUV built by [KNR](http://knr.meil.pw.edu.pl/). Simulation includes:
- underwater shaders
    - color correction
    - physical camera setup
    - motion blur
- simple fluid dynamics
    - laminar and dynamic flow
    - buoyancy
    
	**WARNING! Docs are outdated!**
	
## Table of contents

- [Usage](#usage)
	- [Startup](#startup)
	- [Settings](#settings-file)
	- [Important notes](#important-notes)
- [Networking](#networking)
	- [Packets structure](#packets-structure)
	- [Video stream](#video-stream)
	- [Simulation control](#simulation-control)
- [Simulation](#simulation)
	- [Physics](#physics)
		- [Thrusters](#thruster)
		- [Fluid dynamics](#fluid-dynamics)
    - [Graphics](#graphics)
    	- [Shaders](#shaders)
    	- [Camera](#camera)
    	- [Streaming](#Streaming)

---

## Usage

### Startup

After starting the built project, small black window of Unity application should appear. Simulation is now running and a client can connect. 

### Settings file

During startup, simulation checks for `settings.json` file where it reads port numbers. If not found it uses default values (see [networking](#networking)). *Quality* defines JPG compression level (from `0` to `100` for least image compression)  
JSON structure:
- videoPort
- jsonPort
- quality

Example  
```json
{
"videoPort": 44208,
"jsonPort": 44211,
"quality": 66
}
```

### Important notes

- JSON must use `"` character, not `'`.
- Video request packet `0x69` is only 1 byte long
- Data length is in little endian

## Networking

Client interact with simulation via 2 different channels for video and control.
- video default port is `44209`
- control default port is `44210` 

### Packets structure

All packets are `TCP/IP`. Data length is equal to 0 when packet doesn't have data. Data length is in little endian.  
**IMPORTANT Video request packet is only 1 byte long, it is just packet type byte!**

| Packet type | Data length as int32 | Data |
| ----------- | -------------------- | ---- | 
| 1 byte | 4 bytes | n bytes |

### Video stream

Client send request packet and server responds with JPG encoded video frame. Size of the image is in range of `15...650KB`.

| Byte | Abbreviation | Description |
| ---- | ------------ | ----------- |
| `0x69` | GET_VID | Request video frame |

### Simulation control

This channel is responsible for controlling simulation: steering the robot, recording data etc. Not all packets include data so see [packets structure](#packets-structure). All data is in JSON format. All packets types are listed in the table below.

Packet type bytes are groped by activity they are connected with:
- `0x00` to `0x9F` none 
- `0xA0` to `0xAF` **motors**
- `0xB0` to `0xBF` **sensors**
- `0xC0` to `0xCF` **simulation**
- `0xD0` to `0xDF` **data** (e.g. recording robot position)
- `0xE0` to `0xEF` none
- `0xF0` to `0xFF` none

| Byte | Abbreviation | Description | JSON from client | JSON from server | Working |
| ---- | ------------ | ------------| ---------------- | ---------------- | ------- | 
| `0xA0` | [SET_MTR](#set_mtr) | Set motor power | ```{FL, FR, ML, MR, B}``` |  | Yes
| `0xA1` | [ARM_MTR](#ARM_MTR) | Arm motors |  |  | 
| `0xB0` | [GET_SENS](#get_sens)	| Get sensors read outs	|  | `{accel, gyro, baro}` | Yes
| `0xB1` | [GET_DEPTH](#GET_DEPTH) | Requests depth texture |	 | `{depth}` | Yes
| `0xC0` | [SET_SIM](#set_sim) | Set simulation options | `{quality}` |	 |
| `0xC1` | [ACK](#ack) | Acknowledgement |  |  | Yes
| `0xC2` | [GET_ORIEN](#get_orien) | Get Okon’s orientation |  | `{pos, rot}` | Yes
| `0xC3` | [SET_ORIEN](#set_orien) | Set Okon’s orientation |	`{pos, rot}` |  | Yes
| `0xD0` | [REC_STRT](#REC_STRT) | Start recording pos. and dir. | | *Verification?*
| `0xD1` | [REC_ST](#REC_ST) | Stop recording | | |
| `0xD2` | [REC_RST](#_RST) | Reset and clear recording | | |
| `0xD3` | [GET_REC](#GET_REC) | Get recorded data | | *Array of pos and dir*
| `0xC5` | [PING](#ping) | Send ping to server |	`{timestamp, ping}` | `{timestamp, ping}` | Yes
| `0xC4` | [KILL](#KILL) | Kill the simulation |	| |

---

#### SET_MTR

Packet sets thrusters power and direction. `FL, FR, ML, MR, B` are abbreviations e.g. `FL` `FrontLeft` thruster. Values are in range from `-1.0f` to `1.0f`.  
JSON structure:  
- FL
- FR
- ML
- MR
- B

Example request`{"FL":0.1,"FR":-1.0,"ML":0.1,"MR":0.0,"B":0.008}`

#### GET_SENS

Packet requests current values of on-board sensors (*gyroscope*, *accelerometer* and *barometer*). Values `x, y, z` for *gyroscope* are in degrees.  
JSON structure:
- gyro
    - x
    - y
    - z
- rot_speed
    - x
    - y
    - z
- accel
    - x
    - y
    - z
- angular_accel
    - x
    - y
    - z
- baro 
  - pressure  

Example response `{"gyro":{"x":-9.81e-16,"y":180.0,"z":5.269e-7},"rot_speed":{"x":1.2e-9,"y":1.41e-11,"z":2.282e-11},"accel":{"x":4.0e-16,"y":0.084,"z":-5.37e-17},"angular_accel":{"x":6.2e-11,"y":7.024e-13,"z":1.13e-12},"baro":{"pressure":11115.315}}`

#### GET_DEPTH

Packet requests depth map.
JSON structure:
- depth

Example response `{"depth":"BASE64_DEPTH_IMAGE_STRING"}`

#### GET_ORIEN

Packet requests current orientation of the robot (*rotation* and *position*). *Position* values are in simulation's world coordinates.
JSON structure:
- rot
    - x
    - y
    - z
- pos
    - x
    - y
    - z

Example response `{"rot":{"x":0.0,"y":0.0,"z":0.0},"pos":{"x":-1.2400000095367432,"y":-0.19980505108833314,"z":-2.7100000381469728}}`

#### SET_ORIEN

Packet allows to set *position* and *rotation* of the robot in simulation's world space. *Rotation* values are in degrees.  
JSON structure:  
- rot
    - x
    - y
    - z
- pos
    - x
    - y
    - z

Example request `{"rot":{"x":0.0,"y":0.0,"z":0.0},"pos":{"x":1.0900000095367432,"y":-0.89980505108833314,"z":-2.7100000381469728}}`

#### PING

Packet allow to measure ping between client and simulation. Client sends only own *timestamp* (UNIX time stamp milliseconds) leaving *ping* equal to 0. Server responses with *timestamp* (also UNIX milliseconds) of received packet and value of the ping.  
JSON structure:
- timestamp
- ping

Example response `{"timestamp":637156806029701490,"ping":11}`

#### SET_SIM

This packet allows to set the compression level of the frame. Value of the *quality* is in range from `0` to `100` (see [settings file](#settings-file)).  
JSON structure

- quality	

#### ACK

ACK packet can help debugging networking. They are sent after each request of the client. *State* tells if last request was OK and *fps* tells last frames per second during last frame.    
JSON structure:
- fps
- state

## Simulation

Simulation uses Unity physics engine with a script that implements fluid dynamics. Also 3d model of the robot is added for correct collisions with objects.

### Physics

Physics in Unity by default is accurate enough for games. For this simulation accuracy was improved by decreasing physics time step to `7` milliseconds.

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

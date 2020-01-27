# AUV Simulation
Okoń AUV simulation built in [Unity](https://unity.com/) 2019.2.0f1. Simulates behaviour of the Okon AUV. Project includes:
- underwater shaders
    - color correction
    - physical camera setup
    - motion blur
- simple fluid dynamics
    - laminar and dynamic flow
    - buoyancy
    
## Table of contents

- [Simulation](#simulation)
	- [Fluid dynamics](#fluid-dynamics)
- [Networking](#networking)
	- [Packet structure](#packet-structure)
	- [Video feed](#video-feed)
	- [Simulation control](#simulation-control)
- [Important notes](#important-notes)

---

## Simulation
Simulation was created with Uinty game engine. It simulates behaviour of the Okon AUV.

### Fluid dynamics
Every object that will be dynamic and is in the water has defined *mass*, *volume*, *fluid dynamics constants*, *center of mass* and *center of volume*. Those variables are needed to calculate correct dynamics in the water.

### Thrusters

Thrusters in simulation have approximated thrust based on [producer charts](https://bluerobotics.com/store/thrusters/t100-t200-thrusters/t200-thruster/). Thrust is approximated with this eqation  
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
	if(val > 0) return Mathf.Max(0f, a*x*x*x + b*x*x + c*x + d);
	else return -Mathf.Max(0f, a*x*x*x + b*x*x + c*x + d);
}
```


## Networking
Client interact with simulation via 2 diffrent channels for video and control. 
### Packet structure
All packets are `TCP/IP`. Data length is equal to 0 when packet doesn't have data.

| Packet type | Data length as int32 | Data |
| ----------- | -------------------- | ---- | 
| 1 byte | 4 bytes | n bytes |

### Video feed
Client send request packet and server responds with JPG encoded video frame.

| Byte | Abbreviation | Description |
| ---- | ------------ | ----------- |
| `0x69` | GET_VID | Request video frame |

### Simulation control
This channel is responsible for controling simulation: steering the robot, recording data etc. Not all dataframes include data so see [packets structure](#packet-structure). All data is in JSON format. All packets types are listed in the table below.
##### Control packets table
Packet type bytes are groped by activity they are connected with:
- `0x00` to `0x9F` none 
- `0xA0` to `0xAF` motors
- `0xB0` to `0xBF` sensors
- `0xC0` to `0xCF` simulation
- `0xD0` to `0xDF` data (e.g. recording robot position)
- `0xE0` to `0xEF` none
- `0xF0` to `0xFF` none

| Byte | Abbreviation | Description | JSON from client | JSON from server | Working |
| ---- | ------------ | ------------| ---------------- | ---------------- | ------- | 
| `0xA0` | [SET_MTR](#set_mtr) | Set motor power | ```{FL, FR, ML, MR, B}``` |  | Yes
| `0xA1` | [ARM_MTR](#ARM_MTR) | Arm motors |  |  | 
| `0xB0` | [GET_SENS](#get_sens)	| Get sensors read outs	|  | `{accel, gyro, baro}` | YES
| `0xC0` | [SET_SIM](#SET_SIM) | Set simulation options | `{quality}` |	 |
| `0xC1` | [ACK](#ACK) | Acknowledgement |  |  |
| `0xC2` | [GET_ORIEN](#get_orien) | Get Okon’s orientation |  | `{pos, rot}` | YES
| `0xC3` | [SET_ORIEN](#set_orien) | Set Okon’s orientation |	`{pos, rot}` |  |
| `0xD0` | [REC_STRT](#REC_STRT) | Start recording pos. and dir. | | *Verification?*
| `0xD1` | [REC_ST](#REC_ST) | Stop recording | | |
| `0xD2` | [REC_RST](#_RST) | Reset and clear recording | | |
| `0xD3` | [GET_REC](#GET_REC) | Get recorded data | | *Array of pos and dir*
| `0xC5` | [PING](#ping) | Send ping to server |	`{timestamp, ping}` | `{timestamp, ping}` | YES
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
- accel
  	- x
    - y
    - z
- baro 
  - pressure  

Example response `{"gyro":{"x":0.0,"y":0.0,"z":0.0},"accel":{"x":-1.8893474690019438e-19,"y":0.001282564247958362,"z":1.6839033465654367e-21},"baro":{"pressure":6462.2197265625}}`

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


## Important notes

- JSON request must include **all** values.
- JSON must use `"` character, not `'`.




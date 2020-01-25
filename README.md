# AUV Simulation
AUV robot simulation built in [Unity](https://unity.com/). Project includes:
- underwater shaders
    - color correction
    - physical camera setup
    - motion blur
- simple fluid dynamics
    - laminar and dynamic flow
    - buoyancy

## Networking
Client interact with simulation via 2 diffrent channels for video and control. All packets are `TCP/IP`.
### Packet structure
##### With data
| Packet type | Data length as int32 | Data |
| ----------- | -------------------- | ---- | 
| 1 byte | 4 bytes | n bytes |
##### Without data
| Packet type | 
| ----------- |
| 1 byte |

### Video feed
Client send request packet and server responds with JPG encoded video frame.
| Byte | Abbreviation | Description |
| ---- | ------------ | ---------------|
| `0x69` | [GET_VID](#set_mtr) | Request video frame |

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
| `0xA1` | [ARM_MTR](#ARM_MTR) | Set motor power |  | ```{accel, gyro, baro}``` | 
| `0xB0` | [GET_SENS](#GET_SENS)	| Get sensors read outs	|  | `{accel, gyro, baro}` |
| `0xC0` | [SET_SIM](#SET_SIM) | Set simulation options | `{quality}` |	 |
| `0xC1` | [ACK](#ACK) | Acknowledgement |  |  |
| `0xC2` | [GET_POS](#GET_POS) | Get Okon’s position |  | `{pos, rot}` | 
| `0xC3` | [SET_POS](#SET_POS) | Set Okon’s position |	`{pos, rot}` |  |
| `0xD0` | [REC_STRT](#REC_STRT) | Start recording pos. and dir. | | *Verification?*
| `0xD1` | [REC_ST](#REC_ST) | Stop recording | | |
| `0xD2` | [REC_RST](#_RST) | Reset and clear recording | | |
| `0xD3` | [GET_REC](#GET_REC) | Get recorded data | | *Array of pos and dir*
| `0xC5` | [PING](#PING) | Send ping to server |	`{timestamp, ping}` | `{timestamp, ping}`
| `0xC4` | [KILL](#KILL) | Kill the simulation |	| |





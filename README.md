# AUV Simulation
AUV robot simulation in Unity. Includes:
- underwater shaders
    - color correction
    - phycical camera setup
    - motion blur
- simple fluid dynamics
    - laminar and dynamic flow
    - boyancy force

## Networking
Client interact with simulation via 2 diffrent channels. First channel is for streaming video *(default `44209`)*.
**Video feed**
Client send [request packet](#get_vid) and server responds with JPG encoded video frame.
**Simulation control**
Second channel is for controling the simulation *(default `44210`)*. All data is in JSON. 
| Byte | Abbreviation | Type of packet | JSON from client | JSON from server | Working |
| ---- | ------------ | ---------------| ---------------- | ---------------- | ------- | 
| `0xA0` | [SET_MTR](#set_mtr) | Set motor power | ```{FL, FR, ML, MR, B}``` |  | Yes
| `0xA1` | [ARM_MTR](#ARM_MTR) | Set motor power |  | ```{accel, gyro, baro}``` | 
| `0xB0` | [GET_SENS](#GET_SENS)	| Get sensors read outs	|  | `{accel, Gyro, Baro}` |
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

### Packet structure
#### With data
| Packet type | Data length as int32 | Data|
| ----------- | -------------------- | --- | 
| 1 byte | 4 bytes | n bytes |
#### Without data
| Packet type | 
| ----------- |
| 1 byte |
--
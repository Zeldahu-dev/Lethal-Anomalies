## 0.3.0 Tour Bus Release
- Added Tour Bus
- Added configs for Tourists : Tour Bus spawn threshold, Min group size, Max group size
- Tourists can no longer pathfind under the ship
- Tourists now use collisions instead of range to explode (this should prevent them from exploding when right behind a player)
- Tourists that are being looked at can no longer be pushed by tourists outside view range
- Fixed the tourists' texture having parasite white pixels
- Tourists now have a working map dot

## 0.2.3 Bugfix
- Fixed the issue that would crash the game whenever a client joined
- Fixed the images in the readme not displaying

## 0.2.2 v73 compatibility
- Mod is now compatible with v73 (no longer compatible with v72 and lower) 
- Added alternative behavior for the Spark Tower (enable it in the config)
- Added a small spark targeting players entering the range of the Spark Tower
- Tourists now are attracted to bright items from the following mods : Chillax Scraps, Emergency Dice

## 0.2.1 v70 Maintenance
- Added config to toggle "Punch It" playing when taking the apparatus with FacilityMeltdown installed
- Modified the track to not cut abruptly when the facility explodes, and made it an ogg
- Spark Towers' collisions now disable on ship leaving
- Tourists no longer explode when colliding with entities if they're not being looked at
- Fixed the Spark Towers having bright white parasite pixels on their texture

## 0.2.0 Tourist Release (The "We're So Back" update)
- Added Tourist
- Updated Spark Tower texture
- Unpowered Spark Towers now have collisions
- The Spark Tower can no longer be moved or rotated by other entities
- Spark Towers now require line of sight to smite players, but not to detect them
- With **Facility Meltdown** and **SoundAPI** installed, taking the apparatus now plays the "Punch It" soundtrack. This is not toggleable for now, but a config will be added soon
- Updated terminal nodes
- The antennas on the Spark Towers now seem to be able to receive and transmit an odd signal

## 0.1.5 Improvements
- Delayed the warning particle effect to match the SFX better
- Added slight pitch randomness to the warning SFX
- Lightning visual now emits from the tower, just a visual change

## 0.1.4 Balancing
- Added per-moon spawn weight configuration
- Added general power level configuration
- Added general max count configuration
- Increased the volume of the warning SFX, and modified its falloff curve to be louder at the edge of the detection range
- Added a particle effect that plays alongside the warning SFX to highlight which tower has been triggered
- Slightly increased the base attack threshold

## 0.1.3 Bugfix
- Removed the scan node on the nest prefab to avoid the terminal breaking
- Increased prefab width and added a spawnDenialPoint to prevent rocks from generating over the towers

## 0.1.2 Bugfix
- Fixed the warning audio only playing for the host

## 0.1.1 Bugfix
- Fixed the bug that'd spawn a truck when looking at the terminal node, thanks Xu Xiaolan

## 0.1.0 Spark Tower Release
- Added Spark Tower
# 2.1.0

- fixed being able to add Base component
- added button to "Switch Materials" component to force max compression
- added button to "Switch Materials" component to force 512x512 resolution
- changed "Switch Materials" existing material detection to search for any material with "Quest" in name
- changed "Remove Components" component to only show index if more than 1
- fixed "Remove Blacklisted Components" component not including disabled objects

# 2.0.0

- update to Unity 2022.3.22f1 and VRCSDK 3.7.1 (Oct 2024)
- moved editor window into Tools menu
- moved actions from editor window into components
- added public methods to Questify from other scripts

# 1.6.0

- removed all JSON file operations (please contact me if you actually used this feature)

# 1.5.0

- auto-check for updates
- fixed number of PhysBones incorrectly showing 0
- change PhysBones editor output
- automatically inspect after build avatar

# 1.4.0

- select all materials button
- toggle PhysBones
- dry run cleans up created avatar
- show success message

# 1.3.0

- fixed materials not being packaged
- do not include duplicates in imported asset output and total
- sort imported assets by filesize
- build button
- fix not including hidden objects

# 1.2.0

- output imported assets

# 1.1.0

- remove all PhysBones action

# 1.0.0

- remove PhysBone action
- replace JSON operations with Newtonsoft
- more visible errors
- hide errors on save actions
- added "show" button to paths
- fix font color
- re-order steps

# alpha2

- switch material and remove game object custom actions working
- all material fields are now copied
- disable auto-create materials by default
- checkbox for toon shader
- mark action as perform at end
- remove "copy path" extension
- remove keyboard shortcut

# alpha1

- initial version

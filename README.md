# VRC Questifyer

A Unity plugin that makes it easy to create a Quest version of a VRChat avatar.

Tested with the Airplane Dragon avatar in Unity 2019.4.31f1 with VRC SDK 3.4.1 (VCC).

## Steps

1. Install the package into your project
2. Right click any object in your scene (eg. your "Body") and add the **VRC Questify** component
3. Add actions that will automatically apply when you try and upload your Quest avatar

You can also open the main window by going to PeanutTools -> VRC Questifyer. It gives you an overview of everything in your scene.

### Actions

#### Switch To Material

It overrides all materials of the current renderer (mesh, particle system, etc.) with the provided materials.

#### Remove Game Object

Deletes the object.

#### Remove Component

Deletes the specified components from the game object. Useful for removing VRChat PhysBones, dynamic lights, constraints or other PC-only components.

## Development

## Release

Package PeanutTools/VRC\*Questifyer as a `.unitypackage`. Rename to `peanuttools_vrcquestifyer_$version.unitypackage`

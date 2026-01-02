# rzpAudioRayTracer - A 3D audio ray tracer concept for Unity

This project was developed using Unity version 6000.0.38f1.

## Controls

Use WASDQE keys to fly around.
Hold right mouse button and move the mouse to look around. 

## Audio source

In the Hierarchy window, there's an **AudioSource** gameobject, which defines the source of sound. Move it around to change its location.

The AudioSource gameobject contains a **CSCoreTest.cs** script where you can change your audio source file by specifying the filename in the **Audio Filename** parameter. The audio file must be located in **Assets/StreamingAssets** folder

## Environment

In the Hierarchy window, there's an **Environment** gameobject that holds every normal and diffraction wall of the environment. To add more walls, duplicate and transform existing ones (the same for diffraction volumes).

### Surface materials

Each wall in the Environment gameobject has an **RTBox.cs** script with surface parameters. Either choose a preset from the **Surface Preset** dropdown, or choose the **Custom** preset which will allow changing individual Absorption coefficients.

**Absorption** coefficients 0 to 3 correspond to frequencies 125Hz, 500Hz, 1000Hz and 4000Hz.

**Roughness** coefficient determines how randomly will rays bounce: 0 for perfect specular (use at least 0.01), 1 for perfectly diffuse scattered.

**Is Diffraction Volume** checkbox should stay checked for cylinders only!

## Ray tracing component

In the Hierarchy window, there's an **AudioRayTracer** gameobject, holding the AudioRayTracer.cs script. You can change the following settings:

|  Parameter  |  Description  |
|-------------|---------------|
|  Number Of Rays  |  The amount of rays being shot at each frame from the audio source.  |
|  Max Bounces  |  Max number of bounces for each rays.  |
|  Show Rays  |  If checked, will show rays in real time in the Scene view.  |
|  Ignore IR  |  If checked, will ignore the gathered IR and replace it with an identity IR that doesn't change the original signal.  |

## Demo

https://github.com/user-attachments/assets/e39bc290-ba82-4a1b-90d0-b49246730342

https://github.com/user-attachments/assets/ab0a4ae7-b52d-4d1d-a017-107fd2c125ab



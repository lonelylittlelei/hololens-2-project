# HoloLens 2 AR Visualization of Neurovascular Aneurysm

This project was originally developed by 4th-year Computer Science students Connor Haines, Justin Weller, Finn Ferguson, and XuanRan Qi at Western University under the supervision of Dr. Elvis Chen at Robarts Research Institute in 2022-23.

**Video Demo:** https://www.youtube.com/watch?v=BPGqZ186OqE

**Side by Side Demo:** https://www.youtube.com/watch?v=eiuOeGblh0M

## Project Description
This project aims to improve surgeon and patient communication by using augmented reality (AR) technology to display 3D holograms where in the past 2D images would otherwise be used. The goal is to display a medical model on multiple HoloLens 2 devices at the same time, located in the same place in the local environment, with interactions synced between all devices. This makes it much easier for humans to interpret 3D medical scans.

Microsoft HoloLens 2 is utilized along with supporting software systems such as Unity, MRTK, Frozen World Engine, and David Hocking's PathfinderGUI (currently private). The intention is that raw medical scans (MRI, CT) should first be pre-processed using [this converter script](https://github.com/lonelylittlelei/DICOM-to-STL-OBJ) or through 3D slicer (automated segmentation), which will produce a 3D .obj or .stl model of the medical image, to then be imported into the application from the host server.

PathfinderGUI is used to synchroize interactions with 3D models between devices, as well as any required data sharing, such as video streaming or .stl files.

Frozen World Engine is used for world-locking, and creating a spatial map for each device. 


## Technical Requirements
- Microsoft HoloLens 2 (in developer mode)
- Unity 2020.3.42f1
- Visual Studio 2022
- Mixed Reality Tool Kit (MRTK) 2.8.2
- Secure Network Connection

- Set up & Installation
- Clone the project from the git repo
- Ensure that all software is updated to match the above technical requirements.
- Open the project in Unity (2020.3.4) and perform any required setup
- Ensure that all packages are successfully installed in the Unity Package Manager:
  ![image](https://github.com/lonelylittlelei/hololens-2-project/assets/155585007/e36ed363-d9bb-4ee8-ba0e-b5687ee7f60a)
- Installation and set up of PathfinderGUI required as well
**Note:** The world locking tools package will cause the error "System.IO.DirectoryNotFoundException: Could not find a part of the path ... " this is because the path is too long. It should have not effect but if you are concerned you can read more [here](https://learn.microsoft.com/en-us/mixed-reality/world-locking-tools/documentation/howtos/initialsetup) on how to resolve it.

## Structure
The system uses a combination of World Locking Tools and Pathfinder GUI's Tracked Object class to establish a local coordinate system for each device and synchronized interactions.

### Summary
When Device 1 connects to the host server, any stl files on the desktop of the host are sent to Device 1. The data packages are received by Network Controller and the stl data is converted into a mesh. The tracked object data is shared with the host server. When Device 2 connects to the server, first the tracked object data is shared. Once the appropriate tracked objects are created, the stl files are sent to all clients again. Both devices will convert and load each mesh again, to ensure that both devices are viewing the same object. 

The tracked object component will send updates of the object's position, rotation, and scale to the host, and from the host these updates are sent to all clients so these interactions are synchronized across all devices. Linear interpolation is used to smooth out the trasition between these updates. For example, if Device 1 grabs and moves an object, Device 2 will also see that same movement.

World Locking Tools automatically creates and distributes spatial anchors to create a spatial map of the local environment for each device. Each device can use the voice command "reinitialize" to restart the program and generate new spatial anchors so that the spatial maps on each device are the same or similar enough for models to be generated in the same physical location. 

### In Depth
On the host PC, when a new client connects, the host will first send any tracked objects stored before sending over the mesh data. All data packages sent from the host are received by Network Controller. When the mesh data is sent to the first connected client, the receiver thread in Network Controller will copy and store the bytes from the message that categorize/identify it. In processing the mesh data packet, it will check if there are any objects in the ObjectList and any Tracked Objects if they have the same stream id. If so, it will process the mesh and attach it to the corresponding object with the matching stream id. If it cannot find any object with the same stream id, it will instantiate the meshprototype (a game object with the MeshLoader component that is inactive), add a tracked object component, and then process and attach the mesh to the instantiated meshprototype. Once it has been created, it is then added to the ObjectList. If there is already an object in the ObjectList, the object will be added and the mesh renderer (if there is one) will be turned off for the most recently added object. 

When the second client connects and the tracked object data is sent over, in processing the tracked object data, there will be a check for object with the same GUID or stream id. Since there are no objects created on this client yet, persistence handler will be called to instantiate the meshprototype. The tracked object class is added, but the mesh is not loaded yet. The newly created object is added to the ObjectList on this device. The meshes are sent to each device, and the meshes with the matching stream id will be loaded onto the corresponding object for each device. This is to ensure that if for some reason the meshes were sent in a different order, both clients will be seeing the same thing rendered for object 1 (i.e. instead of one device seeing mesh1 on object1 and the other seeing mesh2 on object1). 

The tracked object component basically keeps track of the transform info of the object, compares it to the update it received and will change the values of the transform information until it reaches the target transform values. If it has not received an update, it will check its own position with the last position it has recorded and send an update to the host if there is a significant enough change. This update is received by the host, updated on the tracked obejct store on the host before being sent out to all clients. 

World Locking Tools will automatically create and spread out spatial anchors as the user moves around their local environment, creating a spatial map for that device. However, these may not necessarily line up for both users and there is no current implementation in this system to share spatial anchors for establishing a common local coordinate system. So instead, users will need to force a restart of the program which clears the stored spatial anchor data, and generate a new spatial map on restart until the spatial maps for both devices line up to an acceptable degree. 

## Build & Deployment
In Unity:

**Note:** Ensure Holographic Remoting has been disabled before attempting to deploy (Edit > Project Settings > OpenXR and under the UWP settings tab disable “Holographic Remoting remote app”. While it is a useful tool, Holographic remoting does not work with ASA.
1) File > Build Settings
2) Ensure the target platform is set to Universal Windows Platform (UWP). The target platform will have the Unity symbol next to it. If not, select UWP and click Switch Platform at the bottom of the window and let it run.
3) Ensure there are no issues with the Project Validation. Go to Edit > Project Settings > XR Plug-in Management > Project Validation and under the UWP settings tab, see if there are any issues of checks. If there are multiple, there should be an option to Fix All.
4) Back in Build Settings, set the following UWPsettings and then click Build:
   - Target Device: HoloLens
   - Architecture: ARM64
   - Build Type: D3D Project
   - Target SDK Version: Latest installed
   - Minimum Platform Version: 10.0.1024.0.0
   - Visual Studio Version: Latest Installed
   - Build and Run on: Local Machine
   - Build Configuration: Release
   - Compression Method: Default
5) Select a Folder to create the build in
6) Open the .sln solution file in Visual Studio

Once in Visual Studio:

7) Plug HoloLens 2 device into PC via USB
8) In the top menu, select Debug or Release (recommended)
9) Select ARM64
10) Select Device

![image](https://github.com/lonelylittlelei/hololens-2-project/assets/155585007/b0930903-d339-45ea-9766-a59918f2e837)

11) Debug tab > Start Without Debugging (with device signed into main menu) or Click Device again.
12) Wait for it to Compile, Build, and Deploy
  - This will deploy the build locally onto the HL2, so it can now be unplugged
**Note:** For debug logs, select Debug and Debug>Start With Debugging. A long USB cable is recommended, as the device must remain plugged in to show the logs.

Steps: https://learn.microsoft.com/en-us/windows/mixed-reality/develop/unity/build-and-deploy-to-hololens

## Running
How to run the app:
1) On the Host PC, start the PathFinderGUI executable
2) Devices 1 and 2 start the app
3) One or both devices reinitialize (voice command) until spatial maps align
4) To cycle between models, use voice commands "Next" and "Back" (each device will have to use the voice command)
5) Use voice command "Read" and "Mute" for the descriptions to be read aloud
6) Enjoy!

## Next Steps 
1) Sharing of spatial anchors
     - can't use azure spatial anchor's because the anchors are stored in the cloud and system cannot have WAN access
     - don't know if it's possible to access the anchors created and placed by world locking tools?
2) Ensure video streaming works
     - dr. hocking's PathfinderGUI was initially designed for video streaming, and all the Unity side scripts he used are included in the project
     - wasn't able to test out/get it to work for me because more recent versions of PathfinderGUI used Nvidia encoders that my laptop wasn't able to use (i think)
3) Synchronize cycling between models
     - currently, if one user views the next model, it will only change for that device. the other device will stay on the original model unless they also use the command
     - this would probably involve adding a boolean to the tracked object updates that tracks if the mesh renderer on the model is active (would have to change this in PathfinderGUI as well)
     - note: if adding voice commands, have to add them to the mixedrealityspeechcommandsprofile (in scene hierarchy, MixedRealityToolkit > Input > Speech > Speech Commands and then you can Add a New Speech Command)
     - also may want to change how descriptions are updated in ObjectList, would be better if able to identify what type of object is being rendered (artery, brain, video) but again, will probably be something to add to tracked object updates and may not be possible due to limited info available in stl files
4) Send OBJ files from host
     - unity doesn't support .stl files by default, may be easier to send .obj files to load into unity
     - this would involve setting up a way to capture and send on PathfinderGUI
     - may also allow for identifying what type of object is being rendered (see above point on sychronizing cycling)
     - on unity side, likely focusing on just processing what is sent over to turn it into somthing unity can use

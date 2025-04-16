# Unity VR Research Project

This repository contains the source code for a Unity-based VR research project focused on pseudohaptic manipulation with passive haptics with and spatial alignment with real-world environments.

## Requirements
- Unity version **6000.0.34f1**
- Meta Quest 3 headset with **room scanning capabilities**
- Compatible right-hand controller

## Setup Instructions
1. Scan your room using the headset and **save the room**.
2. Adjust the detected surfaces inside the headset to **match your real room layout** as closely as possible. Focus on the table area used for interaction.

## Running the Experiment
1. Open the scene `markTable` located in the `Scenes/` folder in Unity.
2. Sit or stand in front of a **real table**. This is where the virtual cubes will appear.
3. Put on the headset and **run the scene**.
4. Hold the **right controller** and press **any button** to ensure it's being tracked.
5. Place the controller **flat and horizontal** on the real table surface.
6. Press the **trigger button** to spawn a pair of virtual boxes on the table.
7. Use the **grip button** to grab a virtual box when your other virtual hand is near it.

## User Study Replication
To replicate the original user study:
- Place the corresponding **physical boxes** on the table **where the virtual boxes appear**.
- While interacting with the virtual box in VR, **physically grab the real box** to simulate passive haptic feedback.

# Voxel

Procedurally generated, Minecraft-like voxel world using Perlin noise and Perlin noise 3D (for cave generation). The fluid blocks haven't been implemented yet. 

The world keeps being generated on the fly when the player gets near the sides and chunks are visible or hidden, depending on the player's location and draw distance parameter.

Each chunk is 10x10x10 blocks, combined into a single mesh to improve performance.


Easily extendable by adding new layers and assigning block types, as well as probabilities for block types to that layer. 

![image](https://user-images.githubusercontent.com/36895137/201473213-6e17ba12-9efc-4558-b198-f148f09b8dbe.png)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Lighting {


    public static void RecalculateNaturalLight(ChunkLoad chunk) {

        for (int x = 0; x < VoxelData.chunkWidth; ++x) {
            for (int z = 0; z < VoxelData.chunkWidth; ++z) {
                CastNaturalLight(chunk, x, z, VoxelData.chunkHeight - 1);
            }
        }

    }

    public static void CastNaturalLight(ChunkLoad chunk, int x, int z, int yStart) {

        if (yStart > VoxelData.chunkHeight - 1) {
            Debug.Log("Attempted to cast natural light from above the world");
            yStart = VoxelData.chunkHeight - 1;
        }

        bool obstructed = false;

        for (int y = yStart; y > -1; --y) {

            VoxelState voxel = chunk.map[x, y, z];

            if (obstructed) {
                voxel.light = 0;
            } else if (voxel.properties.opacity > 0) {
                voxel.light = 0;
                obstructed = true;
            } else
                voxel.light = 15;

        }
    }

}

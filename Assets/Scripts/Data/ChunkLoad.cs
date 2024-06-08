using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ChunkLoad {

    private int x;
    private int y;

    public Vector2Int position {
        get { return new Vector2Int(x, y); }
        set { x = value.x; y = value.y; }
    }

    public ChunkLoad(Vector2Int _position) { position = _position; }
    public ChunkLoad(int _x, int _y) { x = position.x; y = position.y; }

    [HideInInspector]
    public VoxelState[,,] map = new VoxelState[VoxelData.chunkWidth, VoxelData.chunkHeight, VoxelData.chunkWidth];

    public void Populate() {

        for (int y = 0; y < VoxelData.chunkHeight; ++y) {
            for (int x = 0; x < VoxelData.chunkWidth; ++x) {
                for (int z = 0; z < VoxelData.chunkWidth; ++z) {

                    map[x, y, z] = new VoxelState(WorldData.instance.GetVoxel(new Vector3(x + position.x, y, z + position.y)));

                }
            }
        }

        Lighting.RecalculateNaturalLight(this);
        WorldData.instance.worldLoad.AddToModifiedChunkList(this);

    }

}

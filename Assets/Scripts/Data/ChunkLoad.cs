// CLEAN

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ChunkLoad {

    private int x;
    private int y;

    [System.NonSerialized] public ChunkData chunk;
    [HideInInspector] public VoxelState[,,] map = new VoxelState[VoxelData.chunkWidth, VoxelData.chunkHeight, VoxelData.chunkWidth];

    public Vector2Int position{
        get { return new Vector2Int(x, y); }
        set { x = value.x; y = value.y; }
    }

    public ChunkLoad(int _x, int _y) { x = _x; y = _y; }
    public ChunkLoad(Vector2Int _position) { position = _position; }

    
    public void Populate() {

        for (int y = 0; y < VoxelData.chunkHeight; ++y) {
            for (int x = 0; x < VoxelData.chunkWidth; ++x) {
                for (int z = 0; z < VoxelData.chunkWidth; ++z) {

                    Vector3 voxelGlobalPosition = new Vector3(x + position.x, y, z + position.y);
                    map[x, y, z] = new VoxelState(WorldData.instance.GetVoxel(voxelGlobalPosition), this, new Vector3Int(x, y, z));

                    for (int face = 0; face < 6; ++face) {

                        Vector3Int neighbor = new Vector3Int(x, y, z) + VoxelData.faceChecks[face];
                        if (IsVoxelInChunk(neighbor)) map[x, y, z].neighbors[face] = VoxelStateFromVector3Int(neighbor);
                        else map[x, y, z].neighbors[face] = WorldData.instance.worldLoad.GetVoxel(voxelGlobalPosition + VoxelData.faceChecks[face]);

                    }

                }
            }
        }

        Lighting.RecalculateNaturalLight(this);
        WorldData.instance.worldLoad.AddToModifiedChunkList(this);

    }

    public void ModifyVoxel(Vector3Int position, byte _id) {

        if (map[position.x, position.y, position.z].id == _id) return;

        VoxelState voxel = map[position.x, position.y, position.z];
        // BlockType newVoxel = WorldData.instance.blockTypes[_id];
        byte oldOpacity = voxel.properties.opacity;

        voxel.id = _id;

        if (voxel.properties.opacity != oldOpacity &&
            (position.y == VoxelData.chunkHeight - 1) || (map[position.x, position.y + 1, position.z].light == 15))
            Lighting.CastNaturalLight(this, position.x, position.z, position.y + 1);

        WorldData.instance.worldLoad.AddToModifiedChunkList(this);
        if (chunk != null) WorldData.instance.AddChunkToUpdate(chunk);

    }

    // IsVoxelInChunk() returns whether the voxel is on the inside of a chunk
    public bool IsVoxelInChunk(int x, int y, int z) {
        return !(x < 0 || x > VoxelData.chunkWidth - 1 ||
                 y < 0 || y > VoxelData.chunkHeight - 1 ||
                 z < 0 || z > VoxelData.chunkWidth - 1);
    }

    public bool IsVoxelInChunk(Vector3Int position) {
        return IsVoxelInChunk(position.x, position.y, position.z);
    }

    public VoxelState VoxelStateFromVector3Int(Vector3Int position) {
        return map[position.x, position.y, position.z];
    }

}

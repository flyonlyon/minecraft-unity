// CLEAN

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WorldLoad {

    public string worldName = "Prototype";
    public int seed;

    [System.NonSerialized] public Dictionary<Vector2Int, ChunkLoad> chunks = new Dictionary<Vector2Int, ChunkLoad>();
    [System.NonSerialized] public List<ChunkLoad> modifiedChunks = new List<ChunkLoad>();


    public WorldLoad(string _worldName, int _seed) { worldName = _worldName; seed = _seed; }
    public WorldLoad(WorldLoad world) { worldName = world.worldName; seed = world.seed; }

    public void AddToModifiedChunkList(ChunkLoad chunk) {
        if (!modifiedChunks.Contains(chunk)) modifiedChunks.Add(chunk);
    }

    public ChunkLoad RequestChunk(Vector2Int _coord, bool _create) {

        ChunkLoad chunk;

        lock (WorldData.instance.chunkListThreadLock) {

            if (chunks.ContainsKey(_coord)) {
                chunk = chunks[_coord];
            } else if (!_create) {
                chunk = null;
            } else {
                LoadChunk(_coord);
                chunk = chunks[_coord];
            }
        }

        return chunk;

    }

    public void LoadChunk(Vector2Int _coord) {

        if (chunks.ContainsKey(_coord)) return;

        ChunkLoad chunk = SaveSystem.LoadChunk(worldName, _coord);
        if (chunk != null) {
            chunks.Add(_coord, chunk);
            return;
        }

        chunks.Add(_coord, new ChunkLoad(_coord));
        chunks[_coord].Populate();

    }

    public bool IsVoxelInWorld(Vector3 position) {

        return (0 <= position.x && position.x < VoxelData.worldSizeInVoxels &&
                0 <= position.y && position.y < VoxelData.chunkHeight &&
                0 <= position.z && position.z < VoxelData.worldSizeInVoxels);

    }

    public VoxelState GetVoxel(Vector3 position) {

        if (!IsVoxelInWorld(position)) return null;

        int x = Mathf.FloorToInt(position.x / VoxelData.chunkWidth);
        int z = Mathf.FloorToInt(position.z / VoxelData.chunkWidth);

        x *= VoxelData.chunkWidth;
        z *= VoxelData.chunkWidth;

        ChunkLoad chunk = RequestChunk(new Vector2Int(x, z), false);
        if (chunk == null) return null;

        Vector3Int voxel = new Vector3Int((int)(position.x - x), (int)position.y, (int)(position.z - z));
        return chunk.map[voxel.x, voxel.y, voxel.z];

    }

    public void SetVoxel(Vector3 position, byte value) {

        if (!IsVoxelInWorld(position)) return;

        int x = Mathf.FloorToInt(position.x / VoxelData.chunkWidth);
        int z = Mathf.FloorToInt(position.z / VoxelData.chunkWidth);

        x *= VoxelData.chunkWidth;
        z *= VoxelData.chunkWidth;

        ChunkLoad chunk = RequestChunk(new Vector2Int(x, z), true);
        Vector3Int voxel = new Vector3Int((int)(position.x - x), (int)position.y, (int)(position.z - z));

        chunk.ModifyVoxel(voxel, value);

    }
}

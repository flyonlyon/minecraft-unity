// CLEAN

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class VoxelState {

    public byte id;

    [System.NonSerialized] private byte _light;
    [System.NonSerialized] public ChunkLoad chunk;
    [System.NonSerialized] public VoxelNeighbors neighbors;
    [System.NonSerialized] public Vector3Int position;

    
    public byte castLight {
        get {

            int lightLevel = _light - properties.opacity - 1;
            if (lightLevel < 0) lightLevel = 0;
            return (byte)lightLevel;

        }
    }

    // =============== HAVE CHECKED ABOVE HERE ===============

    public byte light {
        get { return _light; }
        set {

            if (value == _light) return;

            byte oldLightValue = _light;
            byte oldCastValue = castLight;
            _light = value;

            if (_light < oldLightValue) {

                List<int> neighborsToDarken = new List<int>();
                for (int face = 0; face < 6; ++face) {

                    if (neighbors[face] == null) continue;

                    if (neighbors[face].light <= oldCastValue) neighborsToDarken.Add(face);
                    else neighbors[face].PropagateLight();

                }

                foreach (int idx in neighborsToDarken)
                    neighbors[idx].light = 0;

                if (chunk.chunk != null) WorldData.instance.AddChunkToUpdate(chunk.chunk);

            } else if (_light > 1)
                PropagateLight();

        }
    }

    public float lightAsFloat { get { return light * VoxelData.unitOfLight; } }

    public Vector3Int globalPosition {
        get { return new Vector3Int(position.x + chunk.position.x, position.y, position.z + chunk.position.y); }
    }

    public BlockType properties {
        get { return WorldData.instance.blockTypes[id]; }
    }


    public VoxelState(byte _id, ChunkLoad _chunk, Vector3Int _position) {

        id = _id;
        chunk = _chunk;
        neighbors = new VoxelNeighbors(this);
        position = _position;
        light = 0;
        
    }

    public void PropagateLight() {

        if (light < 2) return;

        for (int face = 0; face < 6; ++face) {

            if (neighbors[face] != null)
                if (neighbors[face].light < castLight)
                    neighbors[face].light = castLight;

            if (chunk.chunk != null)
                WorldData.instance.AddChunkToUpdate(chunk.chunk);

        }

    }
}

public class VoxelNeighbors {

    public readonly VoxelState parent;
    public VoxelNeighbors(VoxelState _parent) { parent = _parent; }

    private VoxelState[] _neighbors = new VoxelState[6];
    public int Length { get { return _neighbors.Length; } }

    public VoxelState this[int index] {
        get {

            if (_neighbors[index] == null) {
                _neighbors[index] = WorldData.instance.worldLoad.GetVoxel(parent.globalPosition + VoxelData.faceChecks[index]);
                ReturnNeighbor(index);
            }
                
            return _neighbors[index];

        }
        set {

            _neighbors[index] = value;
            ReturnNeighbor(index);

        }
    }

    private void ReturnNeighbor(int index) {

        if (_neighbors[index] == null) return;

        if (_neighbors[index].neighbors[VoxelData.reverseFaceCheck[index]] != parent)
            _neighbors[index].neighbors[VoxelData.reverseFaceCheck[index]] = parent;

    }
}
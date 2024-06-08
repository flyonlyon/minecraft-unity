using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[HideInInspector]
[System.Serializable]
public class VoxelState {
    public byte id;

    private byte _light;
    public byte light {
        get { return _light; }
        set { _light = value; }
    }

    public float lightAsFloat { get { return (float)light * VoxelData.unitOfLight; } }

    public VoxelState(byte _id) {
        id = _id;
        light = 0;
    }

    public BlockType properties {
        get { return WorldData.instance.blockTypes[id]; }
    }
}

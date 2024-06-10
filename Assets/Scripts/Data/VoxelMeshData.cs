// CLEAN

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "New Voxel Mesh Data", menuName = "Minecraft/Voxel Mesh Data")]
public class VoxelMeshData : ScriptableObject {

    public string blockName;
    public FaceMeshData[] faces; // always going to be 6 (I think will change later)

}

[System.Serializable]
public class VertexData {

    public Vector3 position;
    public Vector2 uv;

    public VertexData(Vector3 _position, Vector2 _uv) { position = _position; uv = _uv; }

}

[System.Serializable]
public class FaceMeshData{

    public string direction;
    public Vector3 normal;
    public VertexData[] vertexData;
    public int[] triangles;

}
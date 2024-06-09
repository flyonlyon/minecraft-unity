using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkData {

    public ChunkCoord chunkCoord;
    public GameObject chunkObject;
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;

    int vertexIndex = 0;
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<int> transparentTriangles = new List<int>();
    Material[] materials = new Material[2]; 
    List<Vector2> uvs = new List<Vector2>();
    List<Color> colors = new List<Color>();
    List<Vector3> normals = new List<Vector3>();

    public Vector3 chunkPosition;

    private bool _isActive;

    ChunkLoad chunkLoad;


    // ChunkData constructor
    public ChunkData(ChunkCoord _chunkCoord) {
        chunkCoord = _chunkCoord;

        chunkObject = new GameObject();
        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        chunkObject.name = "Chunk (" + chunkCoord.x + ", " + chunkCoord.z + ")";
        chunkObject.transform.SetParent(WorldData.instance.transform);
        chunkObject.transform.position = new Vector3(chunkCoord.x * VoxelData.chunkWidth, 0f, chunkCoord.z * VoxelData.chunkWidth);
        chunkPosition = chunkObject.transform.position;

        materials[0] = WorldData.instance.material;
        materials[1] = WorldData.instance.transparentMaterial;
        meshRenderer.materials = materials;

        chunkLoad = WorldData.instance.worldLoad.RequestChunk(new Vector2Int((int)chunkPosition.x, (int)chunkPosition.z), true);
        chunkLoad.chunk = this;

        WorldData.instance.AddChunkToUpdate(this);

    }

    // CreateMeshData() adds all necessary faces to the chunk's mesh
    public void UpdateChunkMesh() {

        ClearMeshData();

        for (int y = 0; y < VoxelData.chunkHeight; ++y) {
            for (int x = 0; x < VoxelData.chunkWidth; ++x) {
                for (int z = 0; z < VoxelData.chunkWidth; ++z) {

                    if (!WorldData.instance.blockTypes[chunkLoad.map[x, y, z].id].isSolid) continue;
                    UpdateMeshData(new Vector3(x, y, z));

                }
            }
        }

        WorldData.instance.chunksToDraw.Enqueue(this);

    }



    private void ClearMeshData() {

        vertexIndex = 0;
        vertices.Clear();
        triangles.Clear();
        transparentTriangles.Clear();
        uvs.Clear();
        colors.Clear();
        normals.Clear();

    }

    public bool IsActive {

        get { return _isActive; }
        set {
            _isActive = value;
            if (chunkObject != null) chunkObject.SetActive(value);
        }

    }

    public void EditVoxel(Vector3 position, byte newID) {

        int xBlock = Mathf.FloorToInt(position.x);
        int yBlock = Mathf.FloorToInt(position.y);
        int zBlock = Mathf.FloorToInt(position.z);

        xBlock -= Mathf.FloorToInt(chunkObject.transform.position.x);
        zBlock -= Mathf.FloorToInt(chunkObject.transform.position.z);

        chunkLoad.ModifyVoxel(new Vector3Int(xBlock, yBlock, zBlock), newID);
        UpdateSurroundingVoxels(new Vector3(xBlock, yBlock, zBlock));

    }

    private void UpdateSurroundingVoxels(Vector3 position) {

        for (int face = 0; face < 6; ++face) {
            Vector3 voxel = position + VoxelData.faceChecks[face];
            if (!chunkLoad.IsVoxelInChunk((int)voxel.x, (int)voxel.y, (int)voxel.z))
                WorldData.instance.AddChunkToUpdate(WorldData.instance.GetChunkFromVector3(voxel + chunkPosition), true);
        }

    }


    public VoxelState GetVoxelFromGlobalVector3(Vector3 position) {

        int xBlock = Mathf.FloorToInt(position.x);
        int yBlock = Mathf.FloorToInt(position.y);
        int zBlock = Mathf.FloorToInt(position.z);

        xBlock -= Mathf.FloorToInt(chunkPosition.x);
        zBlock -= Mathf.FloorToInt(chunkPosition.z);

        return chunkLoad.map[xBlock, yBlock, zBlock];
    }

    // AddVoxelDataToChunk() adds the specified voxel's data to the chunk's mesh
    private void UpdateMeshData(Vector3 position) {

        int x = Mathf.FloorToInt(position.x);
        int y = Mathf.FloorToInt(position.y);
        int z = Mathf.FloorToInt(position.z);

        VoxelState voxel = chunkLoad.map[x, y, z];

        for (int face = 0; face < 6; ++face) {

            VoxelState neighbor = chunkLoad.map[x, y, z].neighbors[face];
            if (neighbor == null || !neighbor.properties.renderNeighbors) continue;

            float lightLevel = neighbor.lightAsFloat;
            int faceVertexCount = 0;

            for (int i = 0; i < voxel.properties.meshData.faces[face].vertexData.Length; ++i) {

                vertices.Add(position + voxel.properties.meshData.faces[face].vertexData[i].position);
                normals.Add(voxel.properties.meshData.faces[face].normal);
                colors.Add(new Color(0, 0, 0, lightLevel));
                AddTexture(voxel.properties.GetTextureID(face), voxel.properties.meshData.faces[face].vertexData[i].uv);
                ++faceVertexCount;

            }

            if (!voxel.properties.renderNeighbors)
                for (int i = 0; i < voxel.properties.meshData.faces[face].triangles.Length; ++i)
                    triangles.Add(vertexIndex + voxel.properties.meshData.faces[face].triangles[i]);

            else
                for (int i = 0; i < voxel.properties.meshData.faces[face].triangles.Length; ++i)
                    transparentTriangles.Add(vertexIndex + voxel.properties.meshData.faces[face].triangles[i]);

            vertexIndex += faceVertexCount;

        }
    }

    // CreateMesh() creates a mesh based on the chunk's vertices, triagles, and uvs
    public void CreateMesh() {

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();

        mesh.subMeshCount = 2;
        mesh.SetTriangles(triangles.ToArray(), 0);
        mesh.SetTriangles(transparentTriangles.ToArray(), 1);
        // mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.colors = colors.ToArray();
        mesh.normals = normals.ToArray();

        meshFilter.mesh = mesh;

    }

    // AddTexture() adds the texture with the specified ID to the chunk's uvs
    private void AddTexture(int textureID, Vector2 uv) {

        float y = textureID / VoxelData.textureAtlasSizeInBlocks;
        float x = textureID - (y * VoxelData.textureAtlasSizeInBlocks);

        y *= VoxelData.normalizedBlockTextureSize;
        x *= VoxelData.normalizedBlockTextureSize;

        y = 1f - y - VoxelData.normalizedBlockTextureSize;

        x += VoxelData.normalizedBlockTextureSize * uv.x;
        y += VoxelData.normalizedBlockTextureSize * uv.y;

        uvs.Add(new Vector2(x, y));

    }
}

public class ChunkCoord {
    public int x;
    public int z;

    public ChunkCoord(Vector3 position) {
        x = Mathf.FloorToInt(position.x) / VoxelData.chunkWidth;
        z = Mathf.FloorToInt(position.z) / VoxelData.chunkWidth;
    }

    public ChunkCoord(int _x = 0, int _z = 0) {
        x = _x;
        z = _z;
    }

    public bool Equals(ChunkCoord other) {
        if (other == null) return false;
        if (other.x == x && other.z == z) return true;
        return false;
    }
}
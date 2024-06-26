// CLEAN

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;


public class WorldData : MonoBehaviour {

    private static WorldData _instance;
    public static WorldData instance { get { return _instance; } }

    public Settings settings;

    [Header("World Generation")]
    public BiomeData biome;

    [Header("Lighting")]
    [Range(0, 1f)] public float globalLightLevel;
    public Color day;
    public Color night;
    
    [Header("Player")]
    public Transform player;
    public Vector3 spawnPosition;

    private List<ChunkCoord> activeChunks = new List<ChunkCoord>();
    public ChunkCoord playerChunkCoord;
    private ChunkCoord playerLastChunkCoord;

    [Header("Materials + Blocks")]
    public Material material;
    public Material transparentMaterial;
    public BlockType[] blockTypes;

    [Header("Chunk Data")]
    private ChunkData[,] chunks = new ChunkData[VoxelData.worldSizeInChunks, VoxelData.worldSizeInChunks];

    private List<ChunkData> chunksToUpdate = new List<ChunkData>();
    public Queue<ChunkData> chunksToDraw = new Queue<ChunkData>();

    private bool applyingModifications = false;
    public Queue<Queue<VoxelMod>> modifications = new Queue<Queue<VoxelMod>>();

    [Header("UI")]
    private bool _inUI = false;
    public GameObject creativeInventory;
    public GameObject cursorSlot;

    public GameObject debugScreen;

    [Header("Performance")]
    private Thread chunkUpdateThread;
    public object chunkUpdateThreadLock = new object();
    public object chunkListThreadLock = new object();

    [Header("Other")]
    public Clouds clouds;
    public WorldLoad worldLoad;
    public string appPath;


    public void Awake() {

        if (_instance != null && _instance != this)
            Destroy(gameObject);
        else _instance = this;

        appPath = Application.persistentDataPath;

    }

    private void Start() {

        worldLoad = SaveSystem.LoadWorld("Skibidi");

        string settingsJSON = File.ReadAllText(Application.dataPath + "/settings.cfg");
        settings = JsonUtility.FromJson<Settings>(settingsJSON);

        Random.InitState(VoxelData.seed);

        Shader.SetGlobalFloat("minGlobalLightLevel", VoxelData.minLightLevel);
        Shader.SetGlobalFloat("maxGlobalLightLevel", VoxelData.maxLightLevel);

        LoadWorld();
        SetGlobalLightValue();

        spawnPosition = new Vector3(VoxelData.worldCenter,
                                    GetTerrainHeight(VoxelData.worldCenter, VoxelData.worldCenter) + 2.5f,
                                    VoxelData.worldCenter);

        player.position = spawnPosition;
        CheckViewDistance();

        playerLastChunkCoord = GetChunkCoordFromVector3(player.position);

        if (settings.enableThreading) {
            chunkUpdateThread = new Thread(new ThreadStart(ThreadedUpdate));
            chunkUpdateThread.Start();
        }

    }

    public void SetGlobalLightValue() {

        Shader.SetGlobalFloat("GlobalLightLevel", globalLightLevel);
        Camera.main.backgroundColor = Color.Lerp(night, day, globalLightLevel);

    }

    private void Update() {

        playerChunkCoord = GetChunkCoordFromVector3(player.position);

        if (!playerChunkCoord.Equals(playerLastChunkCoord))
            CheckViewDistance();

        if (chunksToDraw.Count > 0)
            chunksToDraw.Dequeue().CreateMesh();

        if (!settings.enableThreading) {

            if (!applyingModifications)
                ApplyModifications();

            if (chunksToUpdate.Count > 0)
                UpdateChunks();
        }

        if (Input.GetKeyDown(KeyCode.F3))
            debugScreen.SetActive(!debugScreen.activeSelf);

        if (Input.GetKeyDown(KeyCode.F1))
            SaveSystem.SaveWorld(worldLoad);
    }

    private void LoadWorld() {

        for (int x = (VoxelData.worldSizeInChunks / 2) - settings.loadDistance; x < (VoxelData.worldSizeInChunks / 2) + settings.loadDistance; ++x) {
            for (int z = (VoxelData.worldSizeInChunks / 2) - settings.loadDistance; z < (VoxelData.worldSizeInChunks / 2) + settings.loadDistance; ++z) {

                worldLoad.LoadChunk(new Vector2Int(x, z));

            }
        }

    }

    public void AddChunkToUpdate(ChunkData chunk) {
        AddChunkToUpdate(chunk, false);
    }

    public void AddChunkToUpdate(ChunkData chunk, bool insert) {

        lock (chunkUpdateThreadLock) {

            if (!chunksToUpdate.Contains(chunk)) {
                if (insert) chunksToUpdate.Insert(0, chunk);
                else chunksToUpdate.Add(chunk);
            }

        }

    }

    private void UpdateChunks() {

        lock (chunkUpdateThreadLock) {

            chunksToUpdate[0].UpdateChunkMesh();
            if (!activeChunks.Contains(chunksToUpdate[0].chunkCoord))
                activeChunks.Add(chunksToUpdate[0].chunkCoord);
            chunksToUpdate.RemoveAt(0);

        }

    }

    void ThreadedUpdate() {

        while (true) {
            if (!applyingModifications) ApplyModifications();
            if (chunksToUpdate.Count > 0) UpdateChunks();
        }

    }

    private void OnDisable() {
        if (settings.enableThreading) chunkUpdateThread.Abort();
    }

    void ApplyModifications() {

        applyingModifications = true;

        while (modifications.Count > 0) {

            Queue<VoxelMod> queue = modifications.Dequeue();
            while (queue.Count > 0) {

                VoxelMod currMod = queue.Dequeue();
                worldLoad.SetVoxel(currMod.position, currMod.id);

            }
        }

        applyingModifications = false;

    }

    private ChunkCoord GetChunkCoordFromVector3(Vector3 position) {
        int x = Mathf.FloorToInt(position.x / VoxelData.chunkWidth);
        int z = Mathf.FloorToInt(position.z / VoxelData.chunkWidth);
        return new ChunkCoord(x, z);
    }

    public ChunkData GetChunkFromVector3(Vector3 position) {
        int x = Mathf.FloorToInt(position.x / VoxelData.chunkWidth);
        int z = Mathf.FloorToInt(position.z / VoxelData.chunkWidth);
        return chunks[x, z];
    }

    public void CheckViewDistance() {

        clouds.UpdateClouds();

        ChunkCoord chunkCoord = GetChunkCoordFromVector3(player.position);
        playerLastChunkCoord = playerChunkCoord;

        List<ChunkCoord> previouslyActiveChunks = new List<ChunkCoord>(activeChunks);
        activeChunks.Clear();

        for (int x = chunkCoord.x - settings.viewDistance; x < chunkCoord.x + settings.viewDistance; ++x) {
            for (int z = chunkCoord.z - settings.viewDistance; z < chunkCoord.z + settings.viewDistance; ++z) {

                ChunkCoord newChunkCoord = new ChunkCoord(x, z);

                if (IsChunkInWorld(newChunkCoord)) {

                    if (chunks[x, z] == null)
                        chunks[x, z] = new ChunkData(newChunkCoord);

                    chunks[x, z].IsActive = true;
                    activeChunks.Add(newChunkCoord);
                }

                for (int idx = 0; idx < previouslyActiveChunks.Count; ++idx)
                    if (previouslyActiveChunks[idx].Equals(newChunkCoord))
                        previouslyActiveChunks.RemoveAt(idx);
            }
        }

        foreach (ChunkCoord deadChunk in previouslyActiveChunks)
            chunks[deadChunk.x, deadChunk.z].IsActive = false;

    }

    private int GetTerrainHeight(float x, float z) {
        return Mathf.FloorToInt(biome.solidGroundHeight + biome.terrainHeight * Noise.Get2DPerlin(new Vector2(x, z), 0, biome.terrainScale));
    }

    public bool CheckForVoxel(Vector3 position) {

        VoxelState voxel = worldLoad.GetVoxel(position);
        return blockTypes[voxel.id].isSolid;

    }

    public VoxelState GetVoxelState(Vector3 position) {

        return worldLoad.GetVoxel(position);

    }

    public bool inUI {

        get { return _inUI; }
        set {
            _inUI = value;
            if (_inUI) Cursor.lockState = CursorLockMode.None;
            else Cursor.lockState = CursorLockMode.Locked;

            Cursor.visible = _inUI;
            creativeInventory.SetActive(_inUI);
            cursorSlot.SetActive(_inUI);
        }

    }

    public byte GetVoxel(Vector3 position) {

        int x = Mathf.FloorToInt(position.x);
        int y = Mathf.FloorToInt(position.y);
        int z = Mathf.FloorToInt(position.z);

        // Immutable pass
        if (!IsVoxelInWorld(position))
            return 0; // Air -- If out of bounds,

        if (y == 0)
            return 1; // Bedrock -- If bottom row

        // Basic terrain pass
        int terrainHeight = GetTerrainHeight(x, z);
        byte voxelValue = 0;

        if (y < terrainHeight - 4) voxelValue = 4; // Stone
        else if (y < terrainHeight) voxelValue = 3; // Dirt
        else if (y == terrainHeight) voxelValue = 2; // Grass

        // Second terrain pass
        if (voxelValue == 4) // If Stone
            foreach (Lode lode in biome.lodes)
                if (lode.minHeight < y && y < lode.maxHeight)
                    if (Noise.Get3DPerlin(new Vector3(x, y, z), lode.noiseOffset, lode.scale, lode.threshold))
                        voxelValue = lode.blockID;

        // Tree pass
        if (y == terrainHeight)
            if (Noise.Get2DPerlin(new Vector2(x, z), 0, biome.treeZoneScale) > biome.treeZoneThreshold)
                if (Noise.Get2DPerlin(new Vector2(x, z), 100, biome.treePlacementScale) > biome.treePlacementThreshold)
                    modifications.Enqueue(Structure.MakeTree(position, biome.minTreeSize, biome.maxTreeSize));
            
        return voxelValue;

    }

    public bool IsChunkInWorld(ChunkCoord chunkCoord) {

        return (0 < chunkCoord.x && chunkCoord.x < VoxelData.worldSizeInChunks - 1 &&
                0 < chunkCoord.z && chunkCoord.z < VoxelData.worldSizeInChunks - 1);

    }
     
    public bool IsVoxelInWorld(Vector3 position) {

        return (0 <= position.x && position.x < VoxelData.worldSizeInVoxels &&
                0 <= position.y && position.y < VoxelData.chunkHeight &&
                0 <= position.z && position.z < VoxelData.worldSizeInVoxels);

    }

}

[System.Serializable]
public class BlockType {

    public string blockName;
    public Sprite blockIcon;
    public VoxelMeshData meshData;

    public bool isSolid;
    public bool renderNeighbors;
    public byte opacity;

    public int maxStackSize;

    public int topFaceTexture;
    public int frontFaceTexture; 
    public int rightFaceTexture;
    public int backFaceTexture;
    public int leftFaceTexture;
    public int bottomFaceTexture;

    public int GetTextureID (int faceIndex) {

        switch (faceIndex) {
            case 0:
                return topFaceTexture;
            case 1:
                return frontFaceTexture;
            case 2:
                return rightFaceTexture;
            case 3:
                return backFaceTexture;
            case 4:
                return leftFaceTexture;
            case 5:
                return bottomFaceTexture;

            default:
                Debug.Log("Error in GetTextureID: invalid faceIndex");
                return 0;

        }
    }
}

public class VoxelMod {

    public Vector3 position;
    public byte id;

    public VoxelMod (Vector3 _position, byte _id) {
        position = _position;
        id = _id;
    }

}

[System.Serializable]
public class Settings {

    [Header("Performance")]
    public int loadDistance = 16;
    public int viewDistance = 8;
    public bool enableThreading = true;
    public CloudStyle clouds = CloudStyle.Fast;

    [Header("Controls")]
    [Range(0.5f, 50f)] public float mouseSensitivity = 5f;

}
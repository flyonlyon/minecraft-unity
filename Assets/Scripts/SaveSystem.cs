// CLEAN

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public static class SaveSystem {

    public static void SaveWorld(WorldLoad world) {

        string savePath = WorldData.instance.appPath + "/saves/" + world.worldName + "/";
        if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);

        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(savePath + "world.world", FileMode.Create);

        formatter.Serialize(stream, world);
        stream.Close();

        Thread newThread = new Thread(() => SaveChunks(world));
        newThread.Start();

    }

    public static WorldLoad LoadWorld(string _worldName, int _seed = 0) {

        string loadPath = WorldData.instance.appPath + "/saves/" + _worldName + "/";

        if (File.Exists(loadPath + "world.world")) {

            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(loadPath + "world.world", FileMode.Open);

            WorldLoad world = formatter.Deserialize(stream) as WorldLoad;
            stream.Close();

            return new WorldLoad(world);

        } else {

            WorldLoad world = new WorldLoad(_worldName, _seed);
            SaveWorld(world);

            return world;

        }
    }

    public static void SaveChunks(WorldLoad world) {

        List<ChunkLoad> chunks = new List<ChunkLoad>(world.modifiedChunks);
        world.modifiedChunks.Clear();

        int count = 0;
        foreach (ChunkLoad chunk in chunks) {
            SaveChunk(chunk, world.worldName);
            ++count;
        }

        Debug.Log(count + " chunks saved");
    }

    public static void SaveChunk(ChunkLoad chunk, string worldName) {

        string chunkName = chunk.position.x + "-" + chunk.position.y;

        string savePath = WorldData.instance.appPath + "/saves/" + worldName + "/chunks/";
        if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);

        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(savePath + chunkName + ".chunk", FileMode.Create);

        formatter.Serialize(stream, chunk);
        stream.Close();

    }

    public static ChunkLoad LoadChunk(string _worldName, Vector2Int position) {

        string chunkName = position.x + "-" + position.y;
        string loadPath = WorldData.instance.appPath + "/saves/" + _worldName + "/chunks/" + chunkName + ".chunk";

        if (!File.Exists(loadPath)) return null;

        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(loadPath, FileMode.Open);

        ChunkLoad chunk = formatter.Deserialize(stream) as ChunkLoad;
        stream.Close();

        return chunk;

    }
}

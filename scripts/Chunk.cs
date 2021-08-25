using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

public class Chunk
{
    [Export]
    public static int ChunkSize { get; set; } = 100; // tiles
    public static int TileSize { get; set; } = 16; // tiles
    public static int ChunkGenDistance { get; set; } = 2;
    public static int ChunkUnloadDistance { get; set; } = ChunkGenDistance + 4;

    public static ConcurrentQueue<Chunk> ConcurrentChunkQueue = new ConcurrentQueue<Chunk>();

    public Chunk()
    {
        this.Id = Guid.NewGuid();
        this.TileMapDict = new Dictionary<TileType, TileMap>();
        this.TileMapArrayDict = new Dictionary<TileType, int[,]>();
    }

    public Guid Id {get;set;}

    // this feature of directly creating the tilemaps is currently disabled (goal would be to create the tilemaps in seperate threads to load them to the scene tree).
    // Problem: The scene tree is very slow on adding new tilemaps to the game
    public Dictionary<TileType, TileMap> TileMapDict { get; set; }

    // WORKAROUND TO PREVIOUS PROBLEM!
    public Dictionary<TileType, int[,]> TileMapArrayDict { get; set; }


    // in chunk coordinates
    public Vector2 ChunkCoordinates { get; set; } 

    // in tile coordinates
    public Vector2 ChunkStartTileLocation {
        get {
            return this.ChunkCoordinates * ChunkSize;
        }
    }

    public Vector2 ChunkEndTileLocation {
        get {
            return this.ChunkCoordinates * ChunkSize + new Vector2(ChunkSize, ChunkSize);
        }
    }

    public void AddToConcurrent() {
        ConcurrentChunkQueue.Enqueue(this);
    }

}

public enum TileType {
    Stone = 0,
    Iron = 1,
    Copper = 2
}
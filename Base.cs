using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

public class Base : Node2D
{
	public Base()
	{
		_lastPlayerChunkX = 999999;
		_lastPlayerChunkY = 999999;
	}

	public OpenSimplexNoise SmallAsteroidNoise { get; private set; }
	public OpenSimplexNoise AsteroidClampingNoise { get; private set; }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// var rand = new Random();
		var rand = 123456789;

		this.SmallAsteroidNoise = new OpenSimplexNoise();
		this.SmallAsteroidNoise.Seed = rand;
		this.SmallAsteroidNoise.Octaves = 4;
		this.SmallAsteroidNoise.Period = 128;
		this.SmallAsteroidNoise.Lacunarity = 1.5f;
		this.SmallAsteroidNoise.Persistence = 0.75f;

		// partially asteroid clamping noise to overlay the smallAsteroid Noise
		this.AsteroidClampingNoise = new OpenSimplexNoise();
		this.AsteroidClampingNoise.Seed = rand;
		this.AsteroidClampingNoise.Octaves = 4;
		this.AsteroidClampingNoise.Period = 512;
		this.AsteroidClampingNoise.Lacunarity = 1f;
		this.AsteroidClampingNoise.Persistence = 0.5f;
	}

	private int _lastPlayerChunkX;
	private int _lastPlayerChunkY;

    // Generate world chunks within viewable area
	public override void _PhysicsProcess(float delta)
	{
		var playerX = (int)Math.Round(Player.PlayerCoordinates.x, MidpointRounding.AwayFromZero);
		var playerY = (int)Math.Round(Player.PlayerCoordinates.y, MidpointRounding.AwayFromZero);
		var playerChunkX = (int)Math.Round((float)playerX / (((float)Chunk.ChunkSize * Chunk.TileSize)), MidpointRounding.AwayFromZero);
		var playerChunkY = (int)Math.Round((float)playerY / (((float)Chunk.ChunkSize * Chunk.TileSize)), MidpointRounding.AwayFromZero);

        this.generateWorld(playerChunkX, playerChunkY);

		// check the concurrentQuery for items and add the chunk to the world
		// var dequeue = Chunk.ConcurrentChunkQueue.TryDequeue(out Chunk result);
		var sw = new Stopwatch();
		sw.Start();
		while (sw.ElapsedMilliseconds < 4 &&
			Chunk.ConcurrentChunkQueue.TryDequeue(out Chunk result))
		{
			GetTree().Root.CallDeferred("add_child", result.TileMapDict[TileType.Stone]);
		}
	}

	private List<KeyValuePair<int, int>> _generatedChunkDict = new List<KeyValuePair<int, int>>();
	private List<KeyValuePair<int, int>> _unloadChunkDict = new List<KeyValuePair<int, int>>();

    private void generateWorld(int playerChunkX, int playerChunkY)
    {
		var baseAsteroidTilemap = (Godot.TileMap)GetNode("Asteroid_TileMap");

        // check world generation on every player chunk change
		if(_lastPlayerChunkX == playerChunkX && _lastPlayerChunkY == playerChunkY) {
			return;
		}

		// add new chunks around player
		for (var y = (playerChunkY - Chunk.ChunkGenDistance); y <= playerChunkY + Chunk.ChunkGenDistance; y++) {
			for (var x = (playerChunkX - Chunk.ChunkGenDistance); x <= playerChunkX + Chunk.ChunkGenDistance; x++) {

				// stop if this chunk is alredy created
				if (_generatedChunkDict.Where(c => c.Key == x && c.Value == y).Any()) {
					continue;
				}
				else
				{
					_generatedChunkDict.Add(new KeyValuePair<int, int>(x, y));
				}

				// start a new thread to generate this given chunk
				var chunk = new Chunk();
				chunk.ChunkCoordinates = new Vector2(x,y);
				chunk.TileMapDict.Add(TileType.Stone, (TileMap)baseAsteroidTilemap.Duplicate());
				var thread = new System.Threading.Thread(() => GenerateChunkAsync(chunk, SmallAsteroidNoise, AsteroidClampingNoise));
				thread.Start();
				//this.GenerateChunkAsync(chunk);
			}
		}
		// remove distant chunks around player
		// for (var y = (playerChunkY - Chunk.ChunkUnloadDistance); y <= playerChunkY + Chunk.ChunkUnloadDistance; y ++) {
		// 	for (var x = (playerChunkX - Chunk.ChunkUnloadDistance); x <= playerChunkX + Chunk.ChunkUnloadDistance; x ++) {

		// 		// but never remove correctly loaded chunks
		// 		if (y < (playerChunkY - Chunk.ChunkGenDistance) || y > (playerChunkY + Chunk.ChunkGenDistance) &&
		// 			x < (playerChunkX - Chunk.ChunkGenDistance) || x > (playerChunkX + Chunk.ChunkGenDistance) &&
		// 			_generatedChunkDict.Where(c => c.Key == x && c.Value == y).Any())
		// 		{
		// 			_unloadChunkDict.Add(new KeyValuePair<int, int>(x, y));
		// 		}
		// 	}
		// }

		_lastPlayerChunkX = playerChunkX;
		_lastPlayerChunkY = playerChunkY;
    }

    public void GenerateChunkAsync(Chunk chunk, OpenSimplexNoise smallAsteroidNoise, OpenSimplexNoise asteroidClampingNoise)
    {
		var tileX = chunk.ChunkStartTileLocation.x;
		var tileY = chunk.ChunkStartTileLocation.y;

		var tilemap = chunk.TileMapDict[TileType.Stone];

		for (var y = tileY; y < tileY + Chunk.ChunkSize; y++) {
        	for (var x = tileX; x < tileX + Chunk.ChunkSize; x++) {
				var smallAsteroid_Threshold = smallAsteroidNoise.GetNoise2d(x, y);
				var asteroidClamping_Threshold = asteroidClampingNoise.GetNoise2d(x, y);

				if (smallAsteroid_Threshold > 0.3) {
					tilemap.SetCell((int)x, (int)y, 0);
				}
			}
		}

		// update bitmaskRegion only for this chunk
		tilemap.UpdateBitmaskRegion(chunk.ChunkStartTileLocation, chunk.ChunkEndTileLocation);

		// add this chunk to the newChunkCollection which will be handled by the main thread after completion
		chunk.AddToConcurrent();
    }
}

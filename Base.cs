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

	private List<Tuple<int, int>> _generatedPlayerChunkLst = new List<Tuple<int, int>>();

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
		var playerChunkX = (int)Math.Round((float)playerX / (((float)Chunk.ChunkSize * 16)), MidpointRounding.AwayFromZero);
		var playerChunkY = (int)Math.Round((float)playerY / (((float)Chunk.ChunkSize * 16)), MidpointRounding.AwayFromZero);

        this.generateWorld(playerChunkX, playerChunkY);

		// check the concurrentQuery for items and add the chunk to the world
		// var dequeue = Chunk.ConcurrentChunkQueue.TryDequeue(out Chunk result);
		var sw = new Stopwatch();
		while (sw.ElapsedMilliseconds <= 4 && 
			Chunk.ConcurrentChunkQueue.TryDequeue(out Chunk result))
		{
			sw.Start();
			var newTileMap = result.TileMapDict[TileType.Stone];
			GetTree().Root.CallDeferred("add_child", newTileMap);
			sw.Stop();
		}
	}

	private Dictionary<int, List<int>> generatedChunkDict = new Dictionary<int, List<int>>();

    private void generateWorld(int playerChunkX, int playerChunkY)
    {
		var baseAsteroidTilemap = (Godot.TileMap)GetNode("Asteroid_TileMap");
		baseAsteroidTilemap.Clear();

        // check world generation on every player chunk change
		if(_lastPlayerChunkX == playerChunkX && _lastPlayerChunkY == playerChunkY) {
			return;
		}

		// iterate chunks around player
		for (var y = (playerChunkY - Chunk.ChunkGenDistance); y <= playerChunkY + Chunk.ChunkGenDistance; y ++) {
			for (var x = (playerChunkX - Chunk.ChunkGenDistance); x <= playerChunkX + Chunk.ChunkGenDistance; x ++) {

				// stop if this chunk is alredy created
				if (generatedChunkDict.TryGetValue(x, out List<int> output)) {
					if (output.Where(c => c == y).Any()) {
						continue;
					}
					else
					{
						generatedChunkDict[x].Add(y);
					}
				}
				else {
					var lst = new List<int>();
					lst.Add(y);
					generatedChunkDict.Add(x, lst);
				}

				// start a new thread to generate this given chunk
				
				var chunk = new Chunk();
				chunk.ChunkCoordinates = new Vector2(x,y);
				chunk.TileMapDict.Add(TileType.Stone, (TileMap)baseAsteroidTilemap.Duplicate());
				var thread = new System.Threading.Thread(() => GenerateChunkAsync(chunk));
				thread.Start();
				//this.GenerateChunkAsync(chunk);
			}
		}

		_lastPlayerChunkX = playerChunkX;
		_lastPlayerChunkY = playerChunkY;
    }

    public void GenerateChunkAsync(Chunk chunk)
    {
		var tileX = chunk.ChunkStartTileLocation.x;
		var tileY = chunk.ChunkStartTileLocation.y;

		var tilemap = chunk.TileMapDict[TileType.Stone];

		for (var y = tileY; y < tileY + Chunk.ChunkSize; y++) {
        	for (var x = tileX; x < tileX + Chunk.ChunkSize; x++) {
				var smallAsteroid_Threshold = SmallAsteroidNoise.GetNoise2d(x, y);
				var asteroidClamping_Threshold = AsteroidClampingNoise.GetNoise2d(x, y);

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

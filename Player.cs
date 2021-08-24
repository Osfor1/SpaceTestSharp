using Godot;
using System;

public class Player : KinematicBody2D
{
 	[Export] public float RCSPower = 10;
	[Export] public float RCSFuel = 100;
	[Export] public float CrashSpeed = 200;
	[Export] public int Health = 100;
	public bool IsPlayerAlive 
	{
		get 
		{
			return Health > 0;
		}
	}


	[Export] public bool InfiniteHealth = false;

	private static float _playerVelocity;
	public static float PlayerVelocity 
	{
		get {
			return _playerVelocity;
		}
	}

	private static Vector2 _playerCoordinates;
	public static Vector2 PlayerCoordinates
	{
		get {
			return _playerCoordinates;
		}
	}

	public Vector2 Velocity = new Vector2();

	private Vector2 _previousVelocity = new Vector2();

	public void GetInput()
	{
		if (Input.IsActionPressed("player_right"))
			Velocity.x += 1 * RCSPower;

		if (Input.IsActionPressed("player_left"))
			Velocity.x -= 1 * RCSPower;

		if (Input.IsActionPressed("player_down"))
			Velocity.y += 1 * RCSPower;

		if (Input.IsActionPressed("player_up"))
			Velocity.y -= 1 * RCSPower;
	}

    public override void _PhysicsProcess(float delta)
	{
		if (IsPlayerAlive) {
			GetInput();
		}

		SpaceMovement(delta);

		// Set the variable to determine the velocity in the last frame
		_previousVelocity = new Vector2(Velocity);

		// set static readonly properties
		_playerVelocity = Math.Abs((Math.Abs(Velocity.x) + Math.Abs(Velocity.y)) / 2);
		_playerCoordinates = this.Position;
	}

    private void SpaceMovement(float delta)
    {
		var collisionInfo = MoveAndCollide(Velocity * delta);

		if(collisionInfo != null) {
			var collisionPoint = collisionInfo.Position;
			var collisionSpeed = Math.Abs(Math.Abs(_previousVelocity.x) + Math.Abs(_previousVelocity.y)) / 2;

			// collision handling - but always bounce when player died
			if (collisionSpeed <= CrashSpeed && IsPlayerAlive) {
				// slide and slow down
				Velocity = Velocity.Slide(collisionInfo.Normal) * 0.9f;
			} 
			else {
				// crash - slow down and bounce 
				Velocity = Velocity.Bounce(collisionInfo.Normal) / 1.75f;

				crashCalculation(collisionSpeed, delta);
			}
		}
    }

    private void crashCalculation(float collisionSpeed, float delta)
    {
		var damage = (int)Math.Round((CrashSpeed - collisionSpeed) / 5, MidpointRounding.AwayFromZero);
		if (damage < 0 && !InfiniteHealth)
		{
			Health += damage;
		}
    }
}

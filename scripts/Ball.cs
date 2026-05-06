using Godot;

public partial class Ball : CharacterBody2D
{
	[Signal] public delegate void FellDownEventHandler();

	[Export] public float Speed = 300f;

	[Export] public float MaxSpeedMultiplier = 2f;
	[Export] public float RallySpeedup = 0.05f;
	[Export] public float PaddleHitMaxAngle = 60f;
	[Export] public float PaddleVelocityInfluence = 0.0005f;

	private Vector2 _direction = new Vector2(0.5f, -1).Normalized();
	private float _speedMultiplier = 1f;

	public override void _PhysicsProcess(double delta)
	{
		if (GameState.Instance.Phase != GamePhase.Playing) return;

		Velocity = _direction * Speed * _speedMultiplier;
		var collision = MoveAndCollide(Velocity * (float)delta);

		if (collision != null)
		{
			var collider = collision.GetCollider();

			if (collider is Paddle paddle)
			{
				HandlePaddleHit(paddle);
			}
			else
			{
				_direction = _direction.Bounce(collision.GetNormal());
				IncreaseRallySpeed();

				if (collider is Brick brick)
				{
					brick.Hit();
				}
			}
		}

		var size = GetViewportRect().Size;

		Position = new Vector2(
			Mathf.Clamp(Position.X, 6, size.X - 6),
			Mathf.Max(Position.Y, 6)
		);

		// Отскоки от стен экрана — также триггерят rally
		bool wallBounce = false;
		if (Position.X <= 6 && _direction.X < 0)
		{
			_direction.X = -_direction.X;
			wallBounce = true;
		}
		if (Position.X >= size.X - 6 && _direction.X > 0)
		{
			_direction.X = -_direction.X;
			wallBounce = true;
		}
		if (Position.Y <= 6 && _direction.Y < 0)
		{
			_direction.Y = -_direction.Y;
			wallBounce = true;
		}

		if (wallBounce) IncreaseRallySpeed();

		if (Position.Y >= size.Y)
		{
			EmitSignal(SignalName.FellDown);
		}
	}

	private void HandlePaddleHit(Paddle paddle)
	{
		var prevSpeedMultiplier = _speedMultiplier;
		_speedMultiplier = 1f + ((prevSpeedMultiplier - 1) * 0.5f);

		float paddleHalfWidth = 42f * paddle.Scale.X;
		float relativeX = (Position.X - paddle.Position.X) / paddleHalfWidth;
		relativeX = Mathf.Clamp(relativeX, -1f, 1f);

		float angleRad = Mathf.DegToRad(PaddleHitMaxAngle * relativeX);
		_direction = new Vector2(Mathf.Sin(angleRad), -Mathf.Cos(angleRad));

		Vector2 paddleVelocity = paddle.GetCurrentVelocity();
		float impulseBoost = Mathf.Abs(paddleVelocity.X) * PaddleVelocityInfluence;
		_speedMultiplier = Mathf.Min(_speedMultiplier + impulseBoost, MaxSpeedMultiplier);

		Position = new Vector2(Position.X, paddle.Position.Y - 16);
	}

	private void IncreaseRallySpeed()
	{
		_speedMultiplier = Mathf.Min(_speedMultiplier + RallySpeedup, MaxSpeedMultiplier);
	}

	public void ResetPosition(Vector2 position)
	{
		Position = position;
		_direction = new Vector2(0.5f, -1).Normalized();
		_speedMultiplier = 1f;
	}

	public void SetDirection(Vector2 direction)
	{
		_direction = direction.Normalized();
	}
}

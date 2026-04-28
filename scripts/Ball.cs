using Godot;

public partial class Ball : CharacterBody2D
{
	[Signal] public delegate void FellDownEventHandler();

	[Export] public float Speed = 300f;
	private Vector2 _direction = new Vector2(0.5f, -1).Normalized();

	public override void _PhysicsProcess(double delta)
	{
		if (GameState.Instance.Phase != GamePhase.Playing) return;

		Velocity = _direction * Speed;
		var collision = MoveAndCollide(Velocity * (float)delta);

		if (collision != null)
		{
			_direction = _direction.Bounce(collision.GetNormal());

			// Если столкнулись с блоком — уничтожаем его
			if (collision.GetCollider() is Brick brick)
			{
				brick.Hit();
			}
		}

		// Отскок от краёв экрана (кроме нижнего — там потеря)
		var size = GetViewportRect().Size;
		if (Position.X <= 6 || Position.X >= size.X - 6)
			_direction.X = -_direction.X;
		if (Position.Y <= 6)
			_direction.Y = -_direction.Y;
		if (Position.Y >= size.Y)
		{
			EmitSignal(SignalName.FellDown);
		}
	}

	public void ResetPosition(Vector2 position)
	{
		Position = position;
		_direction = new Vector2(0.5f, -1).Normalized();
	}
}

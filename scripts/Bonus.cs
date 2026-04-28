using Godot;

public partial class Bonus : Area2D
{
	[Signal] public delegate void CollectedEventHandler(int bonusType);

	[Export] public float FallSpeed = 150f;
	public BonusType Type;

	public override void _Ready()
	{
		Modulate = ColorForType(Type);
		BodyEntered += OnBodyEntered;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (GameState.Instance.Phase != GamePhase.Playing) return;

		Position += new Vector2(0, FallSpeed * (float)delta);

		if (Position.Y > GetViewportRect().Size.Y)
		{
			QueueFree();
		}
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is Paddle)
		{
			EmitSignal(SignalName.Collected, (int)Type);
			QueueFree();
		}
	}

	private static Color ColorForType(BonusType type) => type switch
	{
		BonusType.LongPaddle  => new Color(0.3f, 0.8f, 1.0f),    // голубой
		BonusType.ShortPaddle => new Color(1.0f, 0.4f, 0.4f),    // красный
		BonusType.ExtraLife   => new Color(0.4f, 1.0f, 0.4f),    // зелёный
		BonusType.MultiBall   => new Color(1.0f, 1.0f, 0.4f),    // жёлтый
		_ => Colors.White,
	};
}

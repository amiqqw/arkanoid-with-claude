using Godot;

public partial class Brick : StaticBody2D
{
	[Signal] public delegate void DestroyedEventHandler();

	[Export] public int MaxHits = 1;
	[Export] public bool Indestructible = false;
	[Export] public int ScorePerDestroy = 100;

	private int _hitsRemaining;
	private Label _hitsLabel;

	private static Color ColorForHits(int hits) => hits switch
	{
		-1 => new Color(0.4f, 0.4f, 0.4f),
		1 => new Color(0.4f, 0.9f, 0.4f),   // зелёный
		2 => new Color(0.95f, 0.7f, 0.2f),  // оранжевый
		3 => new Color(0.9f, 0.3f, 0.3f),   // красный
		_ => Colors.White,
	};

	public override void _Ready()
	{
		_hitsLabel = GetNode<Label>("HitsLabel");
		_hitsRemaining = MaxHits;

		UpdateVisual();
	}

	/// <summary>
	/// Вызывается мячом при попадании.
	/// Возвращает true, если блок был разрушен этим ударом.
	/// </summary>
	public bool Hit()
	{
		if (Indestructible) return false;

		_hitsRemaining -= 1;

		if (_hitsRemaining <= 0)
		{
			OnDestroyEffect();
			GameState.Instance.AddScore(ScorePerDestroy);
			EmitSignal(SignalName.Destroyed);
			QueueFree();
			return true;
		}

		UpdateVisual();
		return false;
	}

	private void UpdateVisual()
	{
		Color color;

		if (Indestructible)
		{
			color = ColorForHits(-1);
			_hitsLabel.Text = "X";
		}
		else
		{
			color = ColorForHits(_hitsRemaining);
			_hitsLabel.Text = _hitsRemaining.ToString();
		}

		Modulate = color;
	}

	/// <summary>
	/// Точка расширения. Базовая реализация ничего не делает.
	/// BonusBrick переопределяет этот метод, чтобы уронить бонус.
	/// </summary>
	protected virtual void OnDestroyEffect()
	{
	}
}

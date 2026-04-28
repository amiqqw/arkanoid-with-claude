using Godot;

public partial class BonusBrick : Brick
{
	[Signal] public delegate void BonusDropEventHandler(Vector2 dropPosition, int bonusType);

	[Export] public BonusType DropType = BonusType.LongPaddle;

	public override void _Ready()
	{
		base._Ready();
		Modulate = Colors.White;
	}

	protected override void OnDestroyEffect()
	{
		EmitSignal(SignalName.BonusDrop, GlobalPosition, (int)DropType);
	}
}

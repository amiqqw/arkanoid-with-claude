using Godot;

public partial class Paddle : CharacterBody2D
{
	[Export] public float Speed = 400f;

	[Signal] public delegate void InputModeChangedEventHandler(int mode);

	public InputMode CurrentMode => _inputMode;
	private InputMode _inputMode = InputMode.Keyboard;

	private float _baseScaleX;
	private Timer _sizeTimer;
	private Vector2 _mouseVelocity = Vector2.Zero;

	public override void _Ready()
	{
		_baseScaleX = Scale.X;

		_sizeTimer = new Timer { OneShot = true };
		AddChild(_sizeTimer);
		_sizeTimer.Timeout += RestoreSize;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.M)
		{
			ToggleInputMode();
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (GameState.Instance.Phase != GamePhase.Playing) return;

		if (_inputMode == InputMode.Keyboard)
		{
			HandleKeyboard();
		}
		else
		{
			HandleMouse(delta);
		}

		ClampToScreen();
	}

	private void ToggleInputMode()
	{
		_inputMode = (_inputMode == InputMode.Keyboard) ? InputMode.Mouse : InputMode.Keyboard;
		_mouseVelocity = Vector2.Zero;
		Velocity= Vector2.Zero;
		GD.Print($"Paddle input mode: {_inputMode}");
		EmitSignal(SignalName.InputModeChanged, (int)_inputMode);
	}

	private void HandleKeyboard()
	{
		float direction = Input.GetAxis("ui_left", "ui_right");
		Velocity = new Vector2(direction * Speed, 0);
		MoveAndSlide();
	}

	private void HandleMouse(double delta)
	{
		float targetX = GetGlobalMousePosition().X;
		float dx = targetX - Position.X;

		float maxStep = Speed * (float)delta;
		dx = Mathf.Clamp(dx, -maxStep, maxStep);

		Position += new Vector2(dx, 0);
		_mouseVelocity = new Vector2(dx / (float)delta, 0);

		Velocity = Vector2.Zero;
	}

	private void ClampToScreen()
	{
		var viewportWidth = GetViewportRect().Size.X;
		Position = new Vector2(
			Mathf.Clamp(Position.X, 40, viewportWidth - 40),
			Position.Y
		);
	}

	public void ApplySizeMultiplier(float multiplier, float duration)
	{
		Scale = new Vector2(_baseScaleX * multiplier, Scale.Y);
		_sizeTimer.Stop();
		_sizeTimer.Start(duration);
	}

	public void ResetSize()
	{
		_sizeTimer.Stop();
		Scale = new Vector2(_baseScaleX, Scale.Y);
	}

	private void RestoreSize()
	{
		Scale = new Vector2(_baseScaleX, Scale.Y);
	}

	public Vector2 GetCurrentVelocity() => (_inputMode == InputMode.Keyboard) ? Velocity : _mouseVelocity;

}

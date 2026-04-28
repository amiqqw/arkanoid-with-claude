using Godot;

public partial class Paddle : CharacterBody2D
{
	[Export] public float Speed = 400f;

	[Signal] public delegate void InputModeChangedEventHandler(int mode);

	public InputMode CurrentMode => _inputMode;
	private InputMode _inputMode = InputMode.Keyboard;

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.M)
		{
			ToggleInputMode();
		}
	}

	private void ToggleInputMode()
	{
		_inputMode = (_inputMode == InputMode.Keyboard) ? InputMode.Mouse : InputMode.Keyboard;
		GD.Print($"Paddle input mode: {_inputMode}");
		EmitSignal(SignalName.InputModeChanged, (int)_inputMode);
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

		// Ограничиваем смещение скоростью
		float maxStep = Speed * (float)delta;
		dx = Mathf.Clamp(dx, -maxStep, maxStep);

		// Двигаемся вручную (без MoveAndSlide), потому что цель — точная позиция
		Position += new Vector2(dx, 0);

		// Velocity всё равно сбросим, чтобы не накапливалась "память" из keyboard-режима
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
}

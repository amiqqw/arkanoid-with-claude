using System;
using Godot;

public partial class Main : Node2D
{
	[Export] public PackedScene BrickScene;
	[Export] public PackedScene BallScene;
	[Export] public PackedScene PaddleScene;

	private HUD _hud;
	private Ball _ball;
	private Paddle _paddle;
	private Node _bricksContainer;

	private static readonly Vector2 BallStartPosition = new(240, 320);
	private static readonly Vector2 PaddleStartPosition = new(240, 580);

	private float _baseBallSpeed;
	private int _destructibleBricksRemaining;

	public override void _Ready()
	{
		_bricksContainer = new Node { Name = "Bricks" };
		AddChild(_bricksContainer);

		_paddle = PaddleScene.Instantiate<Paddle>();
		AddChild(_paddle);

		_hud = GetNode<HUD>("HUD");
		_paddle.InputModeChanged += _hud.OnPaddleInputModeChanged;
		_hud.OnPaddleInputModeChanged((int)_paddle.CurrentMode);

		_ball = BallScene.Instantiate<Ball>();
		_ball.FellDown += OnFellDown;
		AddChild(_ball);

		_baseBallSpeed = _ball.Speed;

		GameState.Instance.GameOver += OnGameOver;
		GameState.Instance.PhaseChanged += OnPhaseChanged;

		StartNewGame();
	}

	public override void _Process(double delta)
	{
		if (GameState.Instance.Phase == GamePhase.Playing && _destructibleBricksRemaining <= 0)
		{
			GameState.Instance.ChangePhase(GamePhase.Win);
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey keyEvent || !keyEvent.Pressed) return;

		var phase = GameState.Instance.Phase;

		if (keyEvent.Keycode == Key.Space && phase == GamePhase.Start)
		{
			GameState.Instance.ChangePhase(GamePhase.Playing);
		}

		if (keyEvent.Keycode == Key.R && (phase == GamePhase.GameOver || phase == GamePhase.Win))
		{
			StartNewGame();
		}
	}

	/// <summary>
    /// Полный сброс — после Game Over или при первом запуске.
    /// </summary>
	private void StartNewGame()
	{
		foreach (Node child in _bricksContainer.GetChildren())
		{
			child.QueueFree();
		}

		SpawnBricks();

		_ball.ResetPosition(BallStartPosition);
		_paddle.Position = PaddleStartPosition;

		GameState.Instance.ResetLives();
		GameState.Instance.ResetScore();

		GameState.Instance.ChangePhase(GamePhase.Start);
	}

	private void SpawnBricks()
	{
		_destructibleBricksRemaining = 0;

		int rows = LevelLayout.GetLength(0);
		int cols = LevelLayout.GetLength(1);

		for (int row = 0; row < rows; row++)
		{
			for (int col = 0; col < cols; col++)
			{
				int code = LevelLayout[row, col];
				if (code == 0) continue;   // пустая клетка

				var brick = BrickScene.Instantiate<Brick>();
				brick.Position = new Vector2(35 + col * 55, 100 + row * 25);

				if (code == -1)
				{
					brick.Indestructible = true;
					brick.MaxHits = 1;
					brick.ScorePerDestroy = 0;
				}
				else
				{
					brick.MaxHits = code;
					brick.ScorePerDestroy = code * 100;
					brick.Destroyed += OnBrickDestroyed;
					_destructibleBricksRemaining += 1;
				}

				_bricksContainer.AddChild(brick);
			}
		}
	}

	private void OnBrickDestroyed()
	{
		_destructibleBricksRemaining -= 1;
	}

	private void OnFellDown()
	{
		GameState.Instance.LoseLife();

		if (GameState.Instance.Lives > 0)
		{
			_ball.ResetPosition(BallStartPosition);
			GameState.Instance.ChangePhase(GamePhase.Start);
		}
	}

	private void OnGameOver()
	{
		GD.Print("git gud"); // для вайба
		_ball.ResetPosition(BallStartPosition);
	}

	private void OnPhaseChanged(int phaseInt)
	{
		var phase = (GamePhase)phaseInt;
		Input.MouseMode = (phase == GamePhase.Playing)
			? Input.MouseModeEnum.Hidden
			: Input.MouseModeEnum.Visible;
	}
}

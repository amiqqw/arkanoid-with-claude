using System;
using Godot;

public partial class Main : Node2D
{
	[Export] public PackedScene BrickScene;
	[Export] public PackedScene BallScene;
	[Export] public PackedScene PaddleScene;
	[Export] public PackedScene BonusBrickScene;
	[Export] public PackedScene BonusScene;

	private Node _bonusesContainer;
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

		_bonusesContainer = new Node { Name = "Bonuses" };
		AddChild(_bonusesContainer);

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

		if (keyEvent.Keycode == Key.R)
		{
			if (phase == GamePhase.GameOver) StartNewGame();
			else if (phase == GamePhase.Win) AdvanceToNextLevel();
		}
	}

	/// <summary>
	/// Полный сброс — после Game Over или при первом запуске.
	/// </summary>
	private void StartNewGame()
	{
		GameState.Instance.ResetLives();
		GameState.Instance.ResetScore();
		GameState.Instance.ResetProgression();

		LoadLevel(GameState.Instance.CurrentLevel);
	}

	private void AdvanceToNextLevel()
	{
		int nextLevel = GameState.Instance.CurrentLevel + 1;

		if (nextLevel > Levels.Count)
		{
			GameState.Instance.IncreaseSpeedForNewLoop();
			nextLevel = 1;
		}

		GameState.Instance.SetLevel(nextLevel);
		LoadLevel(nextLevel);
	}
	
	private void LoadLevel(int level)
	{
		foreach (Node child in _bricksContainer.GetChildren())
			child.QueueFree();

		foreach (Node child in _bonusesContainer.GetChildren())
			child.QueueFree();

		var layout = Levels.Layouts[level - 1];
		SpawnBricks(layout);

		_ball.ResetPosition(BallStartPosition);
		_paddle.Position = PaddleStartPosition;
		_paddle.ResetSize();

		_ball.Speed = _baseBallSpeed * GameState.Instance.BallSpeedMultiplier;

		GameState.Instance.ChangePhase(GamePhase.Start);
	}

	private void SpawnBricks(int[,] layout)
	{
		_destructibleBricksRemaining = 0;

		int rows = layout.GetLength(0);
		int cols = layout.GetLength(1);

		for (int row = 0; row < rows; row++)
		{
			for (int col = 0; col < cols; col++)
			{
				int code = layout[row, col];
				if (code == 0) continue;

				Vector2 position = new(35 + col * 55, 100 + row * 25);

				if (code >= 10)
				{
					// Бонусный блок: 10/20/30 = прочность 1/2/3
					int hits = code / 10;
					var bonusBrick = BonusBrickScene.Instantiate<BonusBrick>();
					bonusBrick.Position = position;
					bonusBrick.MaxHits = hits;
					bonusBrick.ScorePerDestroy = hits * 100;
					bonusBrick.DropType = (BonusType)GD.RandRange(0, 3);   // случайный тип
					bonusBrick.Destroyed += OnBrickDestroyed;
					bonusBrick.BonusDrop += OnBonusDrop;
					_bricksContainer.AddChild(bonusBrick);
					_destructibleBricksRemaining += 1;
				}
				else if (code == -1)
				{
					var brick = BrickScene.Instantiate<Brick>();
					brick.Position = position;
					brick.Indestructible = true;
					brick.MaxHits = 1;
					brick.ScorePerDestroy = 0;
					_bricksContainer.AddChild(brick);
				}
				else
				{
					var brick = BrickScene.Instantiate<Brick>();
					brick.Position = position;
					brick.MaxHits = code;
					brick.ScorePerDestroy = code * 100;
					brick.Destroyed += OnBrickDestroyed;
					_bricksContainer.AddChild(brick);
					_destructibleBricksRemaining += 1;
				}
			}
		}
	}

	private void OnBonusDrop(Vector2 position, int bonusTypeInt)
	{
		var bonus = BonusScene.Instantiate<Bonus>();
		bonus.Position = position;
		bonus.Type = (BonusType)bonusTypeInt;
		bonus.Collected += OnBonusCollected;
		_bonusesContainer.AddChild(bonus);
	}

	private void OnBonusCollected(int bonusTypeInt)
	{
		var type = (BonusType)bonusTypeInt;
		GD.Print($"Bonus collected: {type}");

		switch (type)
		{
			case BonusType.LongPaddle:
				_paddle.ApplySizeMultiplier(1.5f, 10f);
				break;
			case BonusType.ShortPaddle:
				_paddle.ApplySizeMultiplier(0.7f, 10f);
				break;
			case BonusType.ExtraLife:
				GameState.Instance.AddLife();
				break;
			case BonusType.MultiBall:
				// Заглушка для этапа А — реализуем на этапе B
				GD.Print("MultiBall: not implemented yet");
				break;
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

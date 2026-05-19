using System;
using System.Collections.Generic;
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
	private List<Ball> _balls = new();
	private Paddle _paddle;
	private Node _bricksContainer;

	private static readonly Vector2 BallStartPosition = new(240, 320);
	private static readonly Vector2 PaddleStartPosition = new(240, 550);

	private float _baseBallSpeed;
	private int _destructibleBricksRemaining;

	public override void _Ready()
	{
		GetTree().AutoAcceptQuit = false;

		_bricksContainer = new Node { Name = "Bricks" };
		AddChild(_bricksContainer);

		_bonusesContainer = new Node { Name = "Bonuses" };
		AddChild(_bonusesContainer);

		_hud = GetNode<HUD>("HUD");
		_hud.StartGameRequested   += StartNewGame;
		_hud.ResetScoresRequested += ResetHighScores;
		_hud.BackToMenuRequested  += ReturnToMenu;
		_hud.PauseToggleRequested += OnPauseToggle;
		_hud.ToggleInputRequested += OnToggleInputFromPause;

		var probe = BallScene.Instantiate<Ball>();
		_baseBallSpeed = probe.Speed;
		probe.QueueFree();

		GameState.Instance.GameOver += OnGameOver;
		GameState.Instance.PhaseChanged += OnPhaseChanged;
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

		if (keyEvent.Keycode == Key.Space)
		{
			if (phase == GamePhase.Start) GameState.Instance.ChangePhase(GamePhase.Playing);
			if (phase == GamePhase.Win) AdvanceToNextLevel();
		}

		if (keyEvent.Keycode == Key.W && phase == GamePhase.Playing)
		{
			GameState.Instance.ChangePhase(GamePhase.Win);
		}

		if (keyEvent.Keycode == Key.R && phase == GamePhase.GameOver)
		{
			if (_hud.IsAwaitingName) return;

			if (phase == GamePhase.GameOver) StartNewGame();
		}
	}
	
	private void ResetHighScores()
	{
		HighScoreTable.Instance.Clear();
		GameState.Instance.RefreshHiScoreFromTable();
		_hud.RefreshScoresDisplay();
	}

	private void ReturnToMenu()
	{
		GetTree().Paused = false;
		GameState.Instance.ChangePhase(GamePhase.MainMenu);
	}

	private void StartNewGame()
	{
		GameState.Instance.ResetLives();
		GameState.Instance.ResetScore();
		GameState.Instance.ResetTime();
		GameState.Instance.ResetProgression();

		LoadLevel(GameState.Instance.CurrentLevel);
	}

	private void AdvanceToNextLevel()
	{
		int nextLevel = GameState.Instance.CurrentLevel + 1;

		if (nextLevel > Levels.HandcraftedCount)
		{
			GameState.Instance.IncreaseSpeed(0.1f);
		}

		GameState.Instance.SetLevel(nextLevel);
		LoadLevel(nextLevel);
	}

	private void LoadLevel(int level)
	{
		LoadGameField();

		foreach (Node child in _bricksContainer.GetChildren())
			child.QueueFree();

		foreach (Node child in _bonusesContainer.GetChildren())
			child.QueueFree();

		foreach (var item in _balls)
			item.QueueFree();
		_balls.Clear();

		int[,] layout = level <= Levels.HandcraftedCount
			? Levels.Layouts[level - 1]
			: Levels.GenerateRandom();

		SpawnBricks(layout);

		var ball = BallScene.Instantiate<Ball>();
		AddBall(ball, BallStartPosition);

		_paddle.Position = PaddleStartPosition;
		_paddle.ResetSize();

		GameState.Instance.ChangePhase(GamePhase.Start);
	}

	private void LoadGameField()
	{
		if (_paddle != null) return;

		_paddle = PaddleScene.Instantiate<Paddle>();
		_paddle.Position = PaddleStartPosition;
		AddChild(_paddle);

		_paddle.InputModeChanged += _hud.OnPaddleInputModeChanged;
		_hud.OnPaddleInputModeChanged((int)_paddle.CurrentMode);
	}

	private void UnloadGameField()
	{
		// Блоки
		foreach (Node child in _bricksContainer.GetChildren())
			child.QueueFree();

		// Бонусы
		foreach (Node child in _bonusesContainer.GetChildren())
			child.QueueFree();

		// Мячи
		foreach (var ball in _balls)
			ball.QueueFree();
		_balls.Clear();

		// Paddle
		if (_paddle != null)
		{
			_paddle.QueueFree();
			_paddle = null;
		}
	}
	
	private void AddBall(Ball ball, Vector2 position, Vector2? direction = null)
	{
		ball.Position = position;
		if (direction.HasValue)
		{
			ball.SetDirection(direction.Value);
		}
		ball.Speed = _baseBallSpeed * GameState.Instance.BallSpeedMultiplier;

		// Замыкание захватывает 'ball', и обработчик знает, какой именно мяч упал
		ball.FellDown += () => OnBallFellDown(ball);

		_balls.Add(ball);
		AddChild(ball);
	}

	private void SpawnBricks(int[,] layout)
	{
		_destructibleBricksRemaining = 0;

		int rows = layout.GetLength(0);
		int cols = layout.GetLength(1);

		var viewportSize = GetViewportRect().Size;
		float cellWidth = viewportSize.X / cols;
		float cellHeight = 25f;
		float topMargin = 126f;

		float scaleX = cellWidth / 50f;

		for (int row = 0; row < rows; row++)
		{
			for (int col = 0; col < cols; col++)
			{
				int code = layout[row, col];
				if (code == 0) continue;

				Vector2 position = new Vector2(
					cellWidth * (col + 0.5f),
					topMargin + cellHeight * row
				);

				if (code >= 10)
				{
					int hits = code / 10;
					var bonusBrick = BonusBrickScene.Instantiate<BonusBrick>();
					bonusBrick.Position = position;
					bonusBrick.Scale = new Vector2(scaleX, 1f);
					bonusBrick.MaxHits = hits;
					bonusBrick.ScorePerDestroy = hits * 100;
					bonusBrick.DropType = (BonusType)GD.RandRange(0, 3);
					bonusBrick.Destroyed += OnBrickDestroyed;
					bonusBrick.BonusDrop += OnBonusDrop;
					_bricksContainer.AddChild(bonusBrick);
					_destructibleBricksRemaining += 1;
				}
				else if (code == -1)
				{
					var brick = BrickScene.Instantiate<Brick>();
					brick.Position = position;
					brick.Scale = new Vector2(scaleX, 1f);
					brick.Indestructible = true;
					brick.MaxHits = 1;
					brick.ScorePerDestroy = 0;
					_bricksContainer.AddChild(brick);
				}
				else
				{
					var brick = BrickScene.Instantiate<Brick>();
					brick.Position = position;
					brick.Scale = new Vector2(scaleX, 1f);
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
				SpawnExtraBalls();
				break;
		}
	}

	private void OnBrickDestroyed()
	{
		_destructibleBricksRemaining -= 1;
	}

	private void OnBallFellDown(Ball ball)
	{
		_balls.Remove(ball);
		ball.QueueFree();

		if (_balls.Count == 0)
		{
			GameState.Instance.LoseLife();

			if (GameState.Instance.Lives > 0)
			{
				var newBall = BallScene.Instantiate<Ball>();
				AddBall(newBall, BallStartPosition);
				GameState.Instance.ChangePhase(GamePhase.Start);
			}
		}
	}

	private void OnGameOver()
	{
		GD.Print("git gud"); // для вайба
	}

	private void OnPhaseChanged(int phaseInt)
	{
		var phase = (GamePhase)phaseInt;
		// Input.MouseMode = (phase == GamePhase.Playing)
		// 	? Input.MouseModeEnum.Hidden
		// 	: Input.MouseModeEnum.Visible;

		if (phase == GamePhase.GameOver || phase == GamePhase.Win || phase == GamePhase.MainMenu)
		{
			UnloadGameField();
		}
	}

	private void SpawnExtraBalls()
	{
		var spawnPoints = new List<Vector2>();
		foreach (var ball in _balls)
		{
			spawnPoints.Add(ball.Position + new Vector2(12, 0));
		}

		foreach (var pos in spawnPoints)
		{
			var clone1 = BallScene.Instantiate<Ball>();
			var clone2 = BallScene.Instantiate<Ball>();
			AddBall(clone1, pos, new Vector2(-0.5f, -1).Normalized());
			AddBall(clone2, pos, new Vector2(-0.2f, -0.8f).Normalized());
		}
	}

	public override void _Notification(int what)
	{
		if (what == NotificationWMCloseRequest)
		{
			OnQuitRequested();
		}
	}

	private void OnQuitRequested()
	{
		GD.Print("Window close requested — quitting cleanly.");
		GetTree().Quit();
	}

	private void OnPauseToggle()
	{
		if (GameState.Instance.Phase != GamePhase.Playing) return;

		bool nowPaused = !GetTree().Paused;
		GetTree().Paused = nowPaused;

		if (nowPaused) _hud.ShowPauseMenu();
		else _hud.HidePauseMenu();
	}
	
	private void OnToggleInputFromPause()
	{
		if (_paddle == null) return;
		_paddle.ToggleInputMode();
	}
}

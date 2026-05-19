using Godot;
using System;
using System.Xml.Serialization;

public partial class HUD : CanvasLayer
{
	[Signal] public delegate void StartGameRequestedEventHandler();
	[Signal] public delegate void ResetScoresRequestedEventHandler();
	[Signal] public delegate void BackToMenuRequestedEventHandler();
	[Signal] public delegate void PauseToggleRequestedEventHandler();
	[Signal] public delegate void ToggleInputRequestedEventHandler();
	[Signal] public delegate void OpenBenchmarksRequestedEventHandler();

	private Label _scoreLabel;
	private Label _livesLabel;
	private Label _hiScoreLabel;
	private Label _messageLabel;
	private Label _inputModeLabel;
	private Label _timeLabel;

	private Panel _highScoresPanel;
	private Label _scoresLabel;
	private HBoxContainer _nameInputBox;
	private LineEdit _nameInput;
	private int _newEntryIndex = -1;
	private int _pendingScore = 0;
	public bool IsAwaitingName => _newEntryIndex != -1 && _nameInputBox.Visible;

	private Panel _mainMenuPanel;
	private Button _startButton;
	private Button _viewScoresButton;
	private Button _resetButton;
	private Button _quitButton;
	private Button _backButton;
	private Label _hintLabel;
	private ConfirmationDialog _resetConfirmDialog;
	private bool _viewingScoresFromMenu = false;

	private Panel _pausePanel;
	private Button _resumeButton;
	private Button _toggleInputButton;
	private Button _quitToMenuButton;
	private InputMode _currentInputMode = InputMode.Mouse;

	private Panel _benchmarkPanel;
	private Label _benchmarkResultsLabel;
	private Button _runBenchmarkButton;
	private Button _backFromBenchmarkButton;
	private Button _benchmarksButton;
	private bool _viewingBenchmarksFromMenu = false;

	private Control _topBar;
	private Control _bottomBar;

	private int _currentLives;
	private int _currentLevel;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_messageLabel = GetNode<Label>("MessageLabel");
		_inputModeLabel = GetNode<Label>("BottomBar/InputModeLabel");
		_timeLabel = GetNode<Label>("BottomBar/TimeLabel");

		_scoreLabel = GetNode<Label>("TopBar/ScoreLabel");
		_livesLabel = GetNode<Label>("TopBar/LivesLabel");
		_hiScoreLabel = GetNode<Label>("TopBar/HiScoreLabel");

		_topBar    = GetNode<Control>("TopBar");
		_bottomBar = GetNode<Control>("BottomBar");

		_highScoresPanel = GetNode<Panel>("HighScoresPanel");
		_scoresLabel = GetNode<Label>("HighScoresPanel/VBox/ScoresLabel");
		_nameInputBox = GetNode<HBoxContainer>("HighScoresPanel/VBox/NameInputBox");
		_nameInput = GetNode<LineEdit>("HighScoresPanel/VBox/NameInputBox/NameInput");
		_hintLabel = GetNode<Label>("HighScoresPanel/VBox/HintLabel");
		_backButton = GetNode<Button>("HighScoresPanel/VBox/BackButton");

		_mainMenuPanel = GetNode<Panel>("MainMenuPanel");
		_startButton = GetNode<Button>("MainMenuPanel/MarginContainer/VBox/StartButton");
		_viewScoresButton = GetNode<Button>("MainMenuPanel/MarginContainer/VBox/ViewScoresButton");
		_resetButton = GetNode<Button>("MainMenuPanel/MarginContainer/VBox/ResetButton");
		_quitButton = GetNode<Button>("MainMenuPanel/MarginContainer/VBox/QuitButton");

		_pausePanel = GetNode<Panel>("PausePanel");
		_resumeButton = GetNode<Button>("PausePanel/MarginContainer/VBoxContainer/ResumeButton");
		_toggleInputButton = GetNode<Button>("PausePanel/MarginContainer/VBoxContainer/ToggleInputButton");
		_quitToMenuButton = GetNode<Button>("PausePanel/MarginContainer/VBoxContainer/QuitToMenuButton");

		_resumeButton.Pressed += () => EmitSignal(SignalName.PauseToggleRequested);
		_toggleInputButton.Pressed += () => EmitSignal(SignalName.ToggleInputRequested);
		_quitToMenuButton.Pressed += () => EmitSignal(SignalName.BackToMenuRequested);

		_resetConfirmDialog = GetNode<ConfirmationDialog>("ResetConfirmDialog");
		_resetConfirmDialog.Confirmed += () => EmitSignal(SignalName.ResetScoresRequested);

		_benchmarksButton = GetNode<Button>("MainMenuPanel/MarginContainer/VBox/BenchmarksButton");

		_benchmarkPanel          = GetNode<Panel>("BenchmarkPanel");
		_benchmarkResultsLabel   = GetNode<Label>("BenchmarkPanel/MarginContainer/VBoxContainer/ScrollContainer/HBoxContainer/ResultsLabel");
		_runBenchmarkButton      = GetNode<Button>("BenchmarkPanel/MarginContainer/VBoxContainer/Buttons/RunButton");
		_backFromBenchmarkButton = GetNode<Button>("BenchmarkPanel/MarginContainer/VBoxContainer/Buttons/BackButton");

		_benchmarksButton.Pressed        += OnBenchmarksPressed;
		_runBenchmarkButton.Pressed      += OnRunBenchmarkPressed;
		_backFromBenchmarkButton.Pressed += OnBackFromBenchmarkPressed;

		_startButton.Pressed += () => EmitSignal(SignalName.StartGameRequested);
		_resetButton.Pressed += () => _resetConfirmDialog.PopupCentered();
		_quitButton.Pressed += () => GetTree().Quit();
		_viewScoresButton.Pressed += OnViewScoresPressed;
		_backButton.Pressed += OnBackPressed;


		_nameInput.TextSubmitted += OnNameSubmitted;

		var gs = GameState.Instance;

		gs.ScoreChanged += OnScoreChanged;
		gs.LivesChanged += OnLivesChanged;
		gs.PhaseChanged += OnPhaseChange;
		gs.LevelChanged += OnLevelChanged;
		gs.TimeChanged += OnTimeChanged;

		_currentLives = gs.Lives;
		_currentLevel = gs.CurrentLevel;

		OnLivesChanged(gs.Lives);
		OnScoreChanged(gs.Score, gs.HiScore);
		OnPhaseChange((int)gs.Phase);
	}
	
	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey keyEvent || !keyEvent.Pressed) return;

		if (keyEvent.Keycode == Key.Escape)
		{
			var phase = GameState.Instance.Phase;
			if (phase == GamePhase.Playing)
			{
				EmitSignal(SignalName.PauseToggleRequested);
			}
		}
	}

	private void OnLevelChanged(int level)
	{
		_currentLevel = level;
		UpdateLivesLabel();
	}

	private void OnLivesChanged(int lives)
	{
		_currentLives = lives;
		UpdateLivesLabel();
	}

	private void OnTimeChanged(int seconds)
	{
		_timeLabel.Text = $"Time: {seconds/60}m {seconds % 60}s";
	}

	private void UpdateLivesLabel()
	{
		_livesLabel.Text = $"Lives: {_currentLives}\nLvl: {_currentLevel}";
		if (_currentLives >= 3)
		{
			_livesLabel.Modulate = Colors.Green;
		}
		else
		{
			switch (_currentLives)
			{
				case 0:
					_livesLabel.Modulate = Colors.Gray;
					break;
				case 1:
					_livesLabel.Modulate = Colors.Red;
					break;
				case 2: 
					_livesLabel.Modulate = Colors.Yellow;
					break;
				default:
					break;
			}
		}
	}

	private void OnScoreChanged(int score, int hiScore)
	{
		_scoreLabel.Text = $"Score: {score}";
		_hiScoreLabel.Text = $"Hi Score: {hiScore}";
	}

	private void OnPhaseChange(int newPhaseInt)
	{
		var phase = (GamePhase)newPhaseInt;
		_viewingScoresFromMenu = false;
		_pausePanel.Visible = false;
		_viewingBenchmarksFromMenu = false;
		_benchmarkPanel.Visible = false;
		_topBar.Visible = true;
		_bottomBar.Visible = true;

		switch (phase)
		{
			case GamePhase.MainMenu:
				_mainMenuPanel.Visible = true;
				_highScoresPanel.Visible = false;
				_messageLabel.Visible = false;
				_livesLabel.Visible = false;
				break;

			case GamePhase.Start:
				_mainMenuPanel.Visible = false;
				_highScoresPanel.Visible = false;
				_messageLabel.Text = "Press SPACE to start";
				MessageLabelSetDefault();
				_livesLabel.Visible = true;
				break;

			case GamePhase.Playing:
				_mainMenuPanel.Visible = false;
				_highScoresPanel.Visible = false;
				_messageLabel.Visible = false;
				break;

			case GamePhase.GameOver:
				_mainMenuPanel.Visible = false;
				_messageLabel.Text = "GAME OVER";
				MessageLabelSetGameOver();
				_hintLabel.Text = "Press R to restart";
				_hintLabel.Visible = true;
				_backButton.Visible = true;
				_livesLabel.Visible = false;
				ShowHighScoresPanel();
				break;

			case GamePhase.Win:
				_mainMenuPanel.Visible = false;
				_highScoresPanel.Visible = false;
				_messageLabel.Text = "LEVEL CLEARED!\nPress SPACE to continue";
				MessageLabelSetDefault();
				_messageLabel.Modulate = Colors.Green;
				break;
		}
	}

	private void MessageLabelSetDefault()
	{
		_messageLabel.Position = new Vector2(0, 240);
		_messageLabel.Visible = true;
		_messageLabel.Modulate = Colors.White;
	}
	
	private void MessageLabelSetGameOver()
	{
		_messageLabel.Position = new Vector2(0, -310);
		_messageLabel.Visible = true;
		_messageLabel.Modulate = Colors.Red;
	}


	public void OnPaddleInputModeChanged(int modeInt)
	{
		_currentInputMode = (InputMode)modeInt;
		_inputModeLabel.Text = $"Input: {_currentInputMode} (M to toggle)";
		UpdateToggleInputButtonText();
	}

	private void ShowHighScoresPanel()
	{
		int score = GameState.Instance.Score;
		var table = HighScoreTable.Instance;

		if (table.IsHighScore(score))
		{
			_pendingScore = score;
			_newEntryIndex = -1;
			_nameInputBox.Visible = true;
			_nameInput.Text = "";
			_nameInput.GrabFocus();
		}
		else
		{
			_newEntryIndex = -1;
			_pendingScore = 0;
			_nameInputBox.Visible = false;
		}

		UpdateScoresText();
		_highScoresPanel.Visible = true;
	}

	private void OnNameSubmitted(string text)
	{
		_newEntryIndex = HighScoreTable.Instance.AddEntry(text, _pendingScore);
		_nameInputBox.Visible = false;
		_nameInput.ReleaseFocus();
		_pendingScore = 0;

		UpdateScoresText();
		var newHiScore = HighScoreTable.Instance.HighestScore;
		if (newHiScore != GameState.Instance.HiScore)
		{
			GameState.Instance.RefreshHiScoreFromTable();
		}
	}

	private void UpdateScoresText()
	{
		var entries = HighScoreTable.Instance.Entries;
		var sb = new System.Text.StringBuilder();

		for (int i = 0; i < HighScoreTable.MaxEntries; i++)
		{
			string marker = (i == _newEntryIndex) ? " <" : "";
			if (i < entries.Count)
			{
				sb.AppendLine($"{i + 1,2}. {entries[i].Name,-12} {entries[i].Score,6}{marker}");
			}
			else
			{
				sb.AppendLine($"{i + 1,2}. ---          ---");
			}
		}

		_scoresLabel.Text = sb.ToString();
	}

	private void OnViewScoresPressed()
	{
		_viewingScoresFromMenu = true;
		_mainMenuPanel.Visible = false;
		_highScoresPanel.Visible = true;
		_nameInputBox.Visible = false;
		_hintLabel.Visible = false;
		_backButton.Visible = true;
		_topBar.Visible = false;
		_bottomBar.Visible = false;

		UpdateScoresText();
	}

	private void OnBackPressed()
	{
		if (_viewingScoresFromMenu)
		{
			_viewingScoresFromMenu = false;
			_highScoresPanel.Visible = false;
			_mainMenuPanel.Visible = true;
			_topBar.Visible = true;
			_bottomBar.Visible = true;
		}
		else
		{
			EmitSignal(SignalName.BackToMenuRequested);
		}
	}

	public void RefreshScoresDisplay()
	{
		UpdateScoresText();
	}

	public void ShowPauseMenu()
	{
		UpdateToggleInputButtonText();
		_pausePanel.Visible = true;
	}

	public void HidePauseMenu()
	{
		_pausePanel.Visible = false;
	}

	private void UpdateToggleInputButtonText()
	{
		_toggleInputButton.Text = (_currentInputMode == InputMode.Keyboard)
			? "Switch to Mouse"
			: "Switch to Keyboard";
	}

	private void OnBenchmarksPressed()
	{
		_viewingBenchmarksFromMenu = true;
		_mainMenuPanel.Visible = false;
		_benchmarkPanel.Visible = true;
		_topBar.Visible = false;
		_bottomBar.Visible = false;
		_benchmarkResultsLabel.Text = "Press \"Run\" to start.\n\nWarning: large sizes may freeze the game for ~1 second.";
	}

	private void OnBackFromBenchmarkPressed()
	{
		_viewingBenchmarksFromMenu = false;
		_benchmarkPanel.Visible = false;
		_mainMenuPanel.Visible = true;
		_topBar.Visible = true;
		_bottomBar.Visible = true;
	}

	private void OnRunBenchmarkPressed()
	{
		_benchmarkResultsLabel.Text = "Running...";
		CallDeferred(nameof(RunBenchmarksDeferred));
	}

	private void RunBenchmarksDeferred()
	{
		var sb = new System.Text.StringBuilder();

		var lvlResults = Benchmark.RunLevelGeneration();
		sb.Append(FormatResults(
			"LEVEL GENERATION",
			"Theoretical: O(R * C) — linear in cell count",
			lvlResults, 4.0));

		sb.Append('\n');
		sb.Append('\n');

		var nearestResults = Benchmark.RunNearestSearch();
		sb.Append(FormatResults(
			"NEAREST BRICK SEARCH (naive)",
			"Theoretical: O(N) — linear scan",
			nearestResults, 10.0));

		_benchmarkResultsLabel.Text = sb.ToString();
	}

	private string FormatResults(
		string title,
		string theory,
		System.Collections.Generic.List<BenchmarkResult> results,
		double expectedRatio)
	{
		var sb = new System.Text.StringBuilder();
		sb.Append(title).Append('\n');
		sb.Append(theory).Append('\n');
		sb.Append('\n');
		sb.Append($"{"n",10} | {"avg time",12} | {"ratio",7} | expected").Append('\n');
		sb.Append(new string('-', 50)).Append('\n');

		for (int i = 0; i < results.Count; i++)
		{
			string ratio = "  --  ";
			if (i > 0 && results[i - 1].AvgMs > 0)
			{
				double r = results[i].AvgMs / results[i - 1].AvgMs;
				ratio = $"{r,5:F2}x";
			}
			sb.Append($"{results[i].N,10} | {results[i].AvgMs,9:F3} ms | {ratio,7} | {expectedRatio,5:F1}x").Append('\n');
		}
		return sb.ToString();
	}
}

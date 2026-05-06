using Godot;
using System;
using System.Xml.Serialization;

public partial class HUD : CanvasLayer
{
	private Label _scoreLabel;
	private Label _livesLabel;
	private Label _hiScoreLabel;
	private Label _messageLabel;
	private Label _inputModeLabel;

	private Panel _highScoresPanel;
	private Label _scoresLabel;
	private HBoxContainer _nameInputBox;
	private LineEdit _nameInput;
	private int _newEntryIndex = -1;
	private int _pendingScore = 0;
	public bool IsAwaitingName => _newEntryIndex != -1 && _nameInputBox.Visible;

	private int _currentLives;
	private int _currentLevel;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_scoreLabel = GetNode<Label>("TopBar/ScoreLabel");
		_livesLabel = GetNode<Label>("TopBar/LivesLabel");
		_hiScoreLabel = GetNode<Label>("TopBar/HiScoreLabel");
		_messageLabel = GetNode<Label>("MessageLabel");
		_inputModeLabel = GetNode<Label>("InputModeLabel");
		_highScoresPanel = GetNode<Panel>("HighScoresPanel");
		_scoresLabel     = GetNode<Label>("HighScoresPanel/VBox/ScoresLabel");
		_nameInputBox    = GetNode<HBoxContainer>("HighScoresPanel/VBox/NameInputBox");
		_nameInput = GetNode<LineEdit>("HighScoresPanel/VBox/NameInputBox/NameInput");
		
		_nameInput.TextSubmitted += OnNameSubmitted;

		var gs = GameState.Instance;

		gs.ScoreChanged += OnScoreChanged;
		gs.LivesChanged += OnLivesChanged;
		gs.PhaseChanged += OnPhaseChange;
		gs.LevelChanged += OnLevelChanged;

		_currentLives = gs.Lives;
		_currentLevel = gs.CurrentLevel;

		OnLivesChanged(gs.Lives);
		OnScoreChanged(gs.Score, gs.HiScore);
		OnPhaseChange((int)gs.Phase);
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

		switch (phase)
		{
			case GamePhase.Start:
				_messageLabel.Text = "Press SPACE to start";
				_messageLabel.Visible = true;
				_highScoresPanel.Visible = false;
				break;
			case GamePhase.Playing:
				_messageLabel.Visible = false;
				_highScoresPanel.Visible = false;
				break;
			case GamePhase.GameOver:
				_messageLabel.Text = "GAME OVER";
				_messageLabel.Visible = true;
				ShowHighScoresPanel();
				break;
			case GamePhase.Win:
				_messageLabel.Text = "LEVEL CLEARED!\nPress SPACE to continue";
				_messageLabel.Visible = true;
				_highScoresPanel.Visible = false;
				break;
			default:
				break;
		}
	}

	/// <summary>
	/// Вызывается из Main, когда Paddle переключает режим ввода.
	/// </summary>
	public void OnPaddleInputModeChanged(int modeInt)
	{
		var mode = (InputMode)modeInt;
		_inputModeLabel.Text = $"Input: {mode} (M to toggle)";
	}

	private void ShowHighScoresPanel()
	{
		int score = GameState.Instance.Score;
		var table = HighScoreTable.Instance;

		if (table.IsHighScore(score))
		{
			// попал в топ — ждём ввода имени
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
}

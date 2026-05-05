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
			case 3: 
				_livesLabel.Modulate = Colors.Green;
				break;
			default:
				_livesLabel.Modulate = Colors.White;
				break;
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
				break;
			case GamePhase.Playing:
				_messageLabel.Visible = false;
				break;
			case GamePhase.GameOver:
				_messageLabel.Text = "GAME OVER\nPress R to restart";
				_messageLabel.Visible = true;
				break;
			case GamePhase.Win:
				if (GameState.Instance.CurrentLevel == Levels.Count)
				{
					_messageLabel.Text = "ALL LEVELS CLEARED!\nPress R to restart with +20% ball speed";
				}
				else
				{
					_messageLabel.Text = "LEVEL CLEARED!\nPress R to continue";
				}
				_messageLabel.Visible = true;
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
}

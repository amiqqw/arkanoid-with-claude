using Godot;
using System;

public partial class HUD : CanvasLayer
{
	private Label _scoreLabel;
	private Label _livesLabel;
	private Label _hiScoreLabel;
	private Label _messageLabel;
	private Label _inputModeLabel;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_scoreLabel   = GetNode<Label>("TopBar/ScoreLabel");
		_livesLabel   = GetNode<Label>("TopBar/LivesLabel");
		_hiScoreLabel = GetNode<Label>("TopBar/HiScoreLabel");
		_messageLabel = GetNode<Label>("MessageLabel");
		_inputModeLabel = GetNode<Label>("InputModeLabel");

		var gs = GameState.Instance;

		gs.ScoreChanged += OnScoreChanged;
		gs.LivesChanged += OnLivesChanged;
		gs.PhaseChanged += OnPhaseChange;

		OnLivesChanged(gs.Lives);
		OnScoreChanged(gs.Score, gs.HiScore);
		OnPhaseChange((int)gs.Phase);
	}

	private void OnLivesChanged(int lives)
	{
		_livesLabel.Text = $"Lives: {lives}";
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
				_messageLabel.Text = "YOU WIN!\nPress R to continue";
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

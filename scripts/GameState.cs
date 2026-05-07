using Godot;

public partial class GameState : Node
{
    public static GameState Instance { get; private set; }

    [Signal] public delegate void LivesChangedEventHandler(int lives);
    [Signal] public delegate void ScoreChangedEventHandler(int score, int hiScore);
    [Signal] public delegate void GameOverEventHandler();
    [Signal] public delegate void PhaseChangedEventHandler(int newPhase);
    [Signal] public delegate void LevelChangedEventHandler(int level);
    [Signal] public delegate void TimeChangedEventHandler(int seconds);

    public const int StartingLives = 3;
    public const float SpeedIncreasePerLoop = 0.2f;
    private int _startingLevel = 3; //! поменять на 1 перед сдачей
    private const int _maxLives = 5;

    public int ElapsedSeconds { get; private set; }
    private float _timeAccumulator = 0f;

    public int Lives { get; private set; }
    public int Score { get; private set; }
    public int HiScore { get; private set; }
    public GamePhase Phase { get; private set; }
    public int CurrentLevel { get; private set; }
    public float BallSpeedMultiplier { get; private set; }

    public override void _Ready()
    {
        Instance = this;
        Lives = StartingLives;
        Score = 0;
        HiScore = HighScoreTable.Instance.HighestScore;
        Phase = GamePhase.Start;
        CurrentLevel = _startingLevel;
        BallSpeedMultiplier = 1f;
    }

    public override void _Process(double delta)
    {
        if (Phase != GamePhase.Playing) return;

        _timeAccumulator += (float)delta;

        int newSeconds = (int)_timeAccumulator;
        if (newSeconds != ElapsedSeconds)
        {
            ElapsedSeconds = newSeconds;
            EmitSignal(SignalName.TimeChanged, ElapsedSeconds);
        }
    }

    public void ResetTime()
    {
        _timeAccumulator = 0f;
        ElapsedSeconds = 0;
        EmitSignal(SignalName.TimeChanged, 0);
    }


    public void ChangePhase(GamePhase newPhase)
    {
        if (Phase == newPhase) return;
        Phase = newPhase;
        EmitSignal(SignalName.PhaseChanged, (int)newPhase);
    }

    public void AddScore(int amount)
    {
        Score += amount;
        if (Score > HiScore)
            HiScore = Score;

        EmitSignal(SignalName.ScoreChanged, Score, HiScore);
    }

	public void LoseLife()
	{
		Lives -= 1;
		EmitSignal(SignalName.LivesChanged, Lives);

		if (Lives <= 0)
		{
			EmitSignal(SignalName.GameOver);
			ChangePhase(GamePhase.GameOver);
		}
	}
	
	public void AddLife()
	{
		if (Lives < _maxLives)
        {
            Lives += 1;
            EmitSignal(SignalName.LivesChanged, Lives);
        }
	}

    public void ResetLives()
    {
        Lives = StartingLives;
        EmitSignal(SignalName.LivesChanged, Lives);
    }

    public void ResetScore()
    {
        Score = 0;
        EmitSignal(SignalName.ScoreChanged, Score, HiScore);
    }

    public void SetLevel(int level)
    {
        CurrentLevel = level;
        EmitSignal(SignalName.LevelChanged, CurrentLevel);
    }

    public void ResetProgression()
    {
        CurrentLevel = _startingLevel;
        BallSpeedMultiplier = 1f;
        EmitSignal(SignalName.LevelChanged, CurrentLevel);
    }

    public void IncreaseSpeed(float amount)
    {
        BallSpeedMultiplier *= 1 + amount;
    }

    public void RefreshHiScoreFromTable()
    {
        int newHi = HighScoreTable.Instance.HighestScore;
        if (newHi != HiScore)
        {
            HiScore = newHi;
            EmitSignal(SignalName.ScoreChanged, Score, HiScore);
        }
    }
}
using Godot;

public partial class GameState : Node
{
    public static GameState Instance { get; private set; }

    [Signal] public delegate void LivesChangedEventHandler(int lives);
    [Signal] public delegate void ScoreChangedEventHandler(int score, int hiScore);
    [Signal] public delegate void GameOverEventHandler();
    [Signal] public delegate void PhaseChangedEventHandler(int newPhase);
    [Signal] public delegate void LevelChangedEventHandler(int level);

    public const int StartingLives = 3;
    public const float SpeedIncreasePerLoop = 0.2f;   // +20% за каждый цикл уровней

    public int Lives { get; private set; }
    public int Score { get; private set; }
    public int HiScore { get; private set; }
    public GamePhase Phase { get; private set; }
    public int CurrentLevel { get; private set; }     // 1-based для UI
    public float BallSpeedMultiplier { get; private set; }

    public override void _Ready()
    {
        Instance = this;
        Lives = StartingLives;
        Score = 0;
        HiScore = 0;
        Phase = GamePhase.Start;
        CurrentLevel = 1;
        BallSpeedMultiplier = 1f;
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
        CurrentLevel = 1;
        BallSpeedMultiplier = 1f;
        EmitSignal(SignalName.LevelChanged, CurrentLevel);
    }

    public void IncreaseSpeedForNewLoop()
    {
        BallSpeedMultiplier += SpeedIncreasePerLoop;
    }
}
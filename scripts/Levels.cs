using Godot;

// 0 - пусто
// 1, 2, 3 - обычные, цифра - хп
// 10, 20, 30 - с бонусами, первая цифра - хп
// -1 - неразрушимые

public static class Levels
{
    public static readonly int[][,] Layouts =
    [
        new int[,]
        {
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 1, 1, 1, 1, 1, 1, 1, 1 },
            { 1, 1, 1, 1, 1, 1, 1, 1 },
            { 1, 1, 1, 1, 1, 1, 1, 1 },
            { 0, 0, 0, 0, 0, 0, 0, 0 },
        },
        new int[,]
        {
            { 0,  2, 2, 2, 2, 2, 2,  0 },
            { 1,  1,10, 1, 1,10, 1,  1 },
            { 1,  1, 1,-1,-1, 1, 1,  1 },
            { 1,  1, 1,10, 10, 1, 1,  1 },
            { 0,  1, 1, 1, 1, 1, 1,  0 },
        },
        new int[,]
        {
            {-1,  3,  3,  3,  3,  3,  3, -1 },
            { 2,  2, 10,  2,  2, 10,  2,  2 },
            { 1,  1,  1, -1, -1,  1,  1,  1 },
            { 1, 10,  1,  1,  1,  1, 10,  1 },
            { 1,  0,  1, 10, 10,  1,  0,  1 },
        },
    ];

    public static int HandcraftedCount => Layouts.Length;

    private static readonly (int code, float weight)[] BrickWeights =
    [
        (0,   1.5f),   // пусто
        (1,   3.0f),   // 1-удар 
        (2,   1.5f),   // 2-удар
        (3,   1.2f),   // 3-удар 
        (-1,  0.6f),   // неразрушимый 
        (10,  1f),   // бонусный
    ];

    private const int RandomRows = 5;
    private const int RandomCols = 8;

    public static int[,] GenerateRandom()
    {
        var layout = new int[RandomRows, RandomCols];
        for (int row = 0; row < RandomRows; row++)
        {
            for (int col = 0; col < RandomCols; col++)
            {
                layout[row, col] = PickWeightedCode();
            }
        }
        return layout;
    }

    private static int PickWeightedCode()
    {
        float total = 0f;
        foreach (var (_, weight) in BrickWeights)
            total += weight;

        float roll = (float)GD.RandRange(0.0, total);
        float cumulative = 0f;
        foreach (var (code, weight) in BrickWeights)
        {
            cumulative += weight;
            if (roll <= cumulative) return code;
        }
        return 0;
    }
}
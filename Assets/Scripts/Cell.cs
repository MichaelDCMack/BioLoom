public struct Cell
{
    public enum CellStatus
    {
        Empty,
        Normal,
        Collision,
    }

    #region Public Properties

    public CellStatus Status { get; set; }

    public static int Min { get; set; }
    public static int Max { get; set; }

    private int gene;
    public int Gene
    {
        get
        {
            return gene;
        }

        set
        {
            gene = Clamp(value, Min, Max);
        }
    }

    public int Index
    {
        get
        {
            return gene - Min;
        }
    }

    #endregion

    #region Helpers

    public static int Clamp(int value, int min, int max)
    {
        if(value < min)
        {
            return min;
        }
        if(value > max)
        {
            return max;
        }
        return value;
    }

    #endregion
}
using UnityEngine;

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
    public int Gene { get; set; }
    
    #endregion
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexaGrid
{
    #region Variables
    public int xLength;
    public int yLength;
    public int zHeight;
    public double CellSize = 1f; //Distance between neighbouring hexagon centers

    public double[,] valArray;
    #endregion

    #region Constructors
    public HexaGrid(int xSize, int ySize, int zPos)
    {
        xLength = xSize;
        yLength = ySize;
        zHeight = zPos;

        valArray = new double[xSize, ySize];
    }

    public HexaGrid(int xSize, int ySize, int zPos, float CellSize)
    {
        xLength = xSize;
        yLength = ySize;
        zHeight = zPos;
        this.CellSize = CellSize;

        valArray = new double[xSize, ySize];
    }

    public HexaGrid()
    {
        xLength = 0;
        yLength = 0;
        zHeight = 0;
    }
    #endregion

    #region Public Functions
    public void Assign(int xID, int yID, double val)
    {
        valArray[xID, yID] = val;
    }

    public Vector2Int[] GetNeighbours(int xID, int yID)
    {
        Vector2Int[] Neighbours = new Vector2Int[6];

        if (yID % 2 == 0)
        {
            Neighbours[0] = new Vector2Int(xID, yID + 1); //from top right -> clockwise -> to top left
            Neighbours[1] = new Vector2Int(xID + 1, yID);
            Neighbours[2] = new Vector2Int(xID, yID - 1);
            Neighbours[3] = new Vector2Int(xID - 1, yID - 1);
            Neighbours[4] = new Vector2Int(xID - 1, yID);
            Neighbours[5] = new Vector2Int(xID - 1, yID + 1);
        }
        else
        {
            Neighbours[0] = new Vector2Int(xID + 1, yID + 1); //from top right -> clockwise -> to top left
            Neighbours[1] = new Vector2Int(xID + 1, yID);
            Neighbours[2] = new Vector2Int(xID + 1, yID - 1);
            Neighbours[3] = new Vector2Int(xID, yID - 1);
            Neighbours[4] = new Vector2Int(xID - 1, yID);
            Neighbours[5] = new Vector2Int(xID, yID + 1);
        }

        return Neighbours;
    }

    public List<Vector2Int> GetNeighbours(int xID, int yID, List<Vector2Int> ExcludeCells, int xMax, int yMax)
    {
        List<Vector2Int> AllNeighbours = new List<Vector2Int>();
        List<Vector2Int> ValidCells = new List<Vector2Int>();

        if (yID % 2 == 0)
        {
            AllNeighbours.Add(new Vector2Int(xID, yID + 1)); //from top right -> clockwise -> to top left
            AllNeighbours.Add(new Vector2Int(xID + 1, yID));
            AllNeighbours.Add(new Vector2Int(xID, yID - 1));
            AllNeighbours.Add(new Vector2Int(xID - 1, yID - 1));
            AllNeighbours.Add(new Vector2Int(xID - 1, yID));
            AllNeighbours.Add(new Vector2Int(xID - 1, yID + 1));
        }
        else
        {
            AllNeighbours.Add(new Vector2Int(xID + 1, yID + 1)); //from top right -> clockwise -> to top left
            AllNeighbours.Add(new Vector2Int(xID + 1, yID));
            AllNeighbours.Add(new Vector2Int(xID + 1, yID - 1));
            AllNeighbours.Add(new Vector2Int(xID, yID - 1));
            AllNeighbours.Add(new Vector2Int(xID - 1, yID));
            AllNeighbours.Add(new Vector2Int(xID, yID + 1));
        }

        foreach (Vector2Int Cell in AllNeighbours)
        {
            if (!ExcludeCells.Contains(Cell) && Cell.x >= 0 && Cell.y >= 0 && Cell.x < xMax && Cell.y < yMax)
            {
                ValidCells.Add(Cell);
            }
        }

        return ValidCells;
    }

    public void PrintValues()
    {
        for (int x = 0; x < xLength; x++)
            for (int y = 0; y < yLength; y++)
                Debug.Log("TestGrid[" + x + ", " + y + "] has value of " + valArray[x, y]);
    }

    public Vector3 GetWorldPosition(int xID, int yID)
    {
        double yPos = yID * CellSize * Mathf.Sqrt(3) / 2;
        double xPos = (xID + (yID % 2 == 0 ? 0 : 0.5f)) * CellSize;

        return new Vector3((float)xPos, (float)yPos);
    }

    public Vector2Int GetCellID(double xPos, double yPos)
    {
        int yID = Mathf.RoundToInt((float)(yPos * 2 / (CellSize * Mathf.Sqrt(3))));
        int xID = Mathf.RoundToInt((float)((xPos / CellSize) - (yID % 2 == 0 ? 0 : 0.5f)));

        return new Vector2Int(xID, yID);
    }

    public void SetupHexagonalHexaGrid(int GridRadius, out HexaGrid OutputGrid, out List<Vector2Int> InvalidCells)
    {
        OutputGrid = new((GridRadius * 2) + 1, (GridRadius * 2) + 1 + (GridRadius % 2 == 0 ? 0 : 1), 0);
        InvalidCells = new();

        for (int x = 0; x < OutputGrid.xLength; x++)
            for (int y = 0; y < OutputGrid.yLength; y++)
            {
                if (GridRadius % 2 != 0 && y == 0)
                {
                    InvalidCells.Add(new Vector2Int(x, y));
                }
                else if (x < GridRadius)
                {
                    if (y < GridRadius + (GridRadius % 2 == 0 ? 0 : 1))
                    {
                        if (x + y < Mathf.CeilToInt((float)GridRadius / 2) + Mathf.FloorToInt((float)y / 2))
                            InvalidCells.Add(new Vector2Int(x, y));
                        //else
                            //Debug.Log(x + ", " + y);
                    }
                    else
                    {
                        if ((GridRadius * 2) + x - y + (GridRadius % 2 == 0 ? 0 : 2) < Mathf.CeilToInt((float)GridRadius / 2) + Mathf.FloorToInt((float)((GridRadius * 2) - y + (GridRadius % 2 == 0 ? 0 : 2)) / 2))
                            InvalidCells.Add(new Vector2Int(x, y));
                        //else
                            //Debug.Log(x + ", " + y);
                    }
                }
                else
                {
                    if (y < GridRadius + (GridRadius % 2 == 0 ? 0 : 1))
                    {
                        if ((GridRadius * 2) - x + y < Mathf.CeilToInt((float)GridRadius / 2) + Mathf.FloorToInt((float)(y + 1) / 2))
                            InvalidCells.Add(new Vector2Int(x, y));
                        //else
                            //Debug.Log(x + ", " + y);
                    }
                    else
                    {
                        if ((GridRadius * 4) - x - y + (GridRadius % 2 == 0 ? 0 : 1) < Mathf.CeilToInt((float)GridRadius / 2) + Mathf.FloorToInt((float)((GridRadius * 2) - y + 1) / 2))
                            InvalidCells.Add(new Vector2Int(x, y));
                        //else
                            //Debug.Log(x + ", " + y);
                    }
                }
            }
    }
    #endregion
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct int2
{
    public int x;
    public int y;

    public int2(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}


public class GameOfLife : MonoBehaviour
{
    // here will be the origin to clone anytime we want
    public GameObject CellOrigin;
    public float CellSize = 0.5f;
    public float CellOriginalSize = 1f;

    public bool makeCellsStatRandom = false;
    public float ProbabilityOfItAlive = 0.5f;


    public int2 GridSize = new int2(10,20);
    // a grid where we will store our matrice of Cells
    private List<List<GameObject>> Grid;

    // Time Stuff and variables
    public float TimerCount = 2f;
    private float TimerCountLeft;
    public bool TimerIsValid = false;

    // Start is called before the first frame update
    void Start()
    {
        TimerCountLeft = TimerCount;
        InitializeGrid(!makeCellsStatRandom);
    }

    // Update is called once per frame
    void Update()
    {
        if (TimerIsValid)
        {
            if (TimerCountLeft >= 0)
            {
                TimerCountLeft -= Time.deltaTime;
            }
            else
            {
                ChangeToNextStat();
                TimerCountLeft = TimerCount;
            }
        }
    }


    private void ChangeToNextStat()
    {
        for (int i = 0; i < GridSize.x; i++)
        {
            for (int j = 0; j < GridSize.y; j++)
            {
                Grid[i][j].GetComponent<Cell>().isAlive = Grid[i][j].GetComponent<Cell>().rules(CountCellCeighbors(i, j));
            }
        }
        for (int i = 0; i < GridSize.x; i++)
        {
            for (int j = 0; j < GridSize.y; j++)
            {
                Grid[i][j].GetComponent<Cell>().wasAlive = Grid[i][j].GetComponent<Cell>().isAlive;
            }
        }
    }

    // Helper method to handle modulo operation correctly
    int Mod(int k, int n)
    {
        return ((k % n) + n) % n;
    }

    int CountCellCeighbors(int xpos, int ypos)
    {
        int count = 0;
        for (int i = xpos - 1; i <= xpos + 1; i++)
        {
            for (int j = ypos - 1; j <= ypos + 1; j++)
            {
                if (!(i == xpos && j == ypos)) // Skip the cell itself
                {
                    int ni = Mod(i, GridSize.x); 
                    int nj = Mod(j, GridSize.y);
                    if (Grid[ni][nj].GetComponent<Cell>().wasAlive)
                    {
                        count++;
                    }
                }
            }
        }
        return count;
    }

    private void InitializeGrid(bool randomOrNot = false)
    {
        Grid = new List<List<GameObject>>();

        for (int i = 0; i < GridSize.x; i++)
        {
            List<GameObject> row = new List<GameObject>();
            for (int j = 0; j < GridSize.y; j++)
            {
                GameObject cellObject = Instantiate(CellOrigin, new Vector3(i * CellSize + (gameObject.transform.position.x - (GridSize.x * CellSize) / 2), 0, j * CellSize + (gameObject.transform.position.y - (GridSize.y * CellSize) / 2)), Quaternion.identity);
                cellObject.transform.localScale = new Vector3(CellSize/CellOriginalSize, 0.01f, CellSize/ CellOriginalSize);
                // make the parent so it will not bother us in the game engine
                cellObject.gameObject.transform.parent = gameObject.transform;

                cellObject.gameObject.name = "Cell[X: " + i + " ,Y: " + j + " ]";

                cellObject.GetComponent<Cell>().isAlive = false;
                cellObject.GetComponent<Cell>().wasAlive = false;
                if (!randomOrNot)
                {
                    cellObject.GetComponent<Cell>().isAlive = (Random.Range(0, 2) == 0);
                    cellObject.GetComponent<Cell>().wasAlive = cellObject.GetComponent<Cell>().isAlive;
                }
                row.Add(cellObject);
            }
            Grid.Add(row);
        }

        if (randomOrNot)
        {
            SetRandomAliveCells();
        }
    }

    private void SetRandomAliveCells()
    {
        int totalCells = GridSize.y * GridSize.x;
        int aliveCellsCount = Mathf.FloorToInt(totalCells * ProbabilityOfItAlive);

        HashSet<int> selectedIndices = new HashSet<int>();

        while (selectedIndices.Count < aliveCellsCount)
        {
            int randomIndex = Random.Range(0, totalCells);
            if (!selectedIndices.Contains(randomIndex))
            {
                selectedIndices.Add(randomIndex);

                int row = randomIndex / GridSize.y;
                int col = randomIndex % GridSize.y;

                Grid[row][col].GetComponent<Cell>().isAlive = true;
                Grid[row][col].GetComponent<Cell>().wasAlive = true;
            }
        }
    }
}

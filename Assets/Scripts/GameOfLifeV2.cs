using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MyCell<T>
{
    public T isAlive;
    public T wasAlive;

    public MyCell(T stat = default(T))
    {
        wasAlive = isAlive = stat;
    }

    // Optional: You can create a method to set the visibility externally
    public void SetVisibility(T value)
    {
        wasAlive = isAlive = value;
    }
}


[System.Serializable]
public class Rulles
{
    public List<int> Survive;
    public List<int> Born;

    public Rulles()
    {
        Survive = new List<int>();
        Born = new List<int>();
    }
}

public class GameOfLifeV2 : MonoBehaviour
{
    // ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
    public float CellSize = 1f;
    public int CellResolution = 16;
    public int2 GridSize = new int2(10, 20);
    public bool isChanged = true;

    public bool makeCellsStatRandom = false;
    public float ProbabilityOfItAlive = 0.5f;

    // Time Stuff and variables
    public float TimerCount = 2f;
    private float TimerCountLeft;
    public bool TimerIsValid = false;

    // Cet of Rules
    public Rulles SetOfRulles = new Rulles();


    // a grid where we will store our matrice of Cells
    private List<List<MyCell<bool>>> Grid;
    // Ectra variabke cause we have a plan so the 1 in plan is not really one in the world size 
    private int originalSize = 10;

    private Texture2D texture;


    // index stuff ,hover , and other things like adding codintion ect ect;
    private Camera mainCamera; // Reference to the main camera
    private GameObject cursureObject;
    // stuff to check if the position is chnaged of not 
    private int2 OldposHover = new int2(-1,-1);
    private bool oldStatHover = false;

    // functions need to get the index hover
    private void OnMouseOver()
    {
        if (!TimerIsValid)
        {
            Vector2 myIndexHovered = GetIndexFromPosition();
            if (OldposHover.x == -1)
            {
                OldposHover.x = (int)myIndexHovered.x;
                OldposHover.y = (int)myIndexHovered.y;
                // maybe i need to chnage t to wasAlive not the curent ststau
                oldStatHover = Grid[OldposHover.y][OldposHover.x].isAlive;
            }
            if (!(OldposHover.x == (int)myIndexHovered.x && OldposHover.y == (int)myIndexHovered.y))
            {
                DrowCell(OldposHover.x, OldposHover.y, ((oldStatHover) ? Color.white : Color.black));
                OldposHover.x = (int)myIndexHovered.x;
                OldposHover.y = (int)myIndexHovered.y;
                oldStatHover = Grid[OldposHover.y][OldposHover.x].isAlive;
                DrowCell((int)myIndexHovered.x, (int)myIndexHovered.y, Color.green);

                texture.Apply();
                GetComponent<Renderer>().material.mainTexture = texture;
            }
        }
    }

    private void OnMouseExit()
    {
        if (!TimerIsValid)
        {
            DrowCell(OldposHover.x, OldposHover.y, ((oldStatHover) ? Color.white : Color.black));
            OldposHover = new int2(-1, -1);
        }
    }
    private void OnMouseDown()
    {
        if (!TimerIsValid)
        {
            Vector2 myIndexHovered = GetIndexFromPosition();
            Grid[(int)myIndexHovered.y][(int)myIndexHovered.x].SetVisibility(!(Grid[(int)myIndexHovered.y][(int)myIndexHovered.x].isAlive));
            DrowCell((int)myIndexHovered.x, (int)myIndexHovered.y, ((Grid[(int)myIndexHovered.y][(int)myIndexHovered.x].isAlive) ? Color.white : Color.black));
            OldposHover = new int2(-1, -1);
        }
    }

    Vector2 GetIndexFromPosition()
    {
        Vector3 theMousePos = GetPositionHovered();
        return new Vector2((int)(((theMousePos.x + (CellSize * GridSize.x) / 2)) / CellSize), (int)(((theMousePos.z + (CellSize * GridSize.y) / 2)) / CellSize));
    }

    Vector3 GetPositionHovered()
    {
        // Get the mouse position in screen coordinates
        Vector3 mousePosition = Input.mousePosition;

        // Calculate the distance from the camera to the object's plane
        Plane objectPlane = new Plane(Vector3.up, cursureObject.transform.position);

        // Create a ray from the camera to the mouse position
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        Vector3 PositionOuput = new Vector3(0, 0, 0);
        // Calculate the intersection of the ray with the object's plane
        if (objectPlane.Raycast(ray, out float enter))
        {
            Vector3 targetPosition = ray.GetPoint(enter);

            // Move the game object to the target position
            PositionOuput = new Vector3(targetPosition.x, transform.position.y, targetPosition.z) - transform.position;
        }
        return PositionOuput;
    }


    // function return the new stat in the next generation, depend on set of rules . [Finished]
    private bool rules(int neighborsCount,bool isAlive)
    {
        /* [RULES]
             1- Any live cell with fewer than two live neighbours dies, as if by underpopulation.
             2- Any live cell with two or three live neighbours lives on to the next generation.
             3- Any live cell with more than three live neighbours dies, as if by overpopulation.
             4- Any dead cell with exactly three live neighbours becomes a live cell, as if by reproduction.
        */
        bool newStat = false;
        foreach(int nbrStay in SetOfRulles.Survive)
        {
            if (isAlive && neighborsCount == nbrStay)
            {
                newStat = true;
            }
        }
        foreach (int nbrStay in SetOfRulles.Born)
        {
            if (!isAlive && neighborsCount == nbrStay)
            {
                newStat = true;
            }
        }
        return newStat;
    }

    void Start()
    {
        TimerCountLeft = TimerCount;
        InitializeGrid(!makeCellsStatRandom);
        setUpGrid();
        // to get the position index
        cursureObject = new GameObject("MouseTarget");
        if (mainCamera == null)
        {
            mainCamera = Camera.main; // Automatically find the main camera if not assigned
        }
    }

    // function finished and i think the position are correct [Finished]
    private void setUpGrid()
    {
        gameObject.transform.localScale = new Vector3((CellSize * GridSize.x) / originalSize, 1f, (CellSize * GridSize.y) / originalSize);

        // Create a new texture with the specified width and height
        texture = new Texture2D(GridSize.x * CellResolution, GridSize.y * CellResolution);

        // Set alternating colors for each pixel
        for (int x = 0; x < GridSize.x; x++)
        {
            for (int y = 0; y < GridSize.y; y++)
            {
                DrowCell(x, y, Color.black);
                if (Grid[y][x].isAlive)
                {
                    DrowCell(x, y,Color.white);
                    //Debug.Log("Cell alive in position [ " + y + " ][ " + x + " ]");
                }
            }
        }
        texture.Apply();
        GetComponent<Renderer>().material.mainTexture = texture;

    }

    private void DrowAllFromGrid()
    {
        for (int j = 0; j < GridSize.y; j++)
        {
            for (int i = 0; i < GridSize.x; i++)
            {
                DrowCell(i, j, Color.black);
                Grid[j][i].wasAlive = Grid[j][i].isAlive;
                if (Grid[j][i].isAlive)
                {
                    DrowCell(i, j, Color.white);
                }
            }
        }
    }

    // this function is corrected . still need to add code
    private void ChangeToNextStat()
    {
        for (int i = 0; i < GridSize.x; i++)
        {
            for (int j = 0; j < GridSize.y; j++)
            {
                Grid[j][i].isAlive = rules(CountCellCeighbors(i, j), Grid[j][i].isAlive);
            }
        }

        DrowAllFromGrid();
        // do the frst one
        texture.Apply();
        GetComponent<Renderer>().material.mainTexture = texture;
    }

    // function is finished [Finished]
    private void DrowCell(int i,int j, Color theColor)
    {
        for (int k = 0; k < CellResolution; k++)
        {
            for (int w = 0; w < CellResolution; w++)
            {
                texture.SetPixel(i * CellResolution + k, j * CellResolution + w, theColor);
                
            }
        }
    }

    // Helper method to handle modulo operation correctly [Finished]
    int Mod(int k, int n)
    {
        return ((k % n) + n) % n;
    }

    // function that count the nbr of nighber of the cell [Finished]
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
                    if (Grid[nj][ni].wasAlive)
                    {
                        count++;
                    }
                }
            }
        }
        return count;
    }

    // this function i think is almost finished [Finished]
    private void InitializeGrid(bool randomOrNot = false)
    {
        Grid = new List<List<MyCell<bool>>>();

        for (int j = 0; j < GridSize.y; j++)
        {
            List<MyCell<bool>> row = new List<MyCell<bool>>();
            for (int i = 0; i < GridSize.x; i++)
            {
                MyCell<bool> cellObject = new MyCell<bool>(false);

                if (!randomOrNot)
                {
                    cellObject.SetVisibility((Random.Range(0, 2) == 0));
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

    // function [Finished]
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

                // chnage from row to col , col to row
                Grid[col][row].SetVisibility(true);
            }
        }
    }


    // Update is called once per frame
    void Update()
    {
        if (isChanged)
        {
            setUpGrid();
            isChanged = false;
        }
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
        //Debug.Log("nighrbor of osition [ " + tmpPos.y + " ][ " + tmpPos.x + " ] = " + CountCellCeighbors(tmpPos.x, tmpPos.y));
    }
}

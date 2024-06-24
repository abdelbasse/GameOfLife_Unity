using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameOfLifeV3_GPU : MonoBehaviour
{
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

    private RenderTexture renderTexture;
    private Texture2D texture;
    public ComputeShader computeShader;

    // index stuff, hover, and other things like adding condition, etc.
    private Camera mainCamera;
    private GameObject cursureObject;
    // stuff to check if the position is changed or not 
    private int2 OldposHover = new int2(-1, -1);
    private bool oldStatHover = false;

    // function return the new stat in the next generation, depend on set of rules . [Finished]
    private bool rules(int neighborsCount, bool isAlive)
    {
        /* [RULES]
             1- Any live cell with fewer than two live neighbours dies, as if by underpopulation.
             2- Any live cell with two or three live neighbours lives on to the next generation.
             3- Any live cell with more than three live neighbours dies, as if by overpopulation.
             4- Any dead cell with exactly three live neighbours becomes a live cell, as if by reproduction.
        */
        bool newStat = false;
        foreach (int nbrStay in SetOfRulles.Survive)
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


    void Start()
    {
        TimerCountLeft = TimerCount;
        InitializeGrid(!makeCellsStatRandom);
        setUpGrid();

        // to get the position index
        cursureObject = new GameObject("MouseTarget");
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // Initialize the compute shader
        computeShader = Resources.Load<ComputeShader>("GameOfLifeComputeShader");
        if (computeShader == null)
        {
            Debug.LogError("Compute Shader not found!");
            return;
        }
        InitializeRenderTexture();
    }

    void InitializeRenderTexture()
    {
        renderTexture = new RenderTexture(GridSize.x * CellResolution, GridSize.y * CellResolution, 0)
        {
            enableRandomWrite = true
        };
        renderTexture.Create();
    }

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
    }

    private void ChangeToNextStat()
    {
        // Bind the textures and dispatch the compute shader
        computeShader.SetTexture(0, "InputTexture", texture);
        computeShader.SetTexture(0, "Result", renderTexture);
        computeShader.SetInts("GridSize", new int[] { GridSize.x, GridSize.y });
        computeShader.SetInt("CellResolution", CellResolution);

        int threadGroupsX = Mathf.CeilToInt(GridSize.x * CellResolution / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(GridSize.y * CellResolution / 8.0f);
        computeShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        // Copy the result from the render texture to the texture
        RenderTexture.active = renderTexture;
        texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture.Apply();

        GetComponent<Renderer>().material.mainTexture = texture;
    }

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
                    DrowCell(x, y, Color.white);
                }
            }
        }
        texture.Apply();
        GetComponent<Renderer>().material.mainTexture = texture;
    }

    private void DrowCell(int i, int j, Color theColor)
    {
        for (int k = 0; k < CellResolution; k++)
        {
            for (int w = 0; w < CellResolution; w++)
            {
                texture.SetPixel(i * CellResolution + k, j * CellResolution + w, theColor);
            }
        }
    }


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


}

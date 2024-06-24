using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell : MonoBehaviour
{

    public bool isAlive = true;
    public bool wasAlive = true;

    public Material objectMaterial;  // Original material
    public Material hoverMaterial;   // Material for when hovered

    // // This integer variable will control the visibility (0 or 1)
    // public float visibility = 1f;
    private Material originalMaterial;
    private Material clonedMaterial;



    void OnMouseDown()
    {
        isAlive = (isAlive != true);
        wasAlive = isAlive;
    }
    private void OnMouseEnter()
    {
        GetComponent<Renderer>().material = hoverMaterial;
    }

    private void OnMouseExit()
    {
        GetComponent<Renderer>().material = clonedMaterial;
    }

    // Start is called before the first frame update
    void Start()
    {
        originalMaterial = objectMaterial;  // Store original material

        clonedMaterial = new Material(originalMaterial);
        GetComponent<Renderer>().material = clonedMaterial;

        // Update the visibility based on the initial value of 'isAlive'
        UpdateVisibility();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateVisibility();
    }

    // Method to update the visibility based on the 'visibility' variable
    void UpdateVisibility()
    {
        Color color = clonedMaterial.color;
        color.a = 0f;
        if(isAlive)
            color.a = 1f;
        clonedMaterial.color = color;
    }

    // Optional: You can create a method to set the visibility externally
    public void SetVisibility(bool value)
    {
        isAlive = value;
        UpdateVisibility();
    }

    // function return the new stat in the next generation, depend on set of rules .
    public bool rules(int neighborsCount)
    {
        /* [RULES]
             1- Any live cell with fewer than two live neighbours dies, as if by underpopulation.
             2- Any live cell with two or three live neighbours lives on to the next generation.
             3- Any live cell with more than three live neighbours dies, as if by overpopulation.
             4- Any dead cell with exactly three live neighbours becomes a live cell, as if by reproduction.
        */
        return ((isAlive && neighborsCount >= 2 && neighborsCount <= 3) || (!isAlive && neighborsCount == 3));
    }
}

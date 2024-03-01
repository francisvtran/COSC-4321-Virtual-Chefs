using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecipeChange : MonoBehaviour
{

    public bool IsTaken;
    public GameObject recipe;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void IsTakenNow()
    {
        IsTaken = true;
        return;
    }

    public void IsNotTakenNow()
    {
        IsTaken = false;
        return;
    }

    
    public void setRecipe(GameObject newer) 
    {
        recipe = newer;
        return;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

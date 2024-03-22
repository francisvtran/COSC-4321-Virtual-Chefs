using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class RecipeWriting : MonoBehaviour
{

    public TMP_Text text;
    public int tableID;
    string curOrder;
    //public RandomRecipe RecipeManager;
    private void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
        if (tableID == null)
        {
            tableID = 0;
        } 
        //IsTaken = false;
    }
    private void OnEnable()
    {
        //RecipeManager = GetComponent<RandomRecipe>();
        /*
        RandomRecipe.makeBurger += WriteBurger;
        RandomRecipe.makeSalad += WriteSalad;
        RandomRecipe.makeTaco += WriteTaco;
        */
        //RandomRecipe.m_MyEvent.AddListener(WriteRecipe);
        RandomRecipe.recipeMade += WriteRecipe;
        ReadFood.orderGiven += ClearWriting;
    }

    private void OnDisable()
    {
        /*
        RandomRecipe.makeBurger -= WriteBurger;
        RandomRecipe.makeSalad -= WriteSalad;
        RandomRecipe.makeTaco -= WriteTaco;
        */
        //RecipeManager.m_MyEvent.RemoveListener(WriteRecipe);
        RandomRecipe.recipeMade -= WriteRecipe;
        ReadFood.orderGiven -= ClearWriting;
    }
    
    public void WriteRecipe(int n, string c, string t)
    {
        //IsTaken = true;
        if (n == tableID)
        {
            text.text = "<size=12%><b>Table " + n + "</b>\n" + t;
            curOrder = c;
        }
    }

    public void ClearWriting(int n, double s)
    {
        if (n == tableID)
        {
            text.text = "<size=20%>No Order";
            curOrder = "";
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

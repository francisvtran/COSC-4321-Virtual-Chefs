using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/*
[System.Serializable]
public class RecipeMadeEvent : UnityEvent<int, string, string>
{
    
}
*/
public class RandomRecipe : MonoBehaviour
{
    private int returnNum;
    /*
    public delegate void MakeBurger();
    public static event MakeBurger makeBurger;
    public delegate void MakeSalad();
    public static event MakeSalad makeSalad;
    public delegate void MakeTaco();
    public static event MakeTaco makeTaco;
    */

    public bool IsTaken1, IsTaken2, IsTaken3, IsTaken4;


    public delegate void RecipeMadeEvent(int n, string c, string t);
    public static event RecipeMadeEvent recipeMade;
    /*TicketNumber, RecipeCode, RecipeTxt*/

    //public RecipeMadeEvent m_MyEvent;
    //public delegate void Rec
    // Start is called before the first frame update
    private void OnEnable()
    {
        //m_MyEvent.AddListener(RecipeWriting.WriteRecipe);
        //TicketManage.makeRecipe += GenerateRecipe;
        ReadFood.orderGiven += ClearTable;
    }

    private void OnDisable()
    {
        //TicketManage.makeRecipe -= GenerateRecipe;
        ReadFood.orderGiven -= ClearTable;
    }

    private void Start()
    {
        IsTaken1 = false;
        IsTaken2 = false;
        IsTaken3 = false;
        IsTaken4 = false;
        //if (m_MyEvent == null)
        //{
        //m_MyEvent = new RecipeMadeEvent();
        //}
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("r"))
        {
            //recipeMade(1,"","dsdssdf");
            string code = "";
            string text = "";
            int tableNum = 100;
            if (IsTaken4 == false)
            {
                tableNum = 4;
            }
            if (IsTaken3 == false)
            {
                tableNum = 3;
            }
            if (IsTaken2 == false)
            {
                tableNum = 2;
            }
            if (IsTaken1 == false)
            {
                tableNum = 1;
            }
            if (tableNum != 100)
            {
                returnNum = Random.Range(0, 4);
                switch (returnNum)
                {
                    case 0:
                        if (recipeMade != null)
                        {
                            //classic burger
                            code = code + returnNum;
                            text = text + "<size=10%>1 Basic Hamburger:\n";
                            returnNum = Random.Range(0, 2);
                            switch (returnNum)
                            {
                                case 0:
                                    //notToasted
                                    code = code + returnNum;
                                    text = text + "Not Toasted\n";
                                    break;
                                case 1:
                                    //Toasted
                                    code = code + returnNum;
                                    text = text + "Toasted\n";
                                    break;
                            }
                            returnNum = Random.Range(0, 3);
                            switch (returnNum)
                            {
                                case 0:
                                    //rare
                                    code = code + returnNum;
                                    text = text + "Rare\n";
                                    break;
                                case 1:
                                    //medium
                                    code = code + returnNum;
                                    text = text + "Medium\n";
                                    break;

                                case 2:
                                    //welldone
                                    code = code + returnNum;
                                    text = text + "Well Done\n";
                                    break;
                            }
                            //plated
                            code = code + "9991";
                            recipeMade(tableNum, code, text);
                        }
                        break;
                    case 1:
                        if (recipeMade != null)
                        {
                            //CheeseBurger
                            code = code + returnNum;
                            text = text + "<size=10%>1 Cheeseburger:\n";
                            returnNum = Random.Range(0, 2);
                            switch (returnNum)
                            {
                                case 0:
                                    //notToasted
                                    code = code + returnNum;
                                    text = text + "Not Toasted\n";
                                    break;
                                case 1:
                                    //Toasted
                                    code = code + returnNum;
                                    text = text + "Toasted\n";
                                    break;
                            }
                            returnNum = Random.Range(0, 3);
                            switch (returnNum)
                            {
                                case 0:
                                    //rare
                                    code = code + returnNum;
                                    text = text + "Rare\n";
                                    break;
                                case 1:
                                    //medium
                                    code = code + returnNum;
                                    text = text + "Medium\n";
                                    break;

                                case 2:
                                    //welldone
                                    code = code + returnNum;
                                    text = text + "Well Done\n";
                                    break;
                            }
                            //cheese plated
                            text = text + "Cheese\n";
                            code = code + "1991";
                            recipeMade(tableNum, code, text);
                        }
                        break;
                    case 2:
                        if (recipeMade != null)
                        {
                            //Complex Burger
                            code = code + returnNum;
                            text = text + "<size=10%>1 Complex Hamburger:\n";
                            returnNum = Random.Range(0, 2);
                            switch (returnNum)
                            {
                                case 0:
                                    //notToasted
                                    code = code + returnNum;
                                    text = text + "Not Toasted\n";
                                    break;
                                case 1:
                                    //Toasted
                                    code = code + returnNum;
                                    text = text + "Toasted\n";
                                    break;
                            }
                            returnNum = Random.Range(0, 3);
                            switch (returnNum)
                            {
                                case 0:
                                    //rare
                                    code = code + returnNum;
                                    text = text + "Rare\n";
                                    break;
                                case 1:
                                    //medium
                                    code = code + returnNum;
                                    text = text + "Medium\n";
                                    break;

                                case 2:
                                    //welldone
                                    code = code + returnNum;
                                    text = text + "Well Done\n";
                                    break;
                            }
                            returnNum = Random.Range(0, 2);
                            if (returnNum == 1)
                            {
                                //cheese
                                text = text + "Cheese\n";
                                
                            }
                            code = code + returnNum;
                            returnNum = Random.Range(0, 2);
                            if (returnNum == 1)
                            {
                                //tomato
                                text = text + "Tomato\n";
                                
                            }
                            code = code + returnNum;
                            returnNum = Random.Range(0, 2);
                            if (returnNum == 1)
                            {
                                //lettuce
                                text = text + "Lettuce\n";
                                
                            }
                            code = code + returnNum;
                            //plated
                            code = code + "1";
                            recipeMade(tableNum, code, text);
                        }
                        break;
                    case 3:
                        if (recipeMade != null)
                        {
                            //Salad
                            text = "<size=10%>1 Salad:\nCut Tomato\nCut Lettuce\nMixed\n";
                            code = code + returnNum;
                            //Tomato lettuce plated
                            code = code + "999111";
                            recipeMade(tableNum, code, text);
                        }
                        break;
                }
            }

            switch (tableNum)
            {
                case 1:
                    IsTaken1 = true;
                    break;
                case 2:
                    IsTaken2 = true;
                    break;
                case 3:
                    IsTaken3 = true;
                    break;
                case 4:
                    IsTaken4 = true;
                    break;
            }

        }
    }

    void ClearTable(int n, double s)
    {
        if (n == 1)
        {
            IsTaken1 = false;
        }
        if (n == 2)
        {
            IsTaken2 = false;
        }
        if (n == 3)
        {
            IsTaken3 = false;
        }
        if (n == 4)
        {
            IsTaken4 = false;
        }
    }

    void GenerateRecipe()
    {
            //recipeMade(1,"","dsdssdf");
            string code = "";
            string text = "";
            int tableNum = 100;
            if (IsTaken4 == false)
            {
                tableNum = 4;
            }
            if (IsTaken3 == false)
            {
                tableNum = 3;
            }
            if (IsTaken2 == false)
            {
                tableNum = 2;
            }
            if (IsTaken1 == false)
            {
                tableNum = 1;
            }
            if (tableNum != 100)
            {
                returnNum = Random.Range(0, 4);
                switch (returnNum)
                {
                    case 0:
                        if (recipeMade != null)
                        {
                            //classic burger
                            code = code + returnNum;
                            text = text + "<size=10%>1 Basic Hamburger:\n";
                            returnNum = Random.Range(0, 2);
                            switch (returnNum)
                            {
                                case 0:
                                    //notToasted
                                    code = code + returnNum;
                                    text = text + "Not Toasted\n";
                                    break;
                                case 1:
                                    //Toasted
                                    code = code + returnNum;
                                    text = text + "Toasted\n";
                                    break;
                            }
                            returnNum = Random.Range(0, 3);
                            switch (returnNum)
                            {
                                case 0:
                                    //rare
                                    code = code + returnNum;
                                    text = text + "Rare\n";
                                    break;
                                case 1:
                                    //medium
                                    code = code + returnNum;
                                    text = text + "Medium\n";
                                    break;

                                case 2:
                                    //welldone
                                    code = code + returnNum;
                                    text = text + "Well Done\n";
                                    break;
                            }
                            //plated
                            code = code + "9991";
                            recipeMade(tableNum, code, text);
                        }
                        break;
                    case 1:
                        if (recipeMade != null)
                        {
                            //CheeseBurger
                            code = code + returnNum;
                            text = text + "<size=10%>1 Cheeseburger:\n";
                            returnNum = Random.Range(0, 2);
                            switch (returnNum)
                            {
                                case 0:
                                    //notToasted
                                    code = code + returnNum;
                                    text = text + "Not Toasted\n";
                                    break;
                                case 1:
                                    //Toasted
                                    code = code + returnNum;
                                    text = text + "Toasted\n";
                                    break;
                            }
                            returnNum = Random.Range(0, 3);
                            switch (returnNum)
                            {
                                case 0:
                                    //rare
                                    code = code + returnNum;
                                    text = text + "Rare\n";
                                    break;
                                case 1:
                                    //medium
                                    code = code + returnNum;
                                    text = text + "Medium\n";
                                    break;

                                case 2:
                                    //welldone
                                    code = code + returnNum;
                                    text = text + "Well Done\n";
                                    break;
                            }
                            //cheese plated
                            text = text + "Cheese\n";
                            code = code + "1991";
                            recipeMade(tableNum, code, text);
                        }
                        break;
                    case 2:
                        if (recipeMade != null)
                        {
                            //Complex Burger
                            code = code + returnNum;
                            text = text + "<size=10%>1 Complex Hamburger:\n";
                            returnNum = Random.Range(0, 2);
                            switch (returnNum)
                            {
                                case 0:
                                    //notToasted
                                    code = code + returnNum;
                                    text = text + "Not Toasted\n";
                                    break;
                                case 1:
                                    //Toasted
                                    code = code + returnNum;
                                    text = text + "Toasted\n";
                                    break;
                            }
                            returnNum = Random.Range(0, 3);
                            switch (returnNum)
                            {
                                case 0:
                                    //rare
                                    code = code + returnNum;
                                    text = text + "Rare\n";
                                    break;
                                case 1:
                                    //medium
                                    code = code + returnNum;
                                    text = text + "Medium\n";
                                    break;

                                case 2:
                                    //welldone
                                    code = code + returnNum;
                                    text = text + "Well Done\n";
                                    break;
                            }
                            returnNum = Random.Range(0, 2);
                            if (returnNum == 1)
                            {
                                //cheese
                                text = text + "Cheese\n";

                            }
                            code = code + returnNum;
                            returnNum = Random.Range(0, 2);
                            if (returnNum == 1)
                            {
                                //tomato
                                text = text + "Tomato\n";

                            }
                            code = code + returnNum;
                            returnNum = Random.Range(0, 2);
                            if (returnNum == 1)
                            {
                                //lettuce
                                text = text + "Lettuce\n";

                            }
                            code = code + returnNum;
                            //plated
                            code = code + "1";
                            recipeMade(tableNum, code, text);
                        }
                        break;
                    case 3:
                        if (recipeMade != null)
                        {
                            //Salad
                            text = "<size=10%>1 Salad:\nCut Tomato\nCut Lettuce\nMixed\n";
                            code = code + returnNum;
                            //Tomato lettuce plated
                            code = code + "999111";
                            recipeMade(tableNum, code, text);
                        }
                        break;
                }
            }

            switch (tableNum)
            {
                case 1:
                    IsTaken1 = true;
                    break;
                case 2:
                    IsTaken2 = true;
                    break;
                case 3:
                    IsTaken3 = true;
                    break;
                case 4:
                    IsTaken4 = true;
                    break;
            }

        }
}

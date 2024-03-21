using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class ReadFood : MonoBehaviour
{

    public int tableID;
    public string currentOrder;
    public double score;
    public List<GameObject> foodInZone = new List<GameObject>();
    public double timer = 3.0;

    public delegate void OrderGivenEvent(int n, double s);
    public static event OrderGivenEvent orderGiven;

    private void OnEnable()
    {
        RandomRecipe.recipeMade += OrderReceived;
    }

    private void OnDisable()
    {
        RandomRecipe.recipeMade -= OrderReceived;
    }



    // Start is called before the first frame update
    void Start()
    {
        foodInZone.Clear();
        currentOrder = "";
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Plate")
        {
            timer = 3.0;
            foodInZone.Add(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Plate")
        {
            timer = 3.0;
            foodInZone.Remove(other.gameObject);
        }
    }


    public void OrderReceived(int n, string c, string t)
    {
        if (n == tableID)
        {
            currentOrder = c;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (foodInZone.Count > 0)
        {
            timer -= Time.deltaTime;
        }
        if (foodInZone.Count > 0 && timer <= 0 && currentOrder != "")
        {
            
            turnInPlate();
        }
    }

    void turnInPlate ()
    {
        foreach (GameObject obj in foodInZone)
        {
            //GAMEOBJECT.GetComponent<ClassName>().VariableName;
            double correctCount = 0;
            double totalCount = 0;
            //duplicate checkers
            bool Plate = false;
            bool Meat = false;
            bool Cheese = false;
            bool Tomato = false;
            bool TopBun = false;
            bool BotBun = false;
            bool Lettuce = false;
            //List<GameObject> foodCode = obj.GetComponent<Combine>().plate;
            foreach (Food items in obj.GetComponent<Combine>().plate)
            {
                if (items.tag == "Plate")
                {
                    //ignore
                    print("hello");
                    correctCount++;
                    totalCount++;
                }
                if (items.tag == "CookedMeat")
                {
                    if (currentOrder[2] != '9' && Meat != true)
                    {
                        correctCount++;
                        totalCount++;
                        Meat = true;
                    } 
                    else
                    {
                        totalCount++;
                    }
                }
                if (items.tag == "TopBun")
                {
                    if (TopBun != true && currentOrder[0] != 3) {
                        correctCount++;
                        totalCount++;
                        TopBun = true;
                    } else
                    {
                        totalCount++;
                    }
                }
                if (items.tag == "BottomBun")
                {
                    if (BotBun != true && currentOrder[0] != 3)
                    {
                        correctCount++;
                        totalCount++;
                        BotBun = true;
                    }
                    else
                    {
                        totalCount++;
                    }
                }
                if (items.tag == "TomatoSlice")
                {
                    if (currentOrder[4] != '9' && currentOrder[4] != '0' && Tomato != true)
                    {
                        correctCount++;
                        totalCount++;
                        Tomato = true;
                    } 
                    else
                    {
                        totalCount++;
                    }
                }
                if (items.tag == "CheeseSlice")
                {
                    if (currentOrder[3] != '9' && currentOrder[3] != '0' && Cheese != true)
                    {
                        correctCount++;
                        totalCount++;
                        Tomato = true;
                    }
                    else
                    {
                        totalCount++;
                    }
                }
                if (items.tag == "LettuceSlice")
                {
                    if (currentOrder[5] != '9' && currentOrder[5] != '0' && Lettuce != true)
                    {
                        correctCount++;
                        totalCount++;
                        Tomato = true;
                    }
                    else
                    {
                        totalCount++;
                    }
                }
            }
            if (Plate == false) 
            {
                totalCount++;
            }
            if (currentOrder[1] != '9' && TopBun == false)
            {
                totalCount++;
            }
            if (currentOrder[1] != '9' && BotBun == false)
            {
                totalCount++;
            }
            if (currentOrder[4] != '9' && currentOrder[4] != '0' && Tomato != false)
            {
                totalCount++;
            }
            if (currentOrder[3] != '9' && currentOrder[3] != '0' && Cheese != false)
            {
                totalCount++;
            }
            if (currentOrder[2] != '9' && Meat != false)
            {
                totalCount++;
            }
            if (currentOrder[5] != '9' && currentOrder[5] != '0' && Lettuce != false)
            {
                totalCount++;
            }
            score = correctCount / totalCount * 100;
            print(correctCount);
            print(totalCount);
            print(score);
            orderGiven(tableID, score);
            currentOrder = "";
            foreach (Food items in obj.GetComponent<Combine>().plate)
            {
                if (items.tag != "Plate")
                {
                    Destroy(items.item);
                }
            }
            Destroy(obj);
            foodInZone.Remove(obj);
            break;
        }
    }
}
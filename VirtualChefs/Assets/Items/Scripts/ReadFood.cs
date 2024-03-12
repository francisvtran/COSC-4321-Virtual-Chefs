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
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Food")
        {
            foodInZone.Add(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Food")
        {
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
        if (Input.GetKeyDown("f"))
        {
            foreach (GameObject obj in foodInZone)
            {
                //GAMEOBJECT.GetComponent<ClassName>().VariableName;
                string foodCode = obj.GetComponent<FoodStorage>().FoodCode;
                if (currentOrder == foodCode)
                {
                    score = 45.98;
                    print(score);
                    orderGiven(tableID, score);
                    currentOrder = "";
                    Destroy(obj);
                    foodInZone.Remove(obj);
                    break;
                }
            }
        }
    }
}

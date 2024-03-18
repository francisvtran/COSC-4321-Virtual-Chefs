using Oculus.Interaction.Grab;
using Oculus.Interaction.HandGrab;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public struct Food
{
    public GameObject item;
    public string tag;
    public float height;
    public int priority;

    public Food(GameObject food)
    {
        item = food;
        tag = food.tag;

        // Get height of food
        MeshRenderer renderer = food.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            height = renderer.bounds.size.y; // Get height of food
        }
        else
        {
            height = food.transform.localScale.y;
        }

        priority = -1;
        priority = getPriority();
    }
    int getPriority()
    {
        int p = -1;

        if      (tag == "Plate")        { p = 0; }
        else if (tag == "BottomBun")    { p = 1; }
        else if (tag == "LettuceSlice") { p = 2; }
        else if (tag == "CheeseSlice")  { p = 3; }
        else if (tag == "CookedMeat")   { p = 4; }
        else if (tag == "TomatoSlice")  { p = 5; }
        else if (tag == "TopBun")       { p = 6; }

        return p;
    }
}

public class Combine : MonoBehaviour
{
    [SerializeField] List<Food> plate = new List<Food>();
    //[SerializeField] float positionPointer;
    public int size;

    void Start()
    {
        Food food = new Food(this.gameObject);
        plate.Add(food);
        //positionPointer = this.gameObject.transform.localPosition.y;
        size = 1;
    }

    // To check if an  item already exists in the plate so that duplicates aren't possible
    bool inPlateAlready(string foodTag)
    {
        bool found = false;
        // Do item search here
        foreach (Food item in plate)
        {
            if (item.tag == foodTag)
            {
                 found = true;
                 break;
            }
        }
        return found;
    }

    void sortFood()
    {

    }

    void placeFood(Food food, int insertIndex)
    {
        Debug.Log("Type: " + food.tag);
        Debug.Log("Priority: " + food.priority);
        Debug.Log("Height: " + food.height);
        Debug.Log("Insert Index: " + insertIndex);
        float foodPosition = 0f;

        // Get total height of food items that will be below new food item
        for (int i = 0; i < insertIndex; i++) {
            foodPosition += plate[i].height;
        }

        // Update heights for food items above newly placed food
        for (int i = insertIndex; i < size; i++)
        {
            Food ingredient = plate[i];
            Vector3 newPosition = ingredient.item.transform.localPosition;
            newPosition.y += food.height;

            ingredient.item.transform.localPosition = newPosition;
        }

        food.item.transform.SetParent(transform); // Plate becomes parent to food object [plate movements will also move all children together]
        food.item.GetComponent<Rigidbody>().isKinematic = true; // Prevent unwanted physics to occur on child food items
        food.item.GetComponent<Collider>().isTrigger = false; // Prevent triggers from constantly occurring for children in the plate

        // Removes ability to grab food object [may want to change to be able to get food back]
        HandGrabInteractable grabInteractable = food.item.GetComponent<HandGrabInteractable>();
        if (grabInteractable != null)
        {
            // None as grabtype => can't pickup food (but you can pickup the plate)
            grabInteractable.InjectSupportedGrabTypes(GrabTypeFlags.None);
        }

        food.item.transform.localPosition = new Vector3(0, foodPosition, 0); // Place food exactly where it needs to be
        food.item.transform.localRotation = Quaternion.identity; // Reset rotation to (0, 0, 0)
    }

    // Called when an item collides with plate
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("In trigger!");
        Food food = new Food(other.gameObject);
        // If food is not a combinable object or food already exists on the plate, we do not add it to the plate
        if (food.priority < 0 || inPlateAlready(food.tag))
        {
            return;
        }

        // Push new food to list, and sort list based on their priority (ascending)
        bool foundIndex = false;
        int index = 0;
        //Debug.Log("Priority of  " + food.tag + ": [" + food.priority + "]");
        while (!foundIndex)
        {
            int currentP = plate[index].priority;
            //Debug.Log("Current " + plate[index].tag + ": [" + plate[index].priority + "]");
            if (food.priority <= currentP)
            {
                foundIndex = true;
                //Debug.Log("Index: " + index);
                break;
            }
            //Debug.Log("++++++++++++++++++++++++++++++++++++++++++");
            index++;
            if (index >= size)
            {
                foundIndex = true;
                //Debug.Log("Index: " + index);
            }
        }
        plate.Insert(index, food);
        size++;

        // Physically place food onto plate:
        placeFood(food, index);

        // If sort did not change, we don't need to "restack" the plate [MAY NOT NEED SORT IF DOING INSERTS AS ABOVE]
        // Else, stack the items onto the plate 


        if (other.gameObject.CompareTag("Knife"))
        {

        }
    }
}

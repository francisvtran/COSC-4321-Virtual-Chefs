using Oculus.Interaction;
using Oculus.Interaction.Grab;
using Oculus.Interaction.HandGrab;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

// Food struct containing information about a specific food item or plate
public struct Food
{
    public GameObject item;
    public string tag;
    public float height;
    public int priority;

    public Food(GameObject food, float[] foodHeights)
    {
        item = food;
        tag = food.tag;

        // Need to initialize all struct data members before calling getPriority(), else compiler errors emerge
        priority = -1;
        height = -1;

        priority = getPriority();
        if (priority != -1)
        {
            height = foodHeights[priority];
        }
    }
    // Returns priority of food to be used when identifying where a newly inserted food should go in the food stack (lower p => lower on plate)
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
    private HandGrabInteractor handGrab; // Reference to the HandGrabInteractor component

    public List<Food> plate = new List<Food>();
    public float[] foodHeights;
    public bool[] foods;
    public int size;

    void Start()
    {
        initializeHeights();
        initializeFoods();
        Food food = new Food(this.gameObject, foodHeights);
        plate.Add(food);
        size = 1;

    }

    // Sets heights appropriate for food items (numbers gotten through manual height readings via cube object scale)
    void initializeHeights()
    {
        foodHeights = new float[7];
        foodHeights[0] = 0.0350f;    // Plate
        foodHeights[1] = 0.0190f;    // Bottom Bun .0290f
        foodHeights[2] = 0.0060f;    // Lettuce
        foodHeights[3] = 0.0185f;    // Cheese
        foodHeights[4] = 0.0270f;    // Meat
        foodHeights[5] = 0.0090f;    // Tomato
        foodHeights[6] = 0.0841f;    // Top Bun
    }

    // Second catch to ensure dupes truly dont exist
    void initializeFoods()
    {
        foods = new bool[7];
        foods[0] =  true;    // Plate
        foods[1] = false;    // Bottom Bun
        foods[2] = false;    // Lettuce
        foods[3] = false;    // Cheese
        foods[4] = false;    // Meat
        foods[5] = false;    // Tomato
        foods[6] = false;    // Top Bun
    }

    // To check if an  item already exists in the plate so that duplicates aren't possible
    bool inPlateAlready(string foodTag)
    {
        bool found = false;
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

    void placeFood(Food food, int insertIndex)
    {
        float foodPosition = 0f;

        // Get total height of food items that will be below new food item
        for (int i = 0; i < insertIndex; i++)
        {
            foodPosition += plate[i].height;
        }

        // Update heights for food items above newly placed food
        for (int i = insertIndex + 1; i < size; i++)
        {
            Food ingredient = plate[i];
            Vector3 newPosition = ingredient.item.transform.localPosition;
            newPosition.y += food.height;

            ingredient.item.transform.localPosition = newPosition;
        }

        food.item.transform.SetParent(transform);               // Plate becomes parent to food object [so that when the plate moves, all children move together]
        food.item.GetComponent<Rigidbody>().isKinematic = true; // Prevent unwanted physics to occur on child food items
        food.item.GetComponent<Collider>().isTrigger = false;   // Prevent triggers from constantly occurring for children in the plate

        // Removes ability to grab food object [may want to change to be able to get food back]
        HandGrabInteractable grabInteractable = food.item.GetComponent<HandGrabInteractable>();
        if (grabInteractable != null)
        {
            // None as grabtype => can't pickup food (but you can pickup the plate)
            grabInteractable.InjectSupportedGrabTypes(GrabTypeFlags.None);
        }

        food.item.transform.localRotation = Quaternion.identity;                                 // Reset rotation to (0, 0, 0)
        food.item.transform.localPosition = new Vector3(0, foodPosition + (food.height / 4), 0); // Place food exactly where it needs to be
    }


    // Called when an item collides with plate
    private void OnTriggerEnter(Collider other)
    {
        Food food = new Food(other.gameObject, foodHeights);
        if (food.priority < 0 || inPlateAlready(food.tag))
        {
            return;
        }

        ObjectGrabCheck grabCheck = other.GetComponent<ObjectGrabCheck>();
        if (grabCheck != null && grabCheck.IsBeingHeld())
        {
            StartCoroutine(WaitForReleaseAndAdd(food, other));
            return;
        }

        // Object is not being held, but plate is
        // Doubly ensure no duplicates exist
        if (foods[food.priority] == true)
        {
            return;
        }
        foods[food.priority] = true;

        // Find index of where food item should go based on its priority and already placed items
        bool foundIndex = false;
        int index = 0;
        while (!foundIndex)
        {
            int currentP = plate[index].priority;
            if (food.priority <= currentP)
            {
                foundIndex = true;
                break;
            }
            index++;
            if (index >= size)
            {
                foundIndex = true;
            }
        }
        plate.Insert(index, food);
        size++;

        // Physically place food onto plate:
        placeFood(food, index);
    }

    private IEnumerator WaitForReleaseAndAdd(Food food, Collider other)
    {
        ObjectGrabCheck grabCheck = other.GetComponent<ObjectGrabCheck>();

        while (grabCheck.IsBeingHeld())
        {
            yield return null; // Wait for the next frame
        }

        // Check if the object is still within the collision area of the plate
        if (other.gameObject.GetComponent<Collider>().bounds.Intersects(GetComponent<Collider>().bounds))
        {

            // Food already physically placed
            if (foods[food.priority] == true) {
                yield break;
            }
            foods[food.priority] = true;

            // Find index of where food item should go based on its priority and already placed items
            bool foundIndex = false;
            int index = 0;
            while (!foundIndex)
            {
                int currentP = plate[index].priority;
                if (food.priority <= currentP)
                {
                    foundIndex = true;
                    break;
                }
                index++;
                if (index >= size)
                {
                    foundIndex = true;
                }
            }

            plate.Insert(index, food);
            size++;

            // Physically place food onto plate:
            placeFood(food, index);
        }
    }
}

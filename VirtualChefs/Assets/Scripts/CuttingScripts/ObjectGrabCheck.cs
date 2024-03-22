using Oculus.Interaction.Grab;
using UnityEngine;
using System.Linq;
using Oculus.Interaction.HandGrab;

public class ObjectGrabCheck : MonoBehaviour
{
    private HandGrabInteractable handGrabInteractable;
    private bool isGrabbed;

    void Start()
    {
        handGrabInteractable = GetComponent<HandGrabInteractable>();
        isGrabbed = handGrabInteractable.Interactors.Any();
    }

    void Update()
    {
        bool currentlyGrabbed = handGrabInteractable.Interactors.Any();
        if (currentlyGrabbed != isGrabbed)
        {
            isGrabbed = currentlyGrabbed;
        }
    }

    public bool IsBeingHeld()
    {
        return isGrabbed;
    }
}

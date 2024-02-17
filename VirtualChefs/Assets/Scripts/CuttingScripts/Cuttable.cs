using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Cuttable : MonoBehaviour
{
    [SerializeField] CountDisplay countDisplay;

    public TextMeshProUGUI cutText;
    public int cutProgress;
    public int cutGoal;
    public bool cut;

    // Start is called before the first frame update
    void Start()
    {
        cutProgress = 0;
        cutGoal = 5;
        cut = false;

        setCountText();
        countDisplay.ShowMessage(cutText.text);
    }

    void setCountText()
    {
        int cutsLeft = cutGoal - cutProgress;
        if (cutsLeft > 0)
        {
            cutText.text = "Cuts Left: " + cutsLeft.ToString();
        }

        else
        {
            cutText.text = "Object fully cut!";
        }
    }

    void fullyCut()
    {
        cut = true;
        // Modifies the text of UI to show cut completion
        

        // Get position of this game object, create an instance of cut version of object, hide this game object and replace object with cut version

        // Gets object position

        // Creates instance of cut version

        // Hides original object
        //other.gameObject.SetActive(false);

        // Places cut version of object on original position
    }

    void FixedUpdate()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        // If mesh box collides with knife call cut
        if (other.gameObject.CompareTag("Knife") && !cut)
        {
            cutProgress++;
            if (cutProgress >= cutGoal)
            {
                setCountText();
                fullyCut();
            }
            else
            {
                setCountText();
            }
        }
    }
}

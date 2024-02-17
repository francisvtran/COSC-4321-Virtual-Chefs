using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class CountDisplay : MonoBehaviour
{
    public GameObject holder;
    public TextMeshProUGUI cutsRemaining;
    public void ShowMessage(string message)
    {
        if (!holder.activeSelf)
        {
            holder.SetActive(true);
        }
        cutsRemaining.text = message;
    }

    public void ReplaceMessage(string message)
    {
        // False to true might fix changing the message?
        holder.SetActive(false);
        holder.SetActive(true);
        cutsRemaining.text = message;
    }

    public void Hide()
    {
        holder.SetActive(false);
    }

}
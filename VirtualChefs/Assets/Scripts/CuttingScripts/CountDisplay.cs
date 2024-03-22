using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class CountDisplay : MonoBehaviour
{
    public GameObject holder;
    public TextMeshProUGUI cutsRemaining;
    public string messageString;
    public void ShowMessage(string message)
    {
        if (!holder.activeSelf)
        {
            holder.SetActive(true);
        }
        cutsRemaining.text = message;
        messageString = message;
    }

    public void Hide()
    {
        holder.SetActive(false);
    }

}
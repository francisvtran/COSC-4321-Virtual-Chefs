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

    public void Hide()
    {
        holder.SetActive(false);
    }

}
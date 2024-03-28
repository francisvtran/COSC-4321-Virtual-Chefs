using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScoreDisplay : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    public double totalScore;
    public int orders;

    private void Start()
    {
        totalScore = 0.0f;
        orders = 0;
    }

    void OnEnable()
    {
        ReadFood.orderGiven += UpdateScoreDisplay;
    }

    void OnDisable()
    {
        ReadFood.orderGiven -= UpdateScoreDisplay;
    }

    void UpdateScoreDisplay(int tableID, double score)
    {
        totalScore = ((totalScore * orders) + score) / (orders + 1);
        orders++;
        scoreText.text = "Score: " + score.ToString("F2");
    }
}

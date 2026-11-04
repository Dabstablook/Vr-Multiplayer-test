// ScoreDisplay.cs, put it on the text object
using TMPro;
using UnityEngine;

public class ScoreDisplay : MonoBehaviour
{
    public TMP_Text text;

    void Update()
    {
        var r = RhythmNetwork.Instance;
        if (r == null || !r.IsSpawned) return;
        text.text = $"TEAM SCORE {r.teamScore.Value}\nCOMBO {r.combo.Value}   MISSES {r.misses.Value}";
    }
}
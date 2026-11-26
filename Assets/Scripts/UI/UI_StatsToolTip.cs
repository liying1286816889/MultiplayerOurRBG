using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UI_StatsToolTip : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI description;

    public void ShowStatsToolTip(string _text)
    {
        description.text = _text;
        gameObject.SetActive(true);
    }

    public void HideStatsToolTip()
    {
        description.text = "";
        gameObject.SetActive(false);
    }




}

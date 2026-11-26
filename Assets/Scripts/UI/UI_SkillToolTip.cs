using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UI_SkillToolTip : UI_ToolTip
{
    [SerializeField] private TextMeshProUGUI skillDescription;
    [SerializeField] private TextMeshProUGUI skillName;
    [SerializeField] private TextMeshProUGUI skillCost;

    public void ShowToolTip(string _skillDescription,string _skillname,string _skillCost)
    {
        skillDescription.text = _skillDescription;
        skillName.text ="技能:"+"\n "+ _skillname;
        skillCost.text = "花费: " + _skillCost;

        AdjustPosition();
        gameObject.SetActive(true);
    }


    public void HideToolTip() 
    {
        gameObject.SetActive(false);
    }

}

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UI_ItemToolTip : UI_ToolTip
{
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemTypeText;
    [SerializeField] private TextMeshProUGUI itemDescription;

    void Start()
    {

    }

    public void ShowToolTip(ItemData_Equipment item)
    {
        if(item==null)
        {
            return;
        }
        itemNameText.text = "Name:\n"+item.itemName; 
        itemTypeText.text ="Type:"+ item.equipmentType.ToString();
        itemDescription.text = item.GetDescription();
        AdjustPosition();
        gameObject.SetActive(true);
    }

    


    public void HideToolTip()
    {
        gameObject.SetActive(false);
    }


}

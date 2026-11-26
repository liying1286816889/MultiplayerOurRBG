using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerItemDrop : ItemDrop
{
    [Header("Íæ¼ÒµôÂä")]
    [Range(0, 100)]
    [SerializeField] private float chanceToLooseEquipment;
    [Range(0, 100)]
    [SerializeField] private float chanceToLooseMaterial;



    public override void GenerateDrop()
    {
        Inventory inventory= Inventory.instance;

        List<InventoryItem> currentEquipment=inventory.GetEquipmentList();
        for (int i =  currentEquipment.Count; i > 0; i--)
        {
            InventoryItem item = currentEquipment[0];
            if (Random.Range(0f, 100f) <= chanceToLooseEquipment)
            {
                DropItem(item.data);
                inventory.UnEquipItem(item.data as ItemData_Equipment);
            }
        }
        


        List<InventoryItem> currentMaterial = inventory.GetStashList();
        for (int i = currentMaterial.Count; i >0; i--)
        {
            InventoryItem item = currentMaterial[i - 1];
            if (Random.Range(0f, 100f) <= chanceToLooseMaterial)
            {
                DropItem(item.data);
                inventory.RemoveItem(item.data);
            }
        }




    }

}

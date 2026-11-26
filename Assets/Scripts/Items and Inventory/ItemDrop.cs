using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemDrop : MonoBehaviour
{

    [SerializeField] private int possibleItemDrop;
    [SerializeField] private ItemData[] possibleDrops;
    private List<ItemData> dropList= new List<ItemData>();

    [SerializeField] private GameObject dropPrefab;
    
    public virtual void GenerateDrop()
    {
        for (int i = 0; i < possibleDrops.Length; i++)
        {
            if (Random.Range(0, 100) <= possibleDrops[i].dropChance)
            {
                dropList.Add(possibleDrops[i]);
            }
        }

        for (int i = 0; i < possibleItemDrop; i++)
        {
            if(dropList.Count == 0) { return; } 

            ItemData randomitem = dropList[Random.Range(0, dropList.Count-1)];
            dropList.Remove(randomitem);
            DropItem(randomitem);

        }
    }

    

    public void DropItem(ItemData _itemData)
    {
        if (dropPrefab == null) return;

        // 掉落物一律【本地生成】：每个客户端自己掉自己的一份，
        // 互不干扰，也不会再触发 NetworkObject 的销毁报错。
        Vector2 randomVelocity = new Vector2(Random.Range(-5f, 5f), Random.Range(15f, 20f));

        GameObject newDrop = Instantiate(dropPrefab, transform.position, Quaternion.identity);
        ItemObject itemObject = newDrop.GetComponent<ItemObject>();
        if (itemObject == null) return;

        itemObject.SteUpItem(_itemData, randomVelocity);
    }

}

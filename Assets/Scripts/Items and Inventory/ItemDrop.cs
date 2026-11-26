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
        if (NetGame.IsOnline && !NetGame.IsServer)
        {
            // 联机时只有服务器掷骰子并生成掉落物，其它客户端等同步
            return;
        }

        Vector2 randomVelocity = new Vector2(Random.Range(-5f, 5f), Random.Range(15f, 20f));

        GameObject newDrop = NetSpawn.Spawn(dropPrefab, transform.position, Quaternion.identity);
        if (newDrop == null)
        {
            return;
        }

        ItemObject itemObject = newDrop.GetComponent<ItemObject>();
        if (itemObject == null)
        {
            return;
        }

        itemObject.SteUpItem(_itemData, randomVelocity);

        // 联机：把“这是什么道具”同步给其它玩家
        NetItemObject netItem = newDrop.GetComponent<NetItemObject>();
        if (netItem != null)
        {
            netItem.ServerSetItem(_itemData);
        }
    }

}

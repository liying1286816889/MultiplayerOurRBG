using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemObject : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] public ItemData itemData;


    


    private void SetUpVisuals()
    {
        if (itemData == null) return;

        GetComponent<SpriteRenderer>().sprite = itemData.icon;
        gameObject.name = "ItemObject_" + itemData.itemName;
    }

    private void Update()
    {

    }

    public void SteUpItem(ItemData _itemData,Vector2 _velocity)
    {

        itemData = _itemData;
        rb.velocity = _velocity;

        SetUpVisuals();
    }



    public void PickUpItem()
    {
        if (!Inventory.instance.CanAddItem() && itemData.ItemType == ItemType.Equipment)
        {
            rb.velocity = new Vector2(0, 7);
            if (PlayerManager.instance != null && PlayerManager.instance.PlayerOrDefault != null)
            {
                PlayerManager.instance.PlayerOrDefault.fx.CreatePopUpText("没有多余空间");
            }
            return;
        }

        AudioManager.instance.PlaySFX(18,transform);
        Inventory.instance.AddItem(itemData);
        // 联机：背包是每个人自己的，但世界里的这个物体要让所有人都看不到
        NetWorld.Despawn(gameObject);
    }
}

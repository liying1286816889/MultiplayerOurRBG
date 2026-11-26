using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Inventory : MonoBehaviour,ISaveManager
{
    //地址，单例模式,背包唯一
    public static Inventory instance;

    public List<ItemData> startingitem;

    public List<InventoryItem> equipment;
    public Dictionary<ItemData_Equipment, InventoryItem> equipmentDictionary;


    public List<InventoryItem> inventory;

    public Dictionary<ItemData, InventoryItem> inventoryDictionary;


    public List <InventoryItem> stash;
    public Dictionary <ItemData, InventoryItem> stashDictionary; 


    [Header("Inventory UI")]
    [SerializeField] private Transform inventorySlotParent;
    [SerializeField] private Transform stashSlotParent;
    [SerializeField] private Transform equipmentSlotParent;
    [SerializeField] private Transform statSlotParent;



    private UI_ItemSlot[] inventoryItemSlot;
    private UI_ItemSlot[] stashItemSlot;
    private UI_EquipmentSlot[] equipmentSlot;
    private UI_StatsSlot[]  statsSlot;

    [Header("Item Cooldown")]
    private float lastTimeUsedFlask;

    [SerializeField]public float flaskCooldown { get;private set; }


    [Header("Data base")]
    public List<ItemData> itemDataBase; 
    public List<InventoryItem> loadItems;
    public List<ItemData_Equipment> loadEquipment;


    private void Awake()
    {
        if(instance == null)
        {
            instance = this;

        }
        else
        {
            Destroy(gameObject);
        }

    }

    private void Start()
    {
        inventory = new List<InventoryItem>();
        inventoryDictionary = new Dictionary<ItemData, InventoryItem>();

        stash = new List<InventoryItem>();
        stashDictionary = new Dictionary<ItemData, InventoryItem>();

        equipment = new List<InventoryItem>();
        equipmentDictionary = new Dictionary<ItemData_Equipment, InventoryItem>();



        inventoryItemSlot = inventorySlotParent.GetComponentsInChildren<UI_ItemSlot>();
        stashItemSlot = stashSlotParent.GetComponentsInChildren<UI_ItemSlot>();
        equipmentSlot = equipmentSlotParent.GetComponentsInChildren<UI_EquipmentSlot>();
        statsSlot = statSlotParent.GetComponentsInChildren<UI_StatsSlot>();

        Invoke("AddStartingItem", .1f);

    }

    private void AddStartingItem()
    {

        foreach (ItemData_Equipment item in loadEquipment)
        {
           EquipItem(item);

        }



        if(loadItems.Count > 0)
        {
            foreach(InventoryItem item in loadItems)
            {
                for(int i = 0; i < item.stackSize; i++)
                {
                    AddItem(item.data);
                }

            }
            return;
        }


        for (int i = 0; i < startingitem.Count; i++)
        {
            if (startingitem[i] != null)
            {
                AddItem(startingitem[i]);
            }
            
        }
    }

    public void EquipItem(ItemData _item)
    {
        ItemData_Equipment newEquipment = _item as ItemData_Equipment;
        InventoryItem newItem = new InventoryItem(newEquipment);

        ItemData_Equipment oldEquipment = null;


        foreach (KeyValuePair<ItemData_Equipment, InventoryItem> item in equipmentDictionary)
        {
            if (item.Key.equipmentType == newEquipment.equipmentType)
            {
                oldEquipment = item.Key;
            }
        }

        if (oldEquipment != null)
        {
            UnEquipItem(oldEquipment);
            AddItem(oldEquipment);
        }

        equipment.Add(newItem);
        equipmentDictionary.Add(newEquipment, newItem);

        newEquipment.AddModifiers();

        RemoveItem(_item);
        UpdateSlotUI();

    }

    public void UnEquipItem(ItemData_Equipment ItemToRemove)
    {
        if(ItemToRemove == null) return;

        if (equipmentDictionary.TryGetValue(ItemToRemove, out InventoryItem value))
        {
            //AddItem(ItemToRemove);
            equipment.Remove(value);
            equipmentDictionary.Remove(ItemToRemove);
            ItemToRemove.RemoveModifiers();
        }
    }

    private void UpdateSlotUI()
    {
        for (int i = 0; i < equipmentSlot.Length; i++)
        {
            foreach (KeyValuePair<ItemData_Equipment, InventoryItem> item in equipmentDictionary)
            {
                if (item.Key.equipmentType == equipmentSlot[i].slotType)
                {
                    equipmentSlot[i].UpdateSlot(item.Value);
                }
            }

        }



        for (int i = 0; i < inventoryItemSlot.Length; i++)
        {
            inventoryItemSlot[i].CleanUpSlot();
        }

        for (int i = 0; i < stashItemSlot.Length; i++)
        {
            stashItemSlot[i].CleanUpSlot();
        }




        for (int i = 0; i < inventory.Count; i++)
        {
            inventoryItemSlot[i].UpdateSlot(inventory[i]);

        }

        for (int i = 0; i < stash.Count; i++)
        {
            stashItemSlot[i].UpdateSlot(stash[i]);

        }

        UpdateStatsUI();

    }

    public void UpdateStatsUI()
    {
        for (int i = 0; i < statsSlot.Length; i++)//更新UI中的数据
        {
            statsSlot[i].UpdateStatsValueUI();
        }
    }

    public void AddItem(ItemData _item)
    {
        if(_item == null) return;
        if (_item.ItemType == ItemType.Equipment&&CanAddItem() )
        {
            AddToInventory(_item);

        }
        else if (_item.ItemType==ItemType.Material)
        {
            AddToStash(_item);
        }



        UpdateSlotUI();

    }


    private void AddToStash(ItemData _item)
    {
        if (stashDictionary.TryGetValue(_item, out InventoryItem value))
        {
            value.AddStack();
        }
        else
        {
            InventoryItem newItem = new InventoryItem(_item);
            stash.Add(newItem);
            stashDictionary.Add(_item, newItem);
        }
    }

    private void AddToInventory(ItemData _item)
    {
        if (inventoryDictionary.TryGetValue(_item, out InventoryItem value))
        {
            value.AddStack();
        }
        else
        {
            InventoryItem newItem = new InventoryItem(_item);
            inventory.Add(newItem);
            inventoryDictionary.Add(_item, newItem);
        }
    }

    public void RemoveItem(ItemData _item)
    {
        if(_item.ItemType == ItemType.Equipment)
        {
            RemoveFromInventory(_item);
        }else if (_item.ItemType == ItemType.Material)
        {
            RemoveFromStash(_item);
        }


        UpdateSlotUI();
    }

    public bool CanAddItem()
    {
        if(inventory.Count >= inventoryItemSlot.Length)
        {
            return false;
        }
        return true;
    }

    private void RemoveFromStash(ItemData _item)
    {
        if (stashDictionary.TryGetValue(_item, out InventoryItem value))
        {
            if (value.stackSize <= 1)
            {
                stash.Remove(value);
                stashDictionary.Remove(_item);
            }
            else
            {
                value.RemoveStack();
            }
        }
    }

    private void RemoveFromInventory(ItemData _item)
    {
        if (inventoryDictionary.TryGetValue(_item, out InventoryItem value))
        {
            if (value.stackSize <= 1)
            {
                inventory.Remove(value);
                inventoryDictionary.Remove(_item);
            }
            else
            {
                value.RemoveStack();
            }
        }
    }

    public bool CanCraft(ItemData_Equipment _itemToCraft,List<InventoryItem> _requiredMaterials)
    {
        List<InventoryItem> materialsToRemove = new List<InventoryItem>();
        for (int i = 0; i < _requiredMaterials.Count; i++)
        {
            if(stashDictionary.TryGetValue(_requiredMaterials[i].data, out InventoryItem stashValue))
            {
                
                if (stashValue.stackSize < _requiredMaterials[i].stackSize)
                {
                    Debug.Log("not enough materials");
                    return false;
                }
                else
                {
                    materialsToRemove.Add(stashValue);
                }
            }
            else
            {
                Debug.Log("not enough materials");
                return false;
            }
        }

        for (int i = 0; i < materialsToRemove.Count; i++)
        {
            RemoveItem(materialsToRemove[i].data);
        }

        AddItem(_itemToCraft);


        return true;
    }

    public List<InventoryItem> GetEquipmentList()=>equipment;

    public List<InventoryItem> GetStashList() => stash;


    public ItemData_Equipment GetEquipment(EquipmentType _type)
    {
        ItemData_Equipment equipedItem = null;
        foreach (KeyValuePair<ItemData_Equipment, InventoryItem> item in equipmentDictionary)
        {
            if (item.Key.equipmentType == _type)
            {
                equipedItem = item.Key;
            }
        }
        
        return equipedItem;
    }

    public void UseFlask()
    {
        ItemData_Equipment currentFlask=GetEquipment(EquipmentType.Flask);
        if(currentFlask == null)
        {
            return;
        }


        bool canUseFlask=Time.time >= lastTimeUsedFlask + flaskCooldown;

        if (canUseFlask)
        {
            flaskCooldown = currentFlask.itemCooldown;
            currentFlask.Effect(null);
            lastTimeUsedFlask = Time.time;
        }
        
    }

    public void LoadData(GameData _data)
    {
        foreach (KeyValuePair<string, int> pair in _data.inventory)
        {
            foreach(var item in itemDataBase)
            {
                if (item != null && item.itemId ==pair.Key)
                {
                    InventoryItem itemToLoad = new InventoryItem(item);
                    itemToLoad.stackSize = pair.Value;

                    loadItems.Add(itemToLoad);

                }
            }

        }

        foreach (string loadItemId in _data.equipmentId)
        {
            foreach (var item in itemDataBase)
            {
                if(item!=null &&  loadItemId == item.itemId )
                {
                    loadEquipment.Add(item as ItemData_Equipment);



                }

            }


        }


    }

    public void SaveData(ref GameData _data)
    {
        _data.inventory.Clear();
        _data.equipmentId.Clear();
        foreach (KeyValuePair<ItemData,InventoryItem> pair in inventoryDictionary)
        {
            _data.inventory.Add(pair.Key.itemId, pair.Value.stackSize);
        }

        foreach (KeyValuePair<ItemData, InventoryItem> pair in stashDictionary)
        {
            _data.inventory.Add(pair.Key.itemId, pair.Value.stackSize);
        }

        foreach(KeyValuePair<ItemData_Equipment,InventoryItem> pair in equipmentDictionary)
        {
            _data.equipmentId.Add(pair.Key.itemId);
        }

    }


#if UNITY_EDITOR
    [ContextMenu("填满物品数据库")]
    private void FillUpItemDataBase() => itemDataBase = new List<ItemData>(GetItemDataBase());
    private List<ItemData> GetItemDataBase()
    {
        List<ItemData> itemDataBase = new List<ItemData>();
        string[] assetNames = AssetDatabase.FindAssets("", new[] { "Assets/Data/Items" });
        foreach (string SOName in assetNames) 
        {
            var SOpath=AssetDatabase.GUIDToAssetPath(SOName);
            var itemData=AssetDatabase.LoadAssetAtPath<ItemData>(SOpath);
            itemDataBase.Add(itemData);
        }
        return itemDataBase;
    }

#endif

}

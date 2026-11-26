using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 掉在地上的道具（ItemObject）的网络部分。
///
/// 掉落由服务器决定（只有服务器掷骰子），道具的“身份”用 itemId 同步，
/// 客户端拿到 id 之后从自己的道具库里还原出 <see cref="ItemData"/>，
/// 这样两个玩家看到的是同一个道具、同样的图标。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(ItemObject))]
public class NetItemObject : NetworkBehaviour
{
    private readonly NetworkVariable<FixedString64Bytes> networkItemId =
        new NetworkVariable<FixedString64Bytes>(default,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private ItemObject itemObject;

    private void Awake()
    {
        itemObject = GetComponent<ItemObject>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            // 服务器把自己手上的 itemData 广播出去
            if (itemObject != null && itemObject.itemData != null)
            {
                networkItemId.Value = new FixedString64Bytes(itemObject.itemData.itemId);
            }
        }
        else
        {
            ResolveFromId(networkItemId.Value.ToString());
            networkItemId.OnValueChanged += OnItemIdChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        networkItemId.OnValueChanged -= OnItemIdChanged;
        base.OnNetworkDespawn();
    }

    private void OnItemIdChanged(FixedString64Bytes previous, FixedString64Bytes current)
    {
        if (IsServer) return;
        ResolveFromId(current.ToString());
    }

    /// <summary>服务器在生成之前调用，指定这个掉落物是什么。</summary>
    public void ServerSetItem(ItemData data)
    {
        if (!IsServer || data == null) return;

        if (itemObject == null) itemObject = GetComponent<ItemObject>();
        itemObject.itemData = data;

        if (IsSpawned)
        {
            networkItemId.Value = new FixedString64Bytes(data.itemId);
        }
    }

    /// <summary>客户端请求服务器销毁这个掉落物（被拾取时用）。</summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestDespawnServerRpc()
    {
        if (!IsServer) return;

        NetworkObject no = GetComponent<NetworkObject>();
        if (no != null && no.IsSpawned)
        {
            no.Despawn(true);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void ResolveFromId(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return;

        if (itemObject == null) itemObject = GetComponent<ItemObject>();

        ItemData data = FindItemData(itemId);
        if (data == null) return;

        itemObject.itemData = data;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && data.icon != null)
        {
            sr.sprite = data.icon;
        }
    }

    private static ItemData FindItemData(string itemId)
    {
        if (Inventory.instance == null) return null;

        var database = Inventory.instance.itemDataBase;
        if (database == null) return null;

        for (int i = 0; i < database.Count; i++)
        {
            ItemData item = database[i];
            if (item != null && item.itemId == itemId) return item;
        }

        return null;
    }
}

/// <summary>生成网络物件的统一入口。</summary>
public static class NetSpawn
{
    /// <summary>
    /// 生成一个“所有玩家都能看到”的物件。
    /// 单机模式等价于 Instantiate；联机时只有服务器真正生成，客户端靠同步获得。
    /// </summary>
    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, bool destroyWithScene = true)
    {
        if (prefab == null) return null;

        if (!NetGame.IsOnline)
        {
            return Object.Instantiate(prefab, position, rotation);
        }

        NetworkObject template = prefab.GetComponent<NetworkObject>();
        if (template == null)
        {
            // 没有网络身份：只在本地生成，其它玩家看不到（降级表现）
            return Object.Instantiate(prefab, position, rotation);
        }

        if (!NetGame.IsServer)
        {
            // 客户端不生成，等服务器同步过来
            return null;
        }

        GameObject instance = Object.Instantiate(prefab, position, rotation);
        NetworkObject no = instance.GetComponent<NetworkObject>();
        if (no != null)
        {
            no.Spawn(destroyWithScene);
        }

        return instance;
    }
}

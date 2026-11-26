using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour,ISaveManager
{
    
    public static PlayerManager instance;
    public Player player;
    public int currency;

    /// <summary>
    /// 一定返回一个可用的玩家。
    /// 联机时如果还没有完成绑定，会自动用本地玩家兜底，避免空引用。
    /// </summary>
    public Player PlayerOrDefault
    {
        get
        {
            if (player != null) return player;

            Player local = NetGame.LocalPlayerEntity;
            if (local != null)
            {
                player = local;
                return local;
            }

            return null;
        }
    }

    public void Awake()
    {
        if (instance != null)
        {
            Destroy(instance.gameObject );
        }
        else
        {
            instance = this;
        }

    }

    public bool HaveEnoughMoney(int _price)
    {
        if (_price>currency)
        {

            return false;
        }

        currency -= _price;
        return true;
    }

    public int GetCurrency()
    {
        return currency;
    }


    public void LoadData(GameData _data)
    {
        this.currency = _data.currency;

    }

    public void SaveData(ref GameData _data)
    {
        _data.currency = this.currency;
    }

}


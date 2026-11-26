using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class GameManager : MonoBehaviour,ISaveManager
{
    public static GameManager instance;
    

    /// <summary>本地玩家的 Transform（延迟解析：联机时玩家是连接成功后才生成的）。</summary>
    private Transform player
    {
        get
        {
            if (PlayerManager.instance != null && PlayerManager.instance.player != null)
            {
                return PlayerManager.instance.player.transform;
            }
            return null;
        }
    }

    [SerializeField] CheckPiont[] checkpoins;

    [SerializeField] private string closestCheckPointId;

    [Header("Lost currency")]
    [SerializeField] private GameObject lostCurrencyPrefab;
    public int lostCurrencyAmount;
    private float lostCurrencyX;
    private float lostCurrencyY;


    public void RestartScene()
    {
        if (SaveManager.instance != null)
        {
            SaveManager.instance.SaveGame();
        }

        Scene scene = SceneManager.GetActiveScene();

        // 联机时场景切换必须由服务器发起，否则客户端会掉线
        if (NetGame.IsOnline && NetworkManager.Singleton != null)
        {
            if (NetGame.IsServer && NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.LoadScene(scene.name, LoadSceneMode.Single);
            }
            else
            {
                Debug.LogWarning("[GameManager] 联机模式下只有房主可以重开场景");
            }
            return;
        }

        SceneManager.LoadScene(scene.name);
    }


    private void Start()
    {


    }

    public void Awake()
    {
        if (instance != null)
        {
            Destroy(instance.gameObject);
        }
        else
        {
            instance = this;
        }

    }

    public void LoadData(GameData _data)
    {
        StartCoroutine(LoadWithDelay(_data));
    }


    private IEnumerator LoadWithDelay(GameData _data)
    {
        yield return new WaitForSeconds(.1f);
        LoadCheckpoints(_data);
        LoadClosestCheckpoint(_data);

        LoadLostCurrency(_data);


    }
    private void LoadCheckpoints(GameData _data)
    {
        checkpoins = FindObjectsOfType<CheckPiont>();
        foreach (KeyValuePair<string, bool> pair in _data.checkpoints)
        {
            foreach (CheckPiont checkpoint in checkpoins)
            {
                if (checkpoint.id == pair.Key && pair.Value == true)
                {
                    checkpoint.ActivateCheckPoint();
                }
            }
        }
    }
    private void LoadClosestCheckpoint(GameData _data)
    {
        Transform localPlayer = player;

        if (localPlayer == null)
        {
            // 联机时玩家还没生成，等生成之后再执行
            StartCoroutine(LoadClosestCheckpointWhenReady(_data));
            return;
        }

        if (closestCheckPointId == null)
            return;

        closestCheckPointId = _data.closestCheckpointId;

        foreach (CheckPiont checkPiont in checkpoins)
        {
            if (closestCheckPointId == checkPiont.id)
            {
                localPlayer.position = checkPiont.transform.position;

            }
        }
    }

    private void LoadLostCurrency(GameData _data)
    {
        lostCurrencyAmount = _data.lostCurrencyAmount;
        lostCurrencyX = _data.lostCurrencyX;
        lostCurrencyY = _data.lostCurrencyY;

        if (lostCurrencyAmount > 0)
        {
            GameObject newLostCurrency = Instantiate(lostCurrencyPrefab, new Vector3(lostCurrencyX, lostCurrencyY, 0),Quaternion.identity);
            newLostCurrency.GetComponent<LostCurrentcyController>().currentcy=lostCurrencyAmount;

        }
        lostCurrencyAmount = 0;
    }

    

    public void SaveData(ref GameData _data)
    {

        _data.lostCurrencyAmount = lostCurrencyAmount;
        Transform localPlayer = player;
        _data.lostCurrencyX = localPlayer != null ? localPlayer.position.x : 0f;
        _data.lostCurrencyY = localPlayer != null ? localPlayer.position.y : 0f;

        if (FindClosestCheckPoint() != null)
        {
            _data.closestCheckpointId = FindClosestCheckPoint().id;
        }

        _data.checkpoints.Clear();

        if (checkpoins != null)
        {
            foreach (CheckPiont checkpiont in checkpoins)
            {
                if (checkpiont == null) continue;
                _data.checkpoints.Add(checkpiont.id, checkpiont.activationStatus);
            }
        }

    }

    
    private IEnumerator LoadClosestCheckpointWhenReady(GameData _data)
    {
        float timeout = 5f;
        while (timeout > 0f && player == null)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (player == null)
        {
            yield break;
        }

        LoadClosestCheckpoint(_data);
    }

    private CheckPiont FindClosestCheckPoint()
    {
        // 退出游戏 / 主菜单场景里没有玩家，player 会是 null，
        // 这时候不能算距离，直接返回 null（SaveData 会跳过存档点写入）。
        Transform localPlayer = player;
        if (localPlayer == null) return null;
        if (checkpoins == null || checkpoins.Length == 0) return null;

        float closestDistance=Mathf.Infinity;
        CheckPiont closestCheckPoint = null;
        foreach (var checkPiont in checkpoins)
        {
            if (checkPiont == null) continue;

            float distanceToCheckPoint = Vector2.Distance(localPlayer.position, checkPiont.transform.position);
            if(distanceToCheckPoint < closestDistance&&checkPiont.activationStatus==true)
            {
                closestDistance = distanceToCheckPoint;
                closestCheckPoint=checkPiont;
            }
        }

        return closestCheckPoint;
    }

    public void PauseGame(bool _pause)
    {
        // 联机模式下不能冻结 Time.timeScale：那会连带卡住本机的网络收发，
        // 也会让另一个玩家的世界看起来停顿。这里只做本地打开菜单。
        if (NetGame.IsOnline)
        {
            Time.timeScale = 1;
            return;
        }

        if (_pause)
        {
            Time.timeScale = 0;
        }
        else
        {
            Time.timeScale = 1;
        }

    }


}

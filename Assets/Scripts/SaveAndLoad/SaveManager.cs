using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System.Linq;
public class SaveManager : MonoBehaviour
{
    public static SaveManager instance;
    public GameData gameData;
    [SerializeField] private string dataFileName;
    [SerializeField] private bool encryptData;


    public List<ISaveManager> saveManagers; 
    private FileDataHandler dataHandler;

    [ContextMenu("删除已保存的文件")]
    public void DeleateSaveData()
    {
        dataHandler = new FileDataHandler(Application.persistentDataPath, dataFileName,encryptData);
        dataHandler.Deleate();
    }


    private void Awake()
    {
        if (instance != null)
        {
            Destroy(instance.gameObject);
        }
        else
        {
            instance = this;
        }
        
        dataHandler = new FileDataHandler(Application.persistentDataPath, dataFileName,encryptData);

        saveManagers = FindAllSaveManagers();

        LoadGame();
        
        //Invoke("LoadGame", .0f);

    }

    private void Start()
    {
        
    }

    public void NewGame()
    {
        gameData = new GameData(); 
    }

    public void LoadGame()
    {
        gameData = dataHandler.Load(); 
        //这里的游戏数据来源于游戏中
        if(gameData == null)
        {
            Debug.Log("没有可以保存的数据");
            NewGame();
        }

        foreach (ISaveManager saveManager in saveManagers)
        {
            saveManager.LoadData(gameData);

        }


    }

    public void SaveGame()
    {

        foreach (ISaveManager saveManager in saveManagers)
        {
            saveManager.SaveData(ref gameData);
            
        }

        dataHandler.Save(gameData);
    }

    private void OnApplicationQuit()
    {
        SaveGame(); 
    }

    private List<ISaveManager> FindAllSaveManagers()
    {
        IEnumerable<ISaveManager> saveManagers = FindObjectsOfType<MonoBehaviour>(true).OfType<ISaveManager>();
        return new List<ISaveManager>(saveManagers);
    }

    public bool HasSaveData()
    {
        if(dataHandler.Load() != null)
        {
            return true;
        }
        return false;
    }


}

using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI : MonoBehaviour,ISaveManager
{

    [Header("死亡屏幕")]
    [SerializeField] public UI_FadeScreen fadeScreen;
    [SerializeField] private GameObject endText;
    [SerializeField] private GameObject restartButton;


    [SerializeField] private GameObject characterUI;
    [SerializeField] private GameObject skillTreeUI;
    [SerializeField] private GameObject craftUI;
    [SerializeField] private GameObject optionsUI;
    [SerializeField] private GameObject inGameUI;

    

    public UI_ItemToolTip itemToolTip;
    public UI_StatsToolTip statsToolTip;
    public UI_CraftWindow craftWindow;
    public UI_SkillToolTip skillToolTip;
    public bool ischange=false;

    [SerializeField] private UI_VolumeSlider[] volumeSettings;

    int currentMenuIndex = 0;

    private void Awake()
    {

        SwitchTo(skillTreeUI);//先加载技能树，使其可以被SkillManager挂载
        fadeScreen.gameObject.SetActive(true); 

    }



    void Start()
    {
        SwitchTo(null);
        SwitchTo(inGameUI);
        itemToolTip.gameObject.SetActive(false);
        statsToolTip.gameObject.SetActive(false);

        foreach (UI_VolumeSlider item in volumeSettings)
        {
            item.SliderValue(item.slider.value);

        }

    }

    void Update()
    {
        SwitchMenu();
    }

    


    private void SwitchMenu()
    {
        if(characterUI.activeInHierarchy)
        {
            currentMenuIndex = 1;
        }else if ( skillTreeUI.activeInHierarchy)
        {
            currentMenuIndex = 2;
        }else if( craftUI.activeInHierarchy)
        {
            currentMenuIndex = 3;
        }
        else if ( optionsUI.activeInHierarchy)
        {
            currentMenuIndex = 4;
        }
        else
        {
            currentMenuIndex = 0;
            
        }

        if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.Tab))
        {
            AudioManager.instance.PlaySFX(7);
            currentMenuIndex = 1;
        }

        if (currentMenuIndex != 0)
        {
            if (characterUI.activeInHierarchy || skillTreeUI.activeInHierarchy || craftUI.activeInHierarchy || optionsUI.activeInHierarchy)
            {
                if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Escape))
                {
                    AudioManager.instance.PlaySFX(7);
                    currentMenuIndex = 0;
                    ischange = true;
                }
            }


            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.E))
            {
                AudioManager.instance.PlaySFX(7);
                currentMenuIndex++;
                if (currentMenuIndex > 4)
                {
                    currentMenuIndex = 1;
                }

            }

            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.Q))
            {
                AudioManager.instance.PlaySFX(7);
                currentMenuIndex--;
                if (currentMenuIndex < 1)
                {
                    currentMenuIndex = 4;
                }
            }
        }

        if (!ischange && currentMenuIndex == 0)
        {
            return;
        }

        ischange = false;

        //常态下可以添加InGameUI
        if (currentMenuIndex == 0)
        {
            SwitchWithKeyTo(null);
            CheckForInGameUI();
        }
        else if (currentMenuIndex == 1)
        {
            SwitchWithKeyTo(characterUI);
        }
        else if (currentMenuIndex == 2)
        {
            SwitchWithKeyTo(skillTreeUI);
        }
        else if (currentMenuIndex == 3)
        {
            SwitchWithKeyTo(craftUI);
        }
        else if (currentMenuIndex == 4)
        {
            SwitchWithKeyTo(optionsUI);
        }

    }



    public void SwitchTo(GameObject _menu)
    {


        for (int i = 0; i < transform.childCount; i++)
        {
            bool FadeScreen = transform.GetChild(i).GetComponent<UI_FadeScreen>() != null;//我们需要一直保持激活状态
            if (FadeScreen == false)
            {
                transform.GetChild(i).gameObject.SetActive(false);
            }
        }

        if(_menu!=null)
        {

            _menu.SetActive(true);
        }

        if (GameManager.instance!=null)
        {
            if (_menu == inGameUI)
            {
                GameManager.instance.PauseGame(false);
            }
            else
            {
                GameManager.instance.PauseGame(true);
            }

        }



    }

    public void SwitchWithKeyTo(GameObject _menu) 
    {
        if(_menu!=null&&_menu.activeSelf)
        {
            _menu.SetActive(false);
            
        }

        SwitchTo(_menu);
    }

    private void CheckForInGameUI()
    {
        //FadeScene会一直处于激活状态，所以当遇到激活状态的情况下添加它
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).gameObject.activeSelf && transform.GetChild(i).GetComponent<UI_FadeScreen>()==null)
            {

                return;
            }
        }
        SwitchTo(inGameUI);
    }

    public void SwitchOnEndScreen()
    {
        // 每次死亡都重新显示结算文字，然后由协程依次弹出
        if (endText != null) endText.SetActive(false);
        if (restartButton != null) restartButton.SetActive(false);


        fadeScreen.FadeOut();
        StartCoroutine(EndScreenCorutione());
    }

    IEnumerator EndScreenCorutione()
    {
        yield return new WaitForSeconds(1f);
        endText.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        restartButton.SetActive(true);

    }

    public void ReStartButton()
    {
        // 联机时“重开”改成“原地复活”，不打断另一个玩家
        if (NetGame.IsOnline)
        {
            PlayerNetwork local = NetGame.LocalPlayer;
            if (local != null)
            {
                local.RequestRespawn();
            }
            return;
        }

        GameManager.instance.RestartScene();
    }

    /// <summary>关闭死亡结算界面（联机复活时用）。</summary>
    public void HideEndScreen()
    {
        if (endText != null) endText.SetActive(false);
        if (restartButton != null) restartButton.SetActive(false);

        if (fadeScreen != null) fadeScreen.FadeIn();

        SwitchTo(inGameUI);
    }

    


    public void LoadData(GameData _data)
    {
        foreach (KeyValuePair<string,float> pair in _data.voluemSettings)
        {
            foreach (UI_VolumeSlider item in volumeSettings)
            {
                if (item.parameter == pair.Key)
                {
                    item.LoadSlider(pair.Value);
                }
                
            }

        }
    }


    public void SaveData(ref GameData _data)
    {
        _data.voluemSettings.Clear();

        foreach (UI_VolumeSlider item in volumeSettings)
        {
            _data.voluemSettings.Add(item.parameter, item.slider.value);
        }

    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class UI_MainMenu : MonoBehaviour
{
    [SerializeField] private string sceneName = "MainScene";
    [SerializeField] private GameObject continueButton;
    [SerializeField] UI_FadeScreen fadeScreen;


    private void Start()
    {

        if (SaveManager.instance.HasSaveData()==false)
        {
            continueButton.SetActive(false);
            return;
        }


    }



    public void ContinueGame()
    {
        StartCoroutine(LoadWithFadeEffect(1.5f));


    }

    public void NewGame()
    {
        SaveManager.instance.DeleateSaveData();
        StartCoroutine(LoadWithFadeEffect(1.5f));
    }

    public void ExitGame()
    {
        //退出游戏
        Debug.Log("退出游戏");

    }

    IEnumerator LoadWithFadeEffect(float _delay)
    {
        fadeScreen.FadeOut();

        yield return new WaitForSeconds(_delay);

        SceneManager.LoadScene(sceneName);

    }


}

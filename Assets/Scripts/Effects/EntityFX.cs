using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using TMPro;

public class EntityFX : MonoBehaviour
{
    /// <summary>本地玩家；延迟解析，且允许为空（敌人身上也有这个组件）。</summary>
    protected Player player
    {
        get
        {
            if (PlayerManager.instance == null) return null;
            return PlayerManager.instance.PlayerOrDefault;
        }
    }
    [SerializeField] Entity entity;
    protected SpriteRenderer sr;
    [Header("弹出文本")]
    [SerializeField] private GameObject popUpTextPrefab;



    [Header("受击闪光")]
    [SerializeField] private float flashDuration = 0.2f;
    [SerializeField] private Material hitMat;
    private Material originalMat;

    [Header("元素颜色")]

    [SerializeField] private Color[] igniteColor;
    [SerializeField] private Color[] chillColor;
    [SerializeField] private Color[] shockColor;
    [SerializeField] private Color[] bleedColor;

    [Header("元素颗粒")]
    [SerializeField] private ParticleSystem  igniteFX;
    [SerializeField] private ParticleSystem  chileFX;
    [SerializeField] private ParticleSystem  shockFX;

    [Header("打击特效")]
    [SerializeField] private GameObject normalHitFX;
    [SerializeField] private GameObject criticalHitFX;

    private GameObject myHealthBar;


    


    protected virtual void Start()
    {
        sr=GetComponentInChildren<SpriteRenderer>(); 
        originalMat = sr.material;

        UI_HealthBar healthBar = GetComponentInChildren<UI_HealthBar>();
        myHealthBar = healthBar != null ? healthBar.gameObject : null;
    }
    

    
    public void CreatePopUpText(string _text)
    {
        // 服务端不需要飘字（而且 -nographics 下没有渲染）
        if (NetGame.IsDedicatedServer) return;

        float randomX = Random.Range(-2, 2);
        float randomY = Random.Range(3, 5);
        Vector3 positionOffset = new Vector3(randomX, randomY, 0);
        GameObject newText = Instantiate(popUpTextPrefab, transform.position+positionOffset, Quaternion.identity);
        newText.GetComponent<TextMeshPro>().text = _text;
        

    }

    


    



    public void MakerTransprent(bool _transprent)
    {
        if (_transprent)
        {
            sr.color = Color.clear;
            if (myHealthBar != null) myHealthBar.SetActive(false);
        }
        else
        {
            sr.color = Color.white;
            if (myHealthBar != null) myHealthBar.SetActive(true);
        }
    }

    protected IEnumerator FlashFX()
    {
        sr.material = hitMat;
        Color currentColor=sr.color;
        sr.color = Color.white;
        yield return new WaitForSeconds(flashDuration);
        sr.color = currentColor;
        sr.material = originalMat;
    }

    private void ReadColorBlink()
    {
        if(sr.color!=Color.white)
        {
            sr.color = Color.white;
        }
        else
        {
            sr.color = Color.yellow;
        }

    }

    private void CancelColorChange()
    {
        CancelInvoke();
        sr.color = Color.white;


        igniteFX.Stop();
        chileFX.Stop();
        shockFX.Stop(); 
    }


    //烧伤特效
    public void IgniteFxUse(float _seconds)
    {
        igniteFX.Play();

        InvokeRepeating("IgnitedColorFX", 0, 0.3f);
        Invoke("CancelColorChange", _seconds);
    }

   //冰冻特效
    public void ChillFxUse(float _seconds)
    {
        chileFX.Play();

        InvokeRepeating("ChilledColorFX", 0, 0.3f);
        Invoke("CancelColorChange", _seconds);
    }
    //眩晕特效
    public void ShockFxUse(float _seconds)
    {
        shockFX.Play();

        InvokeRepeating("ShockedColorFX", 0, 0.3f);
        Invoke("CancelColorChange", _seconds);
    }
    //重伤流血特效
    public void BleedFxUse(float _seconds)
    {
        InvokeRepeating("BleedColorFX", 0, 0.3f);
        Invoke("CancelColorChange", _seconds);
    }

    public void ClearFX()
    {
        sr.color = Color.clear;
    }

    private void IgnitedColorFX() 
    {
        if(sr.color != igniteColor[0])
        {
            sr.color = igniteColor[0];
        }
        else
        {
            sr.color = igniteColor[1];
        }
    }

    



    private void ChilledColorFX()
    {
        if (sr.color != chillColor[0])
        {
            sr.color = chillColor[0];
        }
        else
        {
            sr.color = chillColor[1];
        }
    }

    private void ShockedColorFX()
    {
        if (sr.color != shockColor[0])
        {
            sr.color = shockColor[0];
        }
        else
        {
            sr.color = shockColor[1];
        }
    }


    private void BleedColorFX()
    {
        if (sr.color != bleedColor[0])
        {
            sr.color = bleedColor[0];
        }
        else
        {
            sr.color = bleedColor[1];
        }
    }

    public void CreateHitFX(Transform _target,bool _critical)
    {

        float xPosition = Random.Range(-.5f, .5f);
        float yPosition = Random.Range(-.5f, .5f);
        float zRotation = Random.Range(-90, 90);

        Vector3 hitFXRotaion = new Vector3(0, 0, zRotation);

        GameObject hitPrefab = normalHitFX;

        if (_critical)
        {
            hitPrefab = criticalHitFX;
            float yRotation = 0;
            zRotation = Random.Range(-45, 45);
            //攻击方向不能动态调用
            if (entity.facingDir == -1)
            {
                yRotation = 180;
            }

            hitFXRotaion=new(0,yRotation,zRotation);
        }
        GameObject newHitFX = Instantiate(hitPrefab, _target.position + new Vector3(xPosition, yPosition, 0), Quaternion.identity,_target);

        newHitFX.transform.Rotate(hitFXRotaion);
        

        Destroy(newHitFX,.5f);

    }
    


}

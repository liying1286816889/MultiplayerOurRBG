using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum SwordType
{
    Regular,
    Bounce,
    Pierce,
    Spin
}

public class Sword_Skill : Skill {
    public SwordType swordType=SwordType.Regular;

    [Header("弹射剑信息")]
    [SerializeField] private UI_SkillTreeSlot bounceUnlockButton;

    [SerializeField] private int bounceAmount;
    [SerializeField] private float bounceSpeed;
    [SerializeField] private float bounceGravity;

    [Header("贯穿信息")]
    [SerializeField] private UI_SkillTreeSlot pierceUnlockButton;

    [SerializeField] private int pierceAmount;
    [SerializeField] private float pierceGravity;

    [Header("旋转信息")]
    [SerializeField] private UI_SkillTreeSlot spinUnlockButton;

    [SerializeField] private float hitCooldown ;
    [SerializeField] private float maxSpinDistance;
    [SerializeField] private float spinDuration;
    [SerializeField] private float spinGravity;

    [Header("普通信息")]
    [SerializeField] private UI_SkillTreeSlot swordUnlockButton;
    public bool swordUnlocked { get;private set; }
    [SerializeField] private GameObject swordPrefab;
    [SerializeField] private Vector2 launchForce;
    [SerializeField] private float RegularswordGravity;
    [SerializeField] private float freezeTimeDuration;
    [SerializeField] private float vulnerableTimeDuration;
    [SerializeField] private float returnSpeed;


    private Vector2 finalDir;

    [Header("瞄准点")]
    [SerializeField] private int numberOfDots;
    [SerializeField] private float spaceBeetweenDots;
    [SerializeField] private GameObject dotPrefab;
    [SerializeField] private Transform dotsParent;


    [Header("被动技能")]
    [SerializeField] private UI_SkillTreeSlot timeStopUnlockButton;
    public bool timeStopUnlocked { get;private set; }

    [SerializeField] private UI_SkillTreeSlot vulnerableUnlockButton;
    public bool vulnerableUnlocked { get;private set;} 


    private GameObject[] dots;

    protected override void Start()
    {
        base.Start();
        GenereateDots();
        SetupGraivty();


        swordUnlockButton.GetComponent<Button>().onClick.AddListener(UnlockSword);

        timeStopUnlockButton.GetComponent<Button>().onClick.AddListener(UnlockTimeStop);
        vulnerableUnlockButton.GetComponent<Button>().onClick.AddListener(UnlockVulnerable);

        bounceUnlockButton.GetComponent<Button>().onClick.AddListener(UnlockBounceSword);
        pierceUnlockButton.GetComponent<Button>().onClick.AddListener(UnlockPierceSword);
        spinUnlockButton.GetComponent<Button>().onClick.AddListener(UnlockSpinSword);

    }

    protected override void CheckUnlock()
    {
        UnlockSword();
        UnlockTimeStop();
        UnlockVulnerable();
        UnlockBounceSword();
        UnlockPierceSword();
        UnlockSpinSword();

    }

    private void SetupGraivty()
    {
        if (swordType == SwordType.Bounce)
        {
            RegularswordGravity = bounceGravity;
        }
        else if (swordType == SwordType.Pierce)
        {
            RegularswordGravity = pierceGravity;
        }
        else if (swordType == SwordType.Spin)
        {
            RegularswordGravity = spinGravity;

        }
    }

    protected override void Update()
    {
        SetupGraivty();
        if (Input.GetKeyUp(KeyCode.Mouse1))
        {
            finalDir =new Vector2(AimDirection().normalized.x * launchForce.x, AimDirection().normalized.y * launchForce.y);

        }
        if(Input.GetKey(KeyCode.Mouse1))
        {
            for (int i = 0; i < dots.Length; i++)
            {
                dots[i].transform.position = DotsPosition(i * spaceBeetweenDots);
            }
        }


    }

    public GameObject FxPrefab => swordPrefab;

    public void CreateSword()    {
        GameObject newSword = Instantiate(swordPrefab, player.transform.position, transform.rotation);
        Sword_Skill_Controller newSwordScript=newSword.GetComponent<Sword_Skill_Controller>();

        
        if (swordType == SwordType.Bounce)
        {
            newSwordScript.SetupBounce(true, bounceAmount,bounceSpeed);
        }
        else if (swordType==SwordType.Pierce)
        {
            newSwordScript.SetupPierce(pierceAmount);
        }else if (swordType == SwordType.Spin)
        {
            newSwordScript.SetupSpin(true,maxSpinDistance, spinDuration,hitCooldown);
        }

        newSwordScript.SetupSword(finalDir, RegularswordGravity, player,freezeTimeDuration,returnSpeed, vulnerableTimeDuration);

        player.AssignNewSword(newSword);

        NetSkillFx.Broadcast(SkillFxKind.Sword, newSword.transform.position, finalDir,
            RegularswordGravity, returnSpeed, swordType == SwordType.Spin ? spinDuration : 0f);
        DotsActive(false);
    }

    #region Unlock Region
    private void UnlockTimeStop()
    {
        if (timeStopUnlockButton.unLocked)
        {
            timeStopUnlocked = true;
        }

    }

    private void UnlockVulnerable()
    {
        if (vulnerableUnlockButton.unLocked)
        {
            vulnerableUnlocked = true;
        }

    }

    private void UnlockSword()
    {
        if (swordUnlockButton.unLocked) 
        {
            swordType=SwordType.Regular;
            swordUnlocked = true;
        }

    }

    private void UnlockBounceSword()
    {
        if (bounceUnlockButton.unLocked)
        {
            swordType = SwordType.Bounce;
        }
    }

    private void UnlockPierceSword()
    {
        if (pierceUnlockButton.unLocked)
        {
            swordType = SwordType.Pierce;
        }

    }

    private void UnlockSpinSword()
    {
        if (spinUnlockButton.unLocked)
        {
            swordType = SwordType.Spin;
        }
    }




    #endregion

    #region aim_region

    public Vector2 AimDirection()
    {
        Vector2 playerPosition=player.transform.position;
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = mousePosition - playerPosition;
        return direction;
    }
    public void DotsActive(bool _isActive)
    {
        for (int i = 0; i < dots.Length; i++)
        {
            dots[i].SetActive(_isActive);
        }

    }


    private void GenereateDots()
    {
        dots=new GameObject[numberOfDots];
        for (int i = 0; i < numberOfDots; i++)
        {
            dots[i] = Instantiate(dotPrefab, player.transform.position, Quaternion.identity, dotsParent);
            dots[i].SetActive(false);
        }

    }

    private Vector2 DotsPosition(float t)
    {
        Vector2 position = (Vector2)player.transform.position + new Vector2(
            AimDirection().normalized.x * launchForce.x,
            AimDirection().normalized.y * launchForce.y) * t + .5f * (Physics2D.gravity * RegularswordGravity) * (t * t);
        return position;
    }
    #endregion

}

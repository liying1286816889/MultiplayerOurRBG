using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Crystal_Skill : Skill
{
    [SerializeField] private float crystalDuration = 5f;
    [SerializeField] private GameObject crystalPrefab;
    private GameObject currentCrystal;

    [Header("Crystal mirage")]
    [SerializeField] private UI_SkillTreeSlot unlockCloneInstanceButton;
    [SerializeField] private bool cloneInsteadOfCrystal;

    [Header("Crystal simple")]
    [SerializeField] private UI_SkillTreeSlot unlockCrystalButton;
    [SerializeField] public bool crystalUnlocked { get;private set; }
    



    [Header("Explosive crystal ")]
    [SerializeField] private UI_SkillTreeSlot unlockExplosiveButton;
    [SerializeField] private bool canExplode;


    [Header("Moving crystal")]
    [SerializeField] private UI_SkillTreeSlot unlockMovingCrystalButton;
    [SerializeField] private bool canMoveToEnemy;
    [SerializeField] private float moveSpeed;

    [Header("Multi stacking crystal")]
    [SerializeField] private UI_SkillTreeSlot unlockMultiStackButton;
    [SerializeField] private bool canUseMultiStack;
    [SerializeField] private int amountOfStacks;
    [SerializeField] private float multiStackCooldown;
    [SerializeField] private float useTimeWindow;
    [SerializeField] private List<GameObject> crystalLeft = new List<GameObject>();

    protected override void Start()
    {
        base.Start();
        unlockCrystalButton.GetComponent<Button>().onClick.AddListener(UnlockCrystal);
        unlockCloneInstanceButton.GetComponent<Button>().onClick.AddListener(UnlockCrystalMirage);
        unlockExplosiveButton.GetComponent<Button>().onClick.AddListener(UnlockeExplosiveCrystal);
        unlockMovingCrystalButton.GetComponent<Button>().onClick.AddListener(UnlockMovingCrystal);
        unlockMultiStackButton.GetComponent<Button>().onClick.AddListener(UnlockMultiStack);

    }

    protected override void CheckUnlock()
    {
        UnlockCrystal();
        UnlockCrystalMirage();
        UnlockeExplosiveCrystal();
        UnlockMovingCrystal();
        UnlockMultiStack();

    }


    #region Unlock Skill Region

    private void UnlockCrystal()
    {
        if (unlockCrystalButton.unLocked)
        {
            crystalUnlocked = true;
        }

    }
    private void UnlockCrystalMirage()
    {
        if (unlockCloneInstanceButton.unLocked)
        {
            cloneInsteadOfCrystal=true;
        }
    }

    private void UnlockeExplosiveCrystal()
    {
        if (unlockExplosiveButton.unLocked)
        {
            canExplode = true;
        }
    }

    private void UnlockMovingCrystal()
    {
        if (unlockMovingCrystalButton.unLocked)
        {
            canMoveToEnemy = true;
        }

    }

    private void UnlockMultiStack()
    {
        if (unlockMultiStackButton.unLocked)
        {
            canUseMultiStack = true;
        }

    }

    #endregion


    public override void UseSkill()
    {
        base.UseSkill();

        if (CanUseMultiCrystal())
        {
            return; 
        }


        if (currentCrystal==null)
        {
            CreateCrystal();

        }
        else
        {
            if(canMoveToEnemy) return; 

            Vector2 playerPos= player.transform.position;
            player.transform.position = currentCrystal.transform.position; 
            currentCrystal.transform.position = playerPos;
            if (cloneInsteadOfCrystal)
            {
                SkillManager.instance.clone.CreateClone(currentCrystal.transform,Vector3.zero);
                Destroy(currentCrystal);
            }
            else
            {
                currentCrystal.GetComponent<Crystal_Skill_Controller>()?.FinishCrystal();

            }



        }

    }

    public GameObject FxPrefab => crystalPrefab;

    public void CreateCrystal()    {
        currentCrystal = Instantiate(crystalPrefab, player.transform.position, Quaternion.identity);
        Crystal_Skill_Controller currentCrystalScript = currentCrystal.GetComponent<Crystal_Skill_Controller>();
        currentCrystalScript.SetupCrystal(crystalDuration, canExplode, canMoveToEnemy, moveSpeed, FindClosesEnemy(currentCrystal.transform),player);

        int fxFlags = (canExplode ? 1 : 0) + (canMoveToEnemy ? 2 : 0);
        NetSkillFx.Broadcast(SkillFxKind.Crystal, currentCrystal.transform.position, Vector3.zero,
            crystalDuration, moveSpeed, fxFlags);    }

    public void CurrentCrystalChooseRandomTarget()=>currentCrystal?.GetComponent<Crystal_Skill_Controller>()?.ChooseRandomEnemy();




    private bool CanUseMultiCrystal()
    {
        if (canUseMultiStack)
        {
            if(crystalLeft.Count > 0)
            {
                if (crystalLeft.Count == amountOfStacks)
                {
                    Invoke("ResetAbility", useTimeWindow);
                }


                cooldown = 0; 
                GameObject crystalToSpawn= crystalLeft[crystalLeft.Count - 1];
                GameObject newCrystal = Instantiate(crystalToSpawn, player.transform.position, Quaternion.identity);

                crystalLeft.Remove(crystalToSpawn);

                newCrystal.GetComponent<Crystal_Skill_Controller>()?.SetupCrystal(crystalDuration, canExplode, canMoveToEnemy, moveSpeed, FindClosesEnemy(newCrystal.transform), player);
                if (crystalLeft.Count <= 0)
                {
                    //ÀäÈ´£¬²¹³äË®¾§ÊýÁ¿
                    cooldown = multiStackCooldown;
                    RefilCrystal();

                }
                return true;

            }

        }
        return false;
    }

    private void RefilCrystal()
    {
        int amountToAdd= amountOfStacks - crystalLeft.Count;

        for (int i = 0; i < amountToAdd; i++)
        {
            crystalLeft.Add(crystalPrefab);
        }
    }
    private void ResetAbility()
    {
        if (cooldownTimer > 0)
        {
            return;
        }


        cooldownTimer = multiStackCooldown;
        RefilCrystal();
    }

}

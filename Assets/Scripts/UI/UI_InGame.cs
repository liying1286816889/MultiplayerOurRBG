using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_InGame : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Slider slider;
    [SerializeField] private Image dashImage;
    [SerializeField] private Image parryImage;
    [SerializeField] private Image crystalImage;
    [SerializeField] private Image swordImage;
    [SerializeField] private Image blackholeImage;
    [SerializeField] private Image flaskImage;


    [Header("Solus info")]
    [SerializeField] private TextMeshProUGUI currentSouls;
    [SerializeField] private float soulsAmount;
    [SerializeField] private float increaseRate = 100;

    private SkillManager skills;

    private void Start()
    {
        skills = SkillManager.instance;

        // 联机时玩家是运行时生成的：本地玩家生成后要把血条重新绑到“我自己”
        MultiplayerEvents.LocalPlayerSpawned += OnLocalPlayerSpawned;

        if (NetGame.HasLocalPlayer)
        {
            OnLocalPlayerSpawned(NetGame.LocalPlayer);
        }
        else if (playerStats != null)
        {
            playerStats.onHealthChanged += UpdateHealthUI;
            UpdateHealthUI();
        }



    }
    private void OnDestroy()
    {
        MultiplayerEvents.LocalPlayerSpawned -= OnLocalPlayerSpawned;
    }

    /// <summary>把界面重新绑定到本地玩家的 PlayerStats。</summary>
    public void OnLocalPlayerSpawned(PlayerNetwork _player)
    {
        if (_player == null) return;

        PlayerStats newStats = _player.GetComponent<PlayerStats>();
        if (newStats == null) return;
        if (playerStats == newStats) return;

        if (playerStats != null)
        {
            playerStats.onHealthChanged -= UpdateHealthUI;
        }

        playerStats = newStats;
        playerStats.onHealthChanged += UpdateHealthUI;

        UpdateHealthUI();
    }

    private void Update()
    {
        if (skills == null)
        {
            skills = SkillManager.instance;
        }

        if (skills == null)
        {
            return;
        }

        UpdataSoulsUI();

        if (Input.GetKeyUp(KeyCode.LeftShift) && skills.dash.dashUnlocked)
        {
            SetCooldownOf(dashImage);
        }

        if (Input.GetKeyUp(KeyCode.Q) && skills.parry.parryUnLocked)
        {
            SetCooldownOf(parryImage);
        }

        if (Input.GetKeyUp(KeyCode.F) && skills.crystal.crystalUnlocked)
        {
            SetCooldownOf(crystalImage);
        }

        if (Input.GetKeyUp(KeyCode.Mouse1) && skills.sword.swordUnlocked)
        {
            SetCooldownOf(swordImage);
        }

        if (Input.GetKeyUp(KeyCode.R) && skills.blackhole.blackHoleUnlocked)
        {
            SetCooldownOf(blackholeImage);
        }

        if (Input.GetKeyUp(KeyCode.Alpha1) && Inventory.instance.GetEquipment(EquipmentType.Flask) != null)
        {
            SetCooldownOf(flaskImage);
        }

        //冲刺图标 
        CheckCooldownOf(dashImage, skills.dash.cooldown);
        //反击图标
        CheckCooldownOf(parryImage, skills.parry.cooldown);
        //水晶图标
        CheckCooldownOf(crystalImage, skills.crystal.cooldown);
        //剑图标
        CheckCooldownOf(swordImage, skills.sword.cooldown);
        //黑洞图标
        CheckCooldownOf(blackholeImage, skills.blackhole.cooldown);
        //药水图标
        CheckCooldownOf(flaskImage, Inventory.instance.flaskCooldown);
    }

    private void UpdataSoulsUI()
    {
        if (soulsAmount < PlayerManager.instance.GetCurrency())
        {
            soulsAmount += Time.deltaTime * increaseRate;
        }
        else
        {
            soulsAmount = PlayerManager.instance.GetCurrency();
        }

        if (currentSouls != null)
        {
            currentSouls.text = Mathf.RoundToInt(soulsAmount).ToString();
        }
    }

    private void UpdateHealthUI()
    {
        if (playerStats == null || slider == null) return;

        slider.maxValue = playerStats.GetMaxHealthValue();
        slider.value = playerStats.currentHealth;
    }

    private void SetCooldownOf(Image _image)
    {
        if (_image.fillAmount <= 0)
        {
            _image.fillAmount = 1;
        }
        
    }

    private void CheckCooldownOf(Image _image,float _cooldown)
    {
        if (_cooldown == 0) return; 
        if (_image.fillAmount > 0)
        {
            _image.fillAmount -= 1/_cooldown*Time.deltaTime;
        }

    }

}

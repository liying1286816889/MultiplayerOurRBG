using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class UI_HealthBar : MonoBehaviour
{
    private Entity entity=>GetComponentInParent<Entity>();
    private RectTransform myTransform;
    private Slider slider;
    private CharacterStats myStats=>GetComponentInParent<CharacterStats>();


    private void Start()
    {
        myTransform = GetComponent<RectTransform>();
        slider = GetComponentInChildren<Slider>();

        UpdateHealthUI();
    }
    private void Update()
    {
        UpdateHealthUI();
    }

    private void UpdateHealthUI()
    {
        slider.maxValue = myStats.GetMaxHealthValue();
        slider.value = myStats.currentHealth;
    }
    private void OnEnable()
    {
        entity.OnFlipped += FlipUI;
        myStats.onHealthChanged += UpdateHealthUI;

    }



    
    private void OnDisable()
    {
        if (entity != null)
        {
            entity.OnFlipped -= FlipUI;
        }
        if (myStats != null)
        {
            myStats.onHealthChanged -= UpdateHealthUI;
        }
    }

    private void FlipUI() => myTransform.Rotate(0f, 180f, 0f);

}

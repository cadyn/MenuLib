﻿using System;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MenuLib.MonoBehaviors;

public sealed class REPOSlider : REPOElement
{
    public enum BarBehavior
    {
        UpdateWithValue,
        StaticAtMinimum,
        StaticAtMaximum
    }
    
    public TextMeshProUGUI labelTMP, descriptionTMP;

    public Action<float> onValueChanged;

    public BarBehavior barBehavior;

    public float value;

    public float min
    {
        get => stringOptions?.Length > 0 ? 0 : _min;
        set => _min = value;
    }
    public float max
    {
        get => stringOptions?.Length > 0 ? stringOptions.Length - 1 : _max;
        set => _max = value;
    }

    public string prefix, postfix;

    public string[] stringOptions
    {
        get => _stringOptions;
        set
        {
            _stringOptions = value;
            UpdateBarText();
        }
    }

    public int precision
    {
        get => stringOptions?.Length > 0 ? 0 : _precision;
        set
        {
            precisionDecimal = value == 0 ? 1 : Mathf.Pow(10, -value);
            _precision = value;
        }
    }

    public float precisionDecimal
    {
        get => stringOptions?.Length > 0 ? 1 : _precisionDecimal;
        set => _precisionDecimal = value;
    }
    
    private RectTransform barRectTransform, barSizeRectTransform, barPointerRectTransform, barMaskRectTransform;
    private TextMeshProUGUI valueTMP, maskedValueTMP;
    
    private MenuPage menuPage;
    private MenuSelectableElement menuSelectableElement;

    private float normalizedValue => (value - min) / (max - min);
    private float _min, _max = 1;
    private float previousValue, _precisionDecimal = .01f;
    private int _precision = 2;
    private string[] _stringOptions = [];
    private bool isHovering;

    private bool hasValueChanged => Math.Abs(value - previousValue) > float.Epsilon;
    
    public void SetValue(float newValue, bool invokeCallback)
    {
        newValue = Mathf.Clamp(newValue, min, max);
        
        if (invokeCallback && Math.Abs(value - newValue) > float.Epsilon)
            onValueChanged.Invoke(newValue);

        previousValue = value = newValue;
        
        UpdateBarVisual();
        UpdateBarText();
    }

    public void Decrement()
    {
        var newValue = value - precisionDecimal;

        if (Math.Abs(value - min) < float.Epsilon)
            newValue = max;
        else if (newValue < min)
            newValue = min;
        
        SetValue(newValue, true);
    }

    public void Increment()
    {
        var newValue = value + precisionDecimal;

        if (Math.Abs(max - value) < float.Epsilon)
            newValue = min;
        else if (newValue > max)
            newValue = max;
        
        SetValue(newValue, true);
    }
    
    
    #warning add description text scroller
    private void Awake()
    {
        rectTransform = transform as RectTransform;
        menuPage = GetComponentInParent<MenuPage>();
        menuSelectableElement = GetComponent<MenuSelectableElement>();
        labelTMP = GetComponentInChildren<TextMeshProUGUI>();
        descriptionTMP = transform.Find("Big Setting Text").GetComponent<TextMeshProUGUI>();
        valueTMP = transform.Find("Bar Text").GetComponent<TextMeshProUGUI>();
        barMaskRectTransform = (RectTransform) transform.Find("MaskedText"); 
        maskedValueTMP = barMaskRectTransform.GetComponentInChildren<TextMeshProUGUI>();
        barPointerRectTransform = (RectTransform) transform.Find("Bar Pointer").transform;

        var horizontalShift = Vector3.right * 5.3f;
        
        labelTMP.rectTransform.localPosition -= horizontalShift;

        descriptionTMP.alignment = TextAlignmentOptions.Left;
        descriptionTMP.enableWordWrapping = descriptionTMP.enableAutoSizing = false;
        descriptionTMP.overflowMode = TextOverflowModes.Masking;
        descriptionTMP.fontSize -= 5;

        descriptionTMP.rectTransform.sizeDelta -= new Vector2(0, 4);

        transform.Find("SliderBG").localPosition -= horizontalShift;

        valueTMP.rectTransform.localPosition -= horizontalShift;
        maskedValueTMP.rectTransform.parent.localPosition -= horizontalShift;
        
        var bar = transform.Find("Bar");
        bar.localPosition -= horizontalShift;
        
        barRectTransform = (RectTransform) bar.Find("RawImage");
        barRectTransform.pivot = Vector2.zero;
        barRectTransform.localPosition = new Vector2(0f, -5f);

        barSizeRectTransform = (RectTransform) transform.Find("BarSize");
        barSizeRectTransform.localPosition -= horizontalShift;
        
        var labelSizeDelta = labelTMP.rectTransform.sizeDelta;
        labelSizeDelta.y -= 10;
        labelTMP.rectTransform.sizeDelta = labelSizeDelta;
        
        var buttons = GetComponentsInChildren<Button>();

        var decrementButton = buttons[0];
        decrementButton.transform.localPosition -= horizontalShift;
        decrementButton.onClick = new Button.ButtonClickedEvent();
        decrementButton.onClick.AddListener(Decrement);
        
        var incrementButton = buttons[1];
        incrementButton.transform.localPosition -= horizontalShift;
        incrementButton.onClick = new Button.ButtonClickedEvent();
        incrementButton.onClick.AddListener(Increment);
        
        Destroy(bar.Find("Extra Bar").gameObject);
        Destroy(GetComponent<MenuSliderMicrophone>());
        Destroy(GetComponent<MenuSlider>());
    }

    private void Update()
    {
        var isHoveringUI = SemiFunc.UIMouseHover(menuPage, barSizeRectTransform, REPOReflection.menuSelectableElement_menuID.GetValue(menuSelectableElement) as string, 5f, 5f);

        if (isHoveringUI)
        {
            if (!isHovering)
                MenuManager.instance.MenuEffectHover(SemiFunc.MenuGetPitchFromYPos(rectTransform));
            
            isHovering = true;
            
            SemiFunc.MenuSelectionBoxTargetSet(menuPage, barSizeRectTransform, new Vector2(-3f, 0f), new Vector2(20f, 10f));
            
            if (!barPointerRectTransform.gameObject.activeSelf)
                barPointerRectTransform.gameObject.SetActive(true);
            
            HandleHovering();
        }
        else
        {
            isHovering = false;

            if (barPointerRectTransform.gameObject.activeSelf)
            {
                barPointerRectTransform.localPosition = barPointerRectTransform.localPosition with { x = -1000f };
                barPointerRectTransform.gameObject.SetActive(false);
            }
        }

        if (!hasValueChanged)
            return;
        
        value = Mathf.Clamp(value, min, max);
        
        UpdateBarVisual();
        UpdateBarText();
        
        onValueChanged.Invoke(previousValue = value);
    }
    
    private void HandleHovering()
    {
        var pointInRect = SemiFunc.UIMouseGetLocalPositionWithinRectTransform(barSizeRectTransform).x;
        
        var multiplier = max - min;
        var steps = precisionDecimal / multiplier;
        var normalized = Mathf.Round(Mathf.Clamp01(pointInRect / barSizeRectTransform.sizeDelta.x) / steps) * steps;
        
        var newXPos = Mathf.Clamp(barSizeRectTransform.localPosition.x + normalized * barSizeRectTransform.sizeDelta.x, barSizeRectTransform.localPosition.x,
            barSizeRectTransform.localPosition.x + barSizeRectTransform.sizeDelta.x) - 2f;

        barPointerRectTransform.localPosition = barPointerRectTransform.localPosition with { x = newXPos };

        if (!Input.GetMouseButton(0))
            return;
        
        value = min + normalized * multiplier;
        
        if (hasValueChanged)
            MenuManager.instance.MenuEffectClick(MenuManager.MenuClickEffectType.Tick, menuPage);
    }
    
    private void UpdateBarVisual()
    {
        var newNormalizedBarValue = barBehavior switch
        {
            BarBehavior.UpdateWithValue => normalizedValue,
            BarBehavior.StaticAtMinimum => 0,
            BarBehavior.StaticAtMaximum => 1,
            _ => throw new ArgumentOutOfRangeException()
        };
        
        barRectTransform.sizeDelta = barMaskRectTransform.sizeDelta = new Vector2(newNormalizedBarValue * 100, 10);
    }

    private void UpdateBarText()
    {
        var valueToDisplay = value;

        prefix ??= string.Empty;
        postfix ??= string.Empty;

        var barText = prefix;
        
        if (stringOptions?.Length > 0)
            barText += stringOptions.ElementAtOrDefault(Convert.ToInt32(valueToDisplay)) ?? stringOptions.First();
        else 
            barText += valueToDisplay.ToString($"F{precision}", CultureInfo.CurrentCulture);

        maskedValueTMP.text = valueTMP.text = barText + postfix;
    }
}
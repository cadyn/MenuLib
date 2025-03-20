﻿using System;
using System.Collections;
using System.Collections.Generic;
using MenuLib.MonoBehaviors;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MenuLib;

public static class MenuAPI
{
    internal static BuilderDelegate mainMenuBuilderDelegates;
    internal static BuilderDelegate escapeMenuBuilderDelegates;

    private static MenuButtonPopUp menuButtonPopup;
    
    public delegate void BuilderDelegate(Transform parent);
    
    public static void AddElementToMainMenu(BuilderDelegate builderDelegate) => mainMenuBuilderDelegates += builderDelegate;
    
    public static void AddElementToEscapeMenu(BuilderDelegate builderDelegate) => escapeMenuBuilderDelegates += builderDelegate;
    
#warning Might create custom versions of this
    public static void OpenPopup(string header, Color headerColor, string content, string buttonText, Action onClick) => MenuManager.instance.PagePopUp(header, headerColor, content, buttonText);

    public static void OpenPopup(string header, Color headerColor, string content, string leftButtonText, Action onLeftClicked, string rightButtonText, Action onRightClicked = null)
    {
        if (!menuButtonPopup)
            menuButtonPopup = MenuManager.instance.gameObject.AddComponent<MenuButtonPopUp>();

        menuButtonPopup.option1Event = new UnityEvent();
        menuButtonPopup.option2Event = new UnityEvent();
        
        if (onLeftClicked != null)
            menuButtonPopup.option1Event.AddListener(new UnityAction(onLeftClicked));
        
        if (onRightClicked != null)
            menuButtonPopup.option2Event.AddListener(new UnityAction(onRightClicked));
        
        MenuManager.instance.PagePopUpTwoOptions(menuButtonPopup, header, headerColor, content, leftButtonText, rightButtonText);
    }

    public static REPOButton CreateREPOButton(string text, Action onClick, Transform parent, Vector2 localPosition = new())
    {
        var newRectTransform = Object.Instantiate(REPOTemplates.buttonTemplate, parent);
        newRectTransform.name = $"Menu Button - {text}";

        newRectTransform.localPosition = localPosition;
        
        var repoButton = newRectTransform.gameObject.AddComponent<REPOButton>();

        repoButton.labelTMP.text = text;
        repoButton.button.onClick = new Button.ButtonClickedEvent();
        
        if (onClick != null)
            repoButton.button.onClick.AddListener(new UnityAction(onClick));

		Object.Destroy(newRectTransform.GetComponent<MenuButtonPopUp>());
        return repoButton;
    }
    
    public static REPOToggle CreateREPOToggle(string text, Action<bool> onToggle, Transform parent, Vector2 localPosition = new(), string leftButtonText = "ON", string rightButtonText = "OFF", bool defaultValue = false)
    {
        var newRectTransform = Object.Instantiate(REPOTemplates.toggleTemplate, parent);
        newRectTransform.name = $"Menu Toggle - {text}";

        newRectTransform.localPosition = localPosition;
        
        var repoToggle = newRectTransform.gameObject.AddComponent<REPOToggle>();

        repoToggle.labelTMP.text = text;
        repoToggle.leftButtonTMP.text = leftButtonText;
        repoToggle.rightButtonTMP.text = rightButtonText;
        repoToggle.onToggle = onToggle;
        
        repoToggle.leftButton.onClick = new Button.ButtonClickedEvent();
        repoToggle.rightButton.onClick = new Button.ButtonClickedEvent();
        
        repoToggle.leftButton.onClick.AddListener(() => repoToggle.SetState(true, true));
        repoToggle.rightButton.onClick.AddListener(() => repoToggle.SetState(false, true));
        
        repoToggle.SetState(defaultValue, false);
        
        Object.Destroy(newRectTransform.GetComponent<MenuTwoOptions>());
        return repoToggle;
    }

    public static REPOPopupPage CreatePopupPage(string headerText, bool pageDimmerVisibility = false, Vector2 localPosition = new())
    {
        var newRectTransform = Object.Instantiate(REPOTemplates.popupPageTemplate, MenuHolder.instance.transform);
        newRectTransform.name = $"Menu Page {headerText}";

#warning fix positions, header, scroll, they all have to move together
        //newRectTransform.localPosition = localPosition;
        
        var repoPopupPage = newRectTransform.gameObject.AddComponent<REPOPopupPage>();
        repoPopupPage.pageDimmerVisibility = repoPopupPage;

        repoPopupPage.headerTMP.text = headerText;
        
        Object.Destroy(newRectTransform.GetComponent<MenuPageSettingsPage>());
        newRectTransform.gameObject.AddComponent<MenuPageSettings>();
        
        return repoPopupPage;
    }

    internal static void OpenPage(MenuPage menuPage, bool pageOnTop)
    {
        var currentMenuPage = REPOReflection.menuManager_CurrentMenuPage.GetValue(MenuManager.instance) as MenuPage;

        var addedPagesOnTop = REPOReflection.menuManager_AddedPagesOnTop.GetValue(MenuManager.instance) as List<MenuPage>; 
        
        switch (pageOnTop)
        {
            case true when addedPagesOnTop == null || addedPagesOnTop.Contains(currentMenuPage):
                return;
            case false:
                REPOReflection.menuManager_PageInactiveAdd.Invoke(MenuManager.instance, [ currentMenuPage ]);
                currentMenuPage?.PageStateSet(MenuPage.PageState.Inactive);
                break;
        }

        menuPage.transform.localPosition = Vector3.zero;
        MenuManager.instance.PageAdd(menuPage);
        menuPage.StartCoroutine(REPOReflection.menuPage_LateStart.Invoke(menuPage, null) as IEnumerator);
            
        REPOReflection.menuPage_AddedPageOnTop.SetValue(menuPage, false);
        
        if (!pageOnTop)
        {
            MenuManager.instance.PageSetCurrent(menuPage.menuPageIndex, menuPage);
        
            REPOReflection.menuPage_PageIsOnTopOfOtherPage.SetValue(menuPage, true);
            REPOReflection.menuPage_PageUnderThisPage.SetValue(menuPage, currentMenuPage);
            return;
        }
        
        REPOReflection.menuPage_ParentPage.SetValue(menuPage, currentMenuPage);
        addedPagesOnTop.Add(menuPage);
    }
}
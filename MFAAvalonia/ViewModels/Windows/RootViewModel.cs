using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MFAAvalonia.Configuration;
using MFAAvalonia.Extensions;
using MFAAvalonia.Extensions.MaaFW;
using MFAAvalonia.Helper;
using MFAAvalonia.Utilities.CardClass;
using SukiUI.Dialogs;
using System;
using System.Linq;

namespace MFAAvalonia.ViewModels.Windows;

public partial class RootViewModel : ViewModelBase
{
    protected override void Initialize()
    {
        CheckDebug();
        // 初始化时校验一次保存的卡片名称
        if (!string.IsNullOrEmpty(NeedCardNameInput))
        {
            IsNeedCardNameValid = CCMgr.Instance.IsCardNameValid(NeedCardNameInput);
        }
    }

    [ObservableProperty] private bool _idle = true;
    [ObservableProperty] private bool _isWindowVisible = true;
    [ObservableProperty] private bool _enableCardSystem = ConfigurationManager.Current.GetValue(ConfigurationKeys.EnableCardSystem, true);
    [ObservableProperty] private bool _enableCardEffect = ConfigurationManager.Current.GetValue(ConfigurationKeys.EnableCardEffect, true);
    [ObservableProperty] private bool _enableBorderEffect = ConfigurationManager.Current.GetValue(ConfigurationKeys.EnableBorderEffect, true);
    [ObservableProperty] private bool _enableHideDuplicateCards = ConfigurationManager.Current.GetValue(ConfigurationKeys.EnableHideDuplicateCards, false);

    partial void OnEnableCardEffectChanged(bool value)
    {
        ConfigurationManager.Current.SetValue(ConfigurationKeys.EnableCardEffect, value);
    }

    partial void OnEnableBorderEffectChanged(bool value)
    {
        ConfigurationManager.Current.SetValue(ConfigurationKeys.EnableBorderEffect, value);
    }

    partial void OnEnableHideDuplicateCardsChanged(bool value)
    {
        ConfigurationManager.Current.SetValue(ConfigurationKeys.EnableHideDuplicateCards, value);
    }

    partial void OnEnableCardSystemChanged(bool value)
    {
        ConfigurationManager.Current.SetValue(ConfigurationKeys.EnableCardSystem, value);
    }

    [ObservableProperty] private bool _isRunning;

    partial void OnIsRunningChanged(bool value)
    {
        Idle = !value;
    }

    public static string Version
    {
        get
        {
            // var version = Assembly.GetExecutingAssembly().GetName().Version;
            // var major = version.Major;
            // var minor = version.Minor >= 0 ? version.Minor : 0;
            // var patch = version.Build >= 0 ? version.Build : 0;
            // return $"v{SemVersion.Parse($"{major}.{minor}.{patch}")}";
            return "v2.5.1"; // Hardcoded version for now, replace with dynamic versioning later
        }
    }

    [ObservableProperty] private Action? _tempResourceUpdateAction;
   
    [ObservableProperty] private string? _windowUpdateInfo = "";

    [ObservableProperty] private string? _resourceName;

    [ObservableProperty] private bool _isResourceNameVisible;

    [ObservableProperty] private string? _resourceVersion;

    [ObservableProperty] private string? _customTitle;

    [ObservableProperty] private bool _isCustomTitleVisible;

    [ObservableProperty] private bool _lockController;


    [ObservableProperty] private bool _isDebugMode = ConfigurationManager.Maa.GetValue(ConfigurationKeys.Recording, false)
        || ConfigurationManager.Maa.GetValue(ConfigurationKeys.SaveDraw, false)
        || ConfigurationManager.Maa.GetValue(ConfigurationKeys.ShowHitDraw, false);
    private bool _shouldTip = true;
    [ObservableProperty] private bool _isUpdating;

    [ObservableProperty] private string _needCardNameInput = ConfigurationManager.Current.GetValue(ConfigurationKeys.NeedCardName, string.Empty);
    [ObservableProperty] private bool? _isNeedCardNameValid = null;

    partial void OnNeedCardNameInputChanged(string value)
    {
        ConfigurationManager.Current.SetValue(ConfigurationKeys.NeedCardName, value ?? string.Empty);
        var trimmed = value?.Trim() ?? string.Empty;
        
        if (string.IsNullOrEmpty(trimmed))
        {
            IsNeedCardNameValid = null;
            CCMgr.Instance.IsCardNameValid(string.Empty); // Reset CCMgr state
        }
        else
        {
            // 使用 CCMgr 提供的逻辑判定文本是否有效（即卡片名称是否存在）
            IsNeedCardNameValid = CCMgr.Instance.IsCardNameValid(trimmed);
        }
    }
    
    [RelayCommand]
    private void TryUpdate()
    {
        TempResourceUpdateAction?.Invoke();
    }
    
    partial void OnLockControllerChanged(bool value)
    {
        if (value)
        {
            Instances.TaskQueueViewModel.ShouldShow = (int)(MaaProcessor.Interface?.Controller?.FirstOrDefault()?.Type).ToMaaControllerTypes(Instances.TaskQueueViewModel.CurrentController);
        }
    }

    public void CheckDebug()
    {
        if (IsDebugMode && _shouldTip && !MaaProcessor.Instance.IsV3)
        {
            DispatcherHelper.PostOnMainThread(() =>
            {
                Instances.DialogManager.CreateDialog().OfType(NotificationType.Warning).WithContent(LangKeys.DebugModeWarning.ToLocalization()).WithActionButton(LangKeys.Ok.ToLocalization(), dialog => { }, true).TryShow();
                _shouldTip = false;
            });
        }
    }

    public void SetUpdating(bool isUpdating)
    {
        IsUpdating = isUpdating;
    }
    
    partial void OnIsDebugModeChanged(bool value)
    {
        if (value)
            CheckDebug();
    }
    private string _resourceNameKey = "";
    private string _resourceFallbackKey = "";
    private string _customTitleKey = "";
    private string _customTitleFallbackKey = "";
    public void ShowResourceName(string name)
    {
        ResourceName = name;
        IsResourceNameVisible = true;

    }

    public void ShowResourceKeyAndFallBack(string? key, string? fallback)
    {
        _resourceNameKey = key ?? string.Empty;
        _resourceFallbackKey = fallback ?? string.Empty;
        UpdateName();
        LanguageHelper.LanguageChanged += (_, __) => UpdateName();
        IsResourceNameVisible = true;
    }

    public void UpdateName()
    {
        var result = LanguageHelper.GetLocalizedDisplayName(_resourceNameKey, _resourceFallbackKey);
        if (result.Equals("debug", StringComparison.OrdinalIgnoreCase))
            IsResourceNameVisible = false;
        else
            ResourceName = result;
    }

    public void ShowResourceVersion(string version)
    {
        version = version.StartsWith("v") ? version : "v" + version;
        ResourceVersion = version;
    }

    public void ShowCustomTitle(string title)
    {
        CustomTitle = title;
        IsCustomTitleVisible = true;
        IsResourceNameVisible = false;
    }

    public void ShowCustomTitleAndFallBack(string? key, string? fallback)
    {
        _customTitleKey = key ?? string.Empty;
        _customTitleFallbackKey = fallback ?? string.Empty;
        UpdateCustomTitle();
        LanguageHelper.LanguageChanged += (_, __) => UpdateCustomTitle();
        if (!string.IsNullOrWhiteSpace(CustomTitle))
        {
            IsCustomTitleVisible = true;
            IsResourceNameVisible = false;
        }
    }

    public void UpdateCustomTitle()
    {
        CustomTitle = LanguageHelper.GetLocalizedDisplayName(_customTitleKey, _customTitleFallbackKey);
    }

    [RelayCommand]
    public void ToggleVisible()
    {
        IsWindowVisible = !IsWindowVisible;
    }
}

using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using FH6Mod.Services;
using FH6Mod.ViewModels.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace FH6Mod.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly GameProcessService _gameProcess;

    [ObservableProperty]
    private string _gameStatusText = "FH6 disconnected";

    [ObservableProperty]
    private bool _isGameAttached;

    public UnlocksViewModel UnlocksPage { get; }
    public DatabaseViewModel DatabasePage { get; }
    public SettingsViewModel SettingsPage { get; }

    public string CurrentVersionText => $"v{App.Services.GetRequiredService<UpdateCheckService>().CurrentVersion.ToString(3)}";

    public MainWindowViewModel()
        : this(App.Services.GetRequiredService<GameProcessService>())
    {
    }

    public MainWindowViewModel(GameProcessService gameProcess)
    {
        _gameProcess = gameProcess;
        _gameProcess.StatusChanged += OnGameStatusChanged;
        OnGameStatusChanged();

        UnlocksPage = App.Services.GetRequiredService<UnlocksViewModel>();
        DatabasePage = App.Services.GetRequiredService<DatabaseViewModel>();
        SettingsPage = App.Services.GetRequiredService<SettingsViewModel>();
    }

    private void OnGameStatusChanged()
    {
        Dispatcher.UIThread.Post(() =>
        {
            IsGameAttached = _gameProcess.IsAttached;
            GameStatusText = _gameProcess.IsAttached
                ? $"FH6 connected · PID {_gameProcess.Pid}"
                : "FH6 disconnected";
        });
    }
}

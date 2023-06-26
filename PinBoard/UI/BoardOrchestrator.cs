using System.Reactive.Disposables;
using System.Windows.Input;
using DynamicData;
using Eto.Forms;
using PinBoard.Models;
using PinBoard.Services;
using PinBoard.Util;
using ReactiveUI;

namespace PinBoard.UI;

public sealed class BoardOrchestrator : IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    private readonly Board _board;
    private readonly BoardControlContainer _container;
    private readonly PanZoomModel _viewModel = new();
    private readonly SerialDisposable _currentViewHider;
    private readonly BoardViewManager _boardViewManager;
    private readonly CropViewManager _cropViewManager;
    private readonly ReactiveValue<object?> _currentView = new();
    private readonly RelayCommand<object?> _pullForwardCommand;
    private readonly RelayCommand<object?> _pushBackCommand;
    private readonly RelayCommand<object?> _cropCommand;
    private readonly RelayCommand<object?> _deleteCommand;

    public BoardOrchestrator(
        Board board,
        IBoardPinFactory boardPinFactory,
        BoardControlContainer container,
        Settings settings)
    {
        _board = board;
        _container = container;
        _pullForwardCommand = new RelayCommand<object?>(OnPullForward, CanPullForward);
        _pushBackCommand = new RelayCommand<object?>(OnPushBack, CanPushBack);
        _cropCommand = new RelayCommand<object?>(OnCrop, CanCrop);
        _deleteCommand = new RelayCommand<object?>(OnDelete, CanDelete);

        new PanZoomController(_viewModel, _container)
            .DisposeWith(_disposables);
        _currentViewHider = new SerialDisposable()
            .DisposeWith(_disposables);
        _boardViewManager = new BoardViewManager(
                board,
                boardPinFactory,
                _viewModel,
                PullForwardCommand,
                PushBackCommand,
                CropCommand,
                DeleteCommand,
                settings)
            .DisposeWith(_disposables);
        _cropViewManager = new CropViewManager(_viewModel, settings)
            .DisposeWith(_disposables);

        _currentView.Value = _boardViewManager;
        _currentViewHider.Disposable = _boardViewManager.Show(container);

        _currentView.WhenAnyValue(x => x.Value)
            .Subscribe(
                _ =>
                {
                    _pullForwardCommand.UpdateCanExecute();
                    _pushBackCommand.UpdateCanExecute();
                    _cropCommand.UpdateCanExecute();
                    _deleteCommand.UpdateCanExecute();
                })
            .DisposeWith(_disposables);
        _boardViewManager.WhenAnyValue(x => x.Focused)
            .Subscribe(
                _ =>
                {
                    _pullForwardCommand.UpdateCanExecute();
                    _pushBackCommand.UpdateCanExecute();
                    _cropCommand.UpdateCanExecute();
                    _deleteCommand.UpdateCanExecute();
                })
            .DisposeWith(_disposables);
        _board.Pins.Connect()
            .SubscribeMany(x => x.Updates.Subscribe(_ => _cropCommand.UpdateCanExecute()))
            .Subscribe(
                _ =>
                {
                    _pullForwardCommand.UpdateCanExecute();
                    _pushBackCommand.UpdateCanExecute();
                })
            .DisposeWith(_disposables);
    }

    public ICommand PullForwardCommand => _pullForwardCommand;
    public ICommand PushBackCommand => _pushBackCommand;
    public ICommand CropCommand => _cropCommand;
    public ICommand DeleteCommand => _deleteCommand;
    public PanZoomModel ViewModel => _viewModel;

    public void Dispose()
    {
        _disposables.Dispose();
    }

    private BoardPin? GetCommandPin(object? o)
    {
        if (_currentView.Value != _boardViewManager)
            return null;

        return o switch
        {
            null => _boardViewManager.Focused,
            BoardPin p => p,
            _ => null
        };
    }

    private int GetPinIndex(object? o)
    {
        var boardPin = GetCommandPin(o);
        if (boardPin == null)
            return -1;
        return _board.Pins.Items.IndexOf(boardPin.PinViewModel.Pin);
    }

    private bool CanPullForward(object? o)
    {
        var index = GetPinIndex(o);
        return index != -1 && index < _board.Pins.Count - 1;
    }

    private void OnPullForward(object? o)
    {
        var index = GetPinIndex(o);
        if (index != -1 && index < _board.Pins.Count - 1)
            _board.Pins.Move(index, index + 1);
    }

    private bool CanPushBack(object? o) => GetPinIndex(o) > 0;

    private void OnPushBack(object? o)
    {
        var index = GetPinIndex(o);
        if (index > 0)
            _board.Pins.Move(index, index - 1);
    }

    private bool CanCrop(object? o) => GetCommandPin(o)?.PinViewModel.Image != null;

    private void OnCrop(object? o)
    {
        var boardPin = GetCommandPin(o);
        if (boardPin?.PinViewModel.Image == null)
            return;

        var doneSubscription = _cropViewManager.Done.Subscribe(_ => OnEdit());
        var hideDisposable = _cropViewManager.Show(boardPin.PinViewModel, _container);
        _currentView.Value = _cropViewManager;
        _currentViewHider.Disposable = new CompositeDisposable(doneSubscription, hideDisposable);
    }

    private bool CanDelete(object? o) => GetCommandPin(o) != null;

    private void OnDelete(object? o)
    {
        if (GetCommandPin(o) is {} boardPin)
            _board.Pins.Remove(boardPin.PinViewModel.Pin);
    }

    private void OnEdit()
    {
        _currentView.Value = _boardViewManager;
        _currentViewHider.Disposable = _boardViewManager.Show(_container);
    }
}

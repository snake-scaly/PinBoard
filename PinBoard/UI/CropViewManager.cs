using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Eto.Drawing;
using Eto.Forms;
using PinBoard.Models;
using PinBoard.Util;
using PinBoard.ViewModels;
using ReactiveUI;

namespace PinBoard.UI;

public sealed class CropViewManager : IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    
    private readonly PanZoomModel _viewModel;
    private readonly Settings _settings;

    private readonly Subject<Unit> _doneSubject = new();
    private readonly RelayCommand _applyCommand;
    private readonly RelayCommand _cancelCommand;
    private readonly BoardButton _confirmButton;
    private readonly BoardButton _cancelButton;

    private PinViewModel? _pinViewModel;
    private BoardPinCropEditor? _editor;

    public CropViewManager(PanZoomModel viewModel, Settings settings)
    {
        _viewModel = viewModel;
        _settings = settings;
        
        _applyCommand = new RelayCommand(OnApply);
        _cancelCommand = new RelayCommand(OnCancel);

        _confirmButton = new BoardButton("ok-icon.png", _settings, _applyCommand) { Size = new SizeF(30, 30) }
            .DisposeWith(_disposables);
        _cancelButton = new BoardButton("delete-icon.png", _settings, _cancelCommand) { Size = new SizeF(30, 30) }
            .DisposeWith(_disposables);

        _viewModel.Changed.Subscribe(_ => UpdateButtons())
            .DisposeWith(_disposables);
    }

    public IObservable<Unit> Done => _doneSubject.AsObservable();

    public void Dispose()
    {
        _disposables.Dispose();
    }

    public IDisposable Show(PinViewModel pinViewModel, BoardControlContainer controlContainer)
    {
        _pinViewModel = pinViewModel;
        _editor = new BoardPinCropEditor(pinViewModel, _viewModel, _applyCommand, _cancelCommand, _settings)
            .DisposeWith(_disposables);

        controlContainer.BoardControls.Edit(
            controls =>
            {
                controls.Add(_editor);
                controls.Add(_confirmButton);
                controls.Add(_cancelButton);
            });

        var resizeBinding = controlContainer.Bind(
            x => x.Size,
            new DelegateBinding<Size>(setValue: OnContainerResize),
            DualBindingMode.OneWayToSource);
        var resizeSubscription = new CallbackDisposable(() => resizeBinding.Unbind());

        var cropChangeSubscription = _editor.WhenAnyValue(x => x.CropRect)
            .Subscribe(_ => UpdateButtons());

        var hideDisposable = new CallbackDisposable(
            () =>
            {
                controlContainer.BoardControls.Edit(
                    controls =>
                    {
                        controls.Remove(_editor);
                        controls.Remove(_confirmButton);
                        controls.Remove(_cancelButton);
                    });

                _disposables.Remove(_editor);
                _pinViewModel = null;
                _editor = null;
            });

        return new CompositeDisposable(resizeSubscription, cropChangeSubscription, hideDisposable);
    }

    private void OnApply()
    {
        if (_pinViewModel == null)
            return;

        var pinCropRect = (_editor!.CropRect - _editor.ImageRect.TopLeft) / _pinViewModel.Pin.Scale!.Value;
        pinCropRect.Restrict(_pinViewModel.Image!.Size);
        _pinViewModel.Pin.Edit(
            pin =>
            {
                pin.CropRect = (_editor.CropRect - _editor.ImageRect.TopLeft) / _pinViewModel.Pin.Scale;
                pin.Center = _editor.CropRect.Center;
            });

        _doneSubject.OnNext(Unit.Default);
    }

    private void OnCancel()
    {
        _doneSubject.OnNext(Unit.Default);
    }

    private void OnContainerResize(Size size)
    {
        _editor!.Location = default;
        _editor.Size = size;
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        if (_editor == null)
            return;

        var viewCropRect = _viewModel.BoardViewTransform.TransformRectangle(_editor.CropRect);
        var anchor = viewCropRect.MiddleTop - new SizeF(0, _settings.DragMargin);
        ButtonPlacementHelper.PlaceButtons(anchor, _editor.Bounds, _confirmButton, _cancelButton);
    }
}

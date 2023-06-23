using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData;
using Eto.Drawing;
using Eto.Forms;
using PinBoard.Models;
using PinBoard.Services;
using PinBoard.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Settings = PinBoard.Models.Settings;

namespace PinBoard.UI;

public sealed class BoardViewManager : ReactiveObject, IDisposable
{
    private readonly Board _board;
    private readonly PanZoomModel _viewModel;
    private readonly CompositeDisposable _disposables = new();

    private readonly IObservableList<BoardPin> _displayList;
    private readonly SerialDisposable _focusSubscription;
    private IObservableListFocusController<BoardPin> _focusController = null!;
    private BoardControlContainer? _controlContainer;

    private readonly BoardButton _cropButton;
    private readonly BoardButton _deleteButton;

    public BoardViewManager(
        Board board,
        IBoardPinFactory pinFactory,
        PanZoomModel viewModel,
        ICommand pullForwardCommand,
        ICommand pushBackCommand,
        ICommand cropCommand,
        ICommand deleteCommand,
        Settings settings)
    {
        _board = board;
        _viewModel = viewModel;
        _focusSubscription = new SerialDisposable().DisposeWith(_disposables);

        _cropButton = new BoardButton("crop-icon.png", settings, cropCommand) { Size = new SizeF(30, 30) }
            .DisposeWith(_disposables);
        _deleteButton = new BoardButton("delete-icon.png", settings, deleteCommand) { Size = new SizeF(30, 30) }
            .DisposeWith(_disposables);

        _displayList = board.Pins.Connect()
            .Transform(pin => pinFactory.CreateBoardPin(
                pin, viewModel, pullForwardCommand, pushBackCommand, cropCommand, deleteCommand))
            .SubscribeMany(
                bp =>
                {
                    var click = bp.Click.Subscribe(_ => SetFocus(bp));
                    var dispose = new CallbackDisposable(() => OnRemovePin(bp));
                    return new CompositeDisposable(click, dispose, bp);
                })
            .Focus(x => _focusController = x)
            .AsObservableList()
            .DisposeWith(_disposables);
    }

    [Reactive]
    public BoardPin? Focused { get; private set; }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    public IDisposable Show(BoardControlContainer container)
    {
        _controlContainer = container;
        _controlContainer.AllowDrop = true;

        var displayListPopulator = _displayList.Connect()
            .Cast(x => (BoardControl)x)
            .PopulateInto(container.BoardControls);

        if (Focused != null)
            TrackFocus();

        var misclickSubscription = container.Misclick.Subscribe(_ => SetFocus(null));

        var resizeBinding = _controlContainer.Bind(
            x => x.Size,
            new DelegateBinding<Size>(setValue: _ => UpdateButtons()),
            DualBindingMode.OneWayToSource);
        var resizeSubscription = new CallbackDisposable(() => resizeBinding.Unbind());

        var dragEnterSubscription =
            Observable.FromEventPattern<DragEventArgs>(_controlContainer, nameof(_controlContainer.DragEnter))
                .Subscribe(e => OnDragEnter(e.Sender, e.EventArgs));
        var dragDropSubscription =
            Observable.FromEventPattern<DragEventArgs>(_controlContainer, nameof(_controlContainer.DragDrop))
                .Subscribe(e => OnDragDrop(e.Sender, e.EventArgs));

        var hideDisposable = new CallbackDisposable(
            () =>
            {
                _controlContainer.AllowDrop = false;
                _controlContainer.BoardControls.RemoveRange(0, _displayList.Count);
                HideFocus();
                _controlContainer = null;
            });

        return new CompositeDisposable(
            displayListPopulator,
            misclickSubscription,
            resizeSubscription,
            dragEnterSubscription,
            dragDropSubscription,
            hideDisposable);
    }

    private void SetFocus(BoardPin? pin)
    {
        if (Focused != null)
        {
            Focused.SetFocus(false);
            _focusController.ResetFocus();
        }

        Focused = pin;

        if (Focused != null)
        {
            Focused.SetFocus(true);
            _focusController.SetFocus(Focused);
            _focusSubscription.Disposable = Focused.Invalidated.Subscribe(_ => TrackFocus());
            TrackFocus();
        }
        else
        {
            _focusSubscription.Disposable = null;
            HideFocus();
        }
    }

    private void TrackFocus()
    {
        if (_controlContainer == null)
            return;

        if (_controlContainer.BoardControls.Items.Contains(_deleteButton) == false)
        {
            // These buttons will stay on top due to the way observable list synchronization works.
            _controlContainer.BoardControls.Edit(
                l =>
                {
                    l.Add(_cropButton);
                    l.Add(_deleteButton);
                });
        }

        UpdateButtons();
    }

    private void UpdateButtons()
    {
        if (_controlContainer == null || Focused == null)
            return;

        ButtonPlacementHelper.PlaceButtons(
            Focused.Bounds.MiddleTop, new RectangleF(_controlContainer.ClientSize), _cropButton, _deleteButton);
    }

    private void HideFocus()
    {
        _controlContainer?.BoardControls.Edit(
            l =>
            {
                l.Remove(_cropButton);
                l.Remove(_deleteButton);
            });
    }

    private void OnRemovePin(BoardPin bp)
    {
        if (bp == Focused)
            SetFocus(null);
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        e.Effects = e.AllowedEffects & DragEffects.Copy | DragEffects.Link;
    }

    private void OnDragDrop(object? sender, DragEventArgs e)
    {
        var control = sender as Control ?? throw new InvalidOperationException("Sender is not a control");

        var boardLocation = _viewModel.ViewBoardTransform.TransformPoint(e.Location);
        var boardViewport = _viewModel.ViewBoardTransform.TransformRectangle(new RectangleF(default, control.Size));

        if (e.Data.ContainsImage)
        {
            _board.Add(e.Data.Image, boardViewport, boardLocation);
        }
        else if (e.Data.ContainsUris)
        {
            if (e.Data.Uris.Length == 1)
                _board.Add(e.Data.Uris[0], boardViewport, boardLocation);
            else
                foreach (var uri in e.Data.Uris)
                    _board.Add(uri, boardViewport);
        }
        else if (e.Data.ContainsText)
        {
            _board.Add(new Uri(e.Data.Text), boardViewport, boardLocation);
        }
    }
}

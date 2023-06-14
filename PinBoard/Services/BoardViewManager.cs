using System.Reactive.Disposables;
using DynamicData;
using Eto.Drawing;
using PinBoard.Models;
using PinBoard.UI;
using PinBoard.Util;

namespace PinBoard.Services;

public sealed class BoardViewManager : IDisposable
{
    private readonly BoardControlContainer _controlContainer;
    private readonly PanZoomModel _viewModel = new();
    private readonly SerialDisposable _focusSubscription;
    private readonly CompositeDisposable _disposables = new();
    private IObservableListFocusController<BoardPin> _focusController = null!;

    private readonly BoardButton _closeButton;
    private BoardPin? _focused;

    public BoardViewManager(BoardControlContainer controlContainer, Board board, IBoardPinFactory pinFactory, Settings settings)
    {
        _controlContainer = controlContainer;
        _focusSubscription = new SerialDisposable().DisposeWith(_disposables);

        new PanZoomController(_viewModel, _controlContainer)
            .DisposeWith(_disposables);

        _closeButton = new BoardButton(settings) { Location = new PointF(10, 10), Size = new SizeF(40, 30) }
            .DisposeWith(_disposables);

        _closeButton.Click.Subscribe(
            _ =>
            {
                if (_focused != null)
                    board.Pins.Remove(_focused.Pin);
            });

        board.Pins.Connect()
            .Transform(pin => pinFactory.CreateBoardPin(pin, _viewModel))
            .SubscribeMany(
                bp =>
                {
                    var click = bp.Click.Subscribe(_ => SetFocus(bp));
                    var dispose = new CallbackDisposable(() => OnRemovePin(bp));
                    return new CompositeDisposable(click, dispose, bp);
                })
            .Focus(x => _focusController = x)
            .Cast(x => (BoardControl)x)
            .PopulateInto(_controlContainer.BoardControls)
            .DisposeWith(_disposables);

        controlContainer.Misclick.Subscribe(_ => SetFocus(null))
            .DisposeWith(_disposables);
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    private void SetFocus(BoardPin? pin)
    {
        if (_focused != null)
        {
            _focused.SetFocus(false);
            _focusController.ResetFocus();
        }

        _focused = pin;

        if (_focused != null)
        {
            _focused.SetFocus(true);
            _focusController.SetFocus(_focused);
            _focusSubscription.Disposable = _focused.Invalidated.Subscribe(_ => TrackFocus());
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
        if (!_controlContainer.BoardControls.Items.Contains(_closeButton))
        {
            // This button will stay on top due to the way observable list synchronization works.
            _controlContainer.BoardControls.Add(_closeButton);
        }

        var pinBounds = _focused!.Bounds;
        var buttonBounds = _closeButton.Bounds;

        var buttonPos = new PointF(
            pinBounds.Left + pinBounds.Width / 2 - buttonBounds.Width / 2,
            pinBounds.Top - buttonBounds.Height);

        var screenBounds = new RectangleF(_controlContainer.Size);
        screenBounds.Inset(10);
        screenBounds.BottomRight -= buttonBounds.Size;
        buttonPos.Restrict(screenBounds);

        _closeButton.Location = buttonPos;
        _closeButton.Invalidate();
    }

    private void HideFocus()
    {
        _controlContainer.BoardControls.Remove(_closeButton);
    }

    private void OnRemovePin(BoardPin bp)
    {
        if (bp == _focused)
            SetFocus(null);
    }
}

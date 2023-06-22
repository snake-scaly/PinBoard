using System.Windows.Input;
using PinBoard.Models;
using PinBoard.UI;

namespace PinBoard.Services;

public class BoardPinFactory : IBoardPinFactory
{
    private readonly IPinViewModelFactory _viewModelFactory;
    private readonly Settings _settings;

    public BoardPinFactory(IPinViewModelFactory viewModelFactory, Settings settings)
    {
        _viewModelFactory = viewModelFactory;
        _settings = settings;
    }

    public BoardPin CreateBoardPin(
        Pin pin,
        PanZoomModel viewModel,
        ICommand pullForwardCommand,
        ICommand pushBackCommand,
        ICommand cropCommand,
        ICommand deleteCommand)
    {
        return new BoardPin(
            _viewModelFactory.CreatePinViewModel(pin),
            viewModel,
            pullForwardCommand,
            pushBackCommand,
            cropCommand,
            deleteCommand,
            _settings);
    }
}

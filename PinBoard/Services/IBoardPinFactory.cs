using System.Windows.Input;
using PinBoard.Models;
using PinBoard.UI;

namespace PinBoard.Services;

public interface IBoardPinFactory
{
    BoardPin CreateBoardPin(
        Pin pin,
        PanZoomModel viewModel,
        ICommand pullForwardCommand,
        ICommand pushBackCommand,
        ICommand cropCommand,
        ICommand deleteCommand);
}

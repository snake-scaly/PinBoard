using PinBoard.Models;
using PinBoard.UI;

namespace PinBoard.Services;

public interface IBoardPinFactory
{
    BoardPin CreateBoardPin(Pin pin, PanZoomModel viewModel);
}

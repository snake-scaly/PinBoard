using PinBoard.Controls;
using PinBoard.Models;
using PinBoard.ViewModels;

namespace PinBoard.Services;

public interface IEditModeFactory
{
    BoardEditMode CreateBoardEditMode(Board board, PanZoomModel viewModel);
    CropEditMode CreateCropEditMode(PanZoomModel viewModel, PinViewModel pinViewModel, IEditMode previousMode);
}

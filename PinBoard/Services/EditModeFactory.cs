using PinBoard.Controls;
using PinBoard.Models;
using PinBoard.ViewModels;

namespace PinBoard.Services;

public class EditModeFactory : IEditModeFactory
{
    private readonly Settings _settings;
    private readonly IPinViewModelFactory _pinViewModelFactory;

    public EditModeFactory(Settings settings, IPinViewModelFactory pinViewModelFactory)
    {
        _settings = settings;
        _pinViewModelFactory = pinViewModelFactory;
    }

    public BoardEditMode CreateBoardEditMode(Board board, PanZoomModel viewModel)
    {
        return new BoardEditMode(board, viewModel, _settings, this, _pinViewModelFactory);
    }

    public CropEditMode CreateCropEditMode(PanZoomModel viewModel, PinViewModel pinViewModel, IEditMode previousMode)
    {
        return new CropEditMode(viewModel, pinViewModel, _settings, previousMode);
    }
}

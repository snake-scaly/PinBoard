using PinBoard.Models;
using PinBoard.ViewModels;

namespace PinBoard.Services;

public interface IPinViewModelFactory
{
    PinViewModel CreatePinViewModel(Pin pin);
}

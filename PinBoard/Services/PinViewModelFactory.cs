using PinBoard.Models;
using PinBoard.ViewModels;

namespace PinBoard.Services;

public class PinViewModelFactory : IPinViewModelFactory
{
    private readonly HttpClient _httpClient;

    public PinViewModelFactory(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public PinViewModel CreatePinViewModel(Pin pin)
    {
        return new PinViewModel(pin, _httpClient);
    }
}

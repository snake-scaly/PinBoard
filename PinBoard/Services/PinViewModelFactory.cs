using Microsoft.Extensions.Logging;
using PinBoard.Models;
using PinBoard.ViewModels;

namespace PinBoard.Services;

public class PinViewModelFactory : IPinViewModelFactory
{
    private readonly HttpClient _httpClient;
    private readonly ILoggerFactory _loggerFactory;

    public PinViewModelFactory(HttpClient httpClient, ILoggerFactory loggerFactory)
    {
        _httpClient = httpClient;
        _loggerFactory = loggerFactory;
    }

    public PinViewModel CreatePinViewModel(Pin pin)
    {
        return new PinViewModel(pin, _httpClient, _loggerFactory.CreateLogger<PinViewModel>());
    }
}

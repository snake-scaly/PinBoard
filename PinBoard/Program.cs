using System.Reflection;
using Eto.Forms;
using Microsoft.Extensions.DependencyInjection;
using PinBoard;
using PinBoard.Models;
using PinBoard.Rx;
using PinBoard.Services;
using ReactiveUI;
using Serilog;
using Serilog.Events;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;
using Splat.Microsoft.Extensions.Logging;
using Splat.Serilog;

var appName = Assembly.GetExecutingAssembly().GetName().Name;
var logDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
if (OperatingSystem.IsLinux())
    logDir = $"{Environment.GetEnvironmentVariable("HOME")}/.local/var/log";
if (OperatingSystem.IsWindows())
    logDir = $"{Environment.GetEnvironmentVariable("APPDATA")}/{appName}";
if (OperatingSystem.IsMacOS())
    logDir = $"{Environment.GetEnvironmentVariable("HOME")}/Library/Logs/{appName}/";

var logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.Console(LogEventLevel.Information)
    .WriteTo.File($"{logDir}/{appName}.log", fileSizeLimitBytes: 1 << 20, rollOnFileSizeLimit: true)
    .CreateLogger();

var services = new ServiceCollection();
services.UseMicrosoftDependencyResolver();

Locator.CurrentMutable.UseSerilogFullLogger(logger);
services.AddLogging(builder => builder.AddSplat());

services.AddSingleton<Settings>();
services.AddSingleton<HttpClient>();
services.AddTransient<MainWindow>();
services.AddTransient<IBoardFileService, BoardFileService>();
services.AddTransient<IEditModeFactory, EditModeFactory>();
services.AddTransient<IPinViewModelFactory, PinViewModelFactory>();

// RxApp static constructor requires a mutable Locator.
RxApp.MainThreadScheduler = new EtoMainThreadScheduler();

var provider = services.BuildServiceProvider();
provider.UseMicrosoftDependencyResolver();

var app = new Application();
app.UIThreadCheckMode = UIThreadCheckMode.Error;
app.Run(provider.GetRequiredService<MainWindow>());

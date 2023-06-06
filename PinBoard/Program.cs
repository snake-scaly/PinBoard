using System.Reflection;
using Eto.Forms;
using PinBoard;
using PinBoard.Services;
using Serilog;
using Serilog.Events;
using Splat;
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

Locator.CurrentMutable.UseSerilogFullLogger(logger);
Locator.CurrentMutable.RegisterLazySingleton(() => new HttpClient());
Locator.CurrentMutable.RegisterConstant<IBoardFileService>(new BoardFileService());

new Application().Run(new MainWindow());

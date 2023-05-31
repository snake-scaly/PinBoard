using Eto.Forms;
using PinBoard;
using Splat;

Locator.CurrentMutable.RegisterLazySingleton(() => new HttpClient());

new Application().Run(new MainWindow());

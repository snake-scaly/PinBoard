using Eto.Drawing;
using Eto.Forms;
using PinBoard.Models;
using PinBoard.Services;
using PinBoard.UI;
using PinBoard.ViewModels;

namespace PinBoard;

public class MainWindow : Form
{
    private readonly IBoardFileService _boardFileService;
    private readonly Board _board;
    private readonly BoardOrchestrator _orchestrator;

    public MainWindow(IBoardFileService boardFileService, Settings settings, IBoardPinFactory bpf)
    {
        _boardFileService = boardFileService;

        ClientSize = new Size(800, 600);
        _board = new Board();

        var scaleLabel = new Label { Text = "100%" };
        var scalePanel = new Panel { Content = scaleLabel, Padding = new Padding(6, 4) };
        var toolbar = new TableLayout(
            new TableRow(
                new TableCell(new Panel(), scaleWidth: true),
                new TableCell(scalePanel)));

        var bcc = new BoardControlContainer(settings);
        _orchestrator = new BoardOrchestrator(_board, bpf, bcc, settings);

        Content = new TableLayout(
            new TableRow(new TableCell(bcc)) { ScaleHeight = true },
            new TableRow(new TableCell(toolbar)));

        DataContext = new MainViewModel(_board, _orchestrator.ViewModel);

        this.BindDataContext(x => x.Title, (MainViewModel x) => x.Title, DualBindingMode.OneWay);
        scaleLabel.BindDataContext(x => x.Text, (MainViewModel x) => x.Scale, DualBindingMode.OneWay);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        var openBoardMenuItem = new Command(OnOpen) { MenuText = "&Open...", Shortcut = Keys.Control | Keys.O };
        var saveBoardMenuItem = new Command(OnSave) { Parent = this, MenuText = "&Save", Shortcut = Keys.Control | Keys.S };
        saveBoardMenuItem.BindDataContext(x => x.Enabled, (MainViewModel m) => m.EnableSave, DualBindingMode.OneWay);
        var saveBoardAsMenuItem = new Command(OnSaveAs) { MenuText = "Save &As...", Shortcut = Keys.Control | Keys.Shift | Keys.S };
        var pinFileMenuItem = new Command(OnPinFile) { MenuText = "Add &Images...", Shortcut = Keys.Control | Keys.I };
        var exitMenuItem = new Command((_, _) => Application.Instance.Quit()) { MenuText = "E&xit", Shortcut = Keys.Alt | Keys.F4 };
        var fileSubMenu = new SubMenuItem
        {
            Text = "&File",
            Items =
            {
                openBoardMenuItem,
                saveBoardMenuItem,
                saveBoardAsMenuItem,
                new SeparatorMenuItem(),
                pinFileMenuItem,
                new SeparatorMenuItem(),
                exitMenuItem,
            }
        };

        var pasteMenuItem = new Command(OnPaste) { MenuText = "&Paste", Shortcut = Keys.Control | Keys.V };
        var pullForwardMenuItem = new Command { MenuText = "Pull &Forward", DelegatedCommand = _orchestrator.PullForwardCommand };
        var pushBackMenuItem = new Command { MenuText = "Push &Back", DelegatedCommand = _orchestrator.PushBackCommand };
        var cropMenuItem = new Command { MenuText = "&Crop", DelegatedCommand = _orchestrator.CropCommand };
        var deleteMenuItem = new Command { MenuText = "&Delete", DelegatedCommand = _orchestrator.DeleteCommand };
        var editSubMenu = new SubMenuItem
        {
            Text = "&Edit",
            Items =
            {
                pasteMenuItem,
                new SeparatorMenuItem(),
                pullForwardMenuItem,
                pushBackMenuItem,
                cropMenuItem,
                deleteMenuItem,
            }
        };

        Menu = new MenuBar(fileSubMenu, editSubMenu);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _board.Dispose();
        base.Dispose(disposing);
    }

    private void OnOpen(object? sender, EventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            Title = "Open Board",
            Filters = { new FileFilter("Boards", "*.pinboard"), new FileFilter("All", "*") },
            CheckFileExists = true,
        };

        var result = ofd.ShowDialog(this);
        if (result != DialogResult.Ok)
            return;

        _boardFileService.Load(_board, ofd.FileName);
    }

    private void OnSave(object? sender, EventArgs e)
    {
        if (_board.Filename != null)
            _boardFileService.Save(_board, _board.Filename);
        else
            OnSaveAs(sender, e);
    }

    private void OnSaveAs(object? sender, EventArgs e)
    {
        var sfd = new SaveFileDialog
        {
            Title = "Save Board",
            Filters = { new FileFilter("Boards", "*.pinboard") },
        };

        var result = sfd.ShowDialog(this);
        if (result != DialogResult.Ok)
            return;

        var filename = sfd.FileName;
        if (!Path.HasExtension(filename))
            filename += ".pinboard";

        _boardFileService.Save(_board, filename);
    }

    private void OnPinFile(object? sender, EventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            Title = "Pin Files",
            Filters = { new FileFilter("All", "*") },
            CheckFileExists = true,
            MultiSelect = true,
        };
        var result = ofd.ShowDialog(this);
        if (result != DialogResult.Ok)
            return;
        List<string> errors = new();
        foreach (var filename in ofd.Filenames)
        {
            try
            {
                _board.Add(new Uri(filename), new RectangleF(ClientSize));
            }
            catch (Exception exception)
            {
                errors.Add(exception.Message);
            }
        }
        if (errors.Any())
            MessageBox.Show(this, string.Join("\n", errors), "Couldn't open some files", MessageBoxType.Warning);
    }

    private void OnPaste(object? sender, EventArgs e)
    {
        if (Clipboard.Instance.ContainsUris)
            foreach (var uri in Clipboard.Instance.Uris)
                _board.Add(uri, new RectangleF(ClientSize));
    }
}

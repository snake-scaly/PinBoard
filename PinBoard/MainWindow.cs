using Eto.Drawing;
using Eto.Forms;
using PinBoard.Controls;
using PinBoard.Models;
using PinBoard.Services;
using PinBoard.ViewModels;
using ReactiveUI;

namespace PinBoard;

public class MainWindow : Form
{
    private readonly IBoardFileService _boardFileService;
    private readonly BoardView _boardView;
    private readonly Board _board;

    public MainWindow(IBoardFileService boardFileService, IEditModeFactory editModeFactory)
    {
        _boardFileService = boardFileService;

        ClientSize = new Size(800, 600);
        _board = new Board();
        _boardView = new BoardView(_board, editModeFactory);
        DataContext = new MainViewModel(_board);
        this.BindDataContext(x => x.Title, (MainViewModel x) => x.Title, DualBindingMode.OneWay);

        var scaleLabel = new Label { Text = "100%" };
        var scalePanel = new Panel { Content = scaleLabel, Padding = new Padding(6, 4) };
        var toolbar = new TableLayout(
            new TableRow(
                new TableCell(new Panel(), scaleWidth: true),
                new TableCell(scalePanel)));

        Content = new TableLayout(
            new TableRow(new TableCell(_boardView)) { ScaleHeight = true },
            new TableRow(new TableCell(toolbar)));

        _boardView.WhenAnyValue(x => x.ViewModel.Scale).Subscribe(x => scaleLabel.Text = x.ToString("P1"));
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        var openBoardCommand = new Command(OnOpen) { MenuText = "&Open...", Shortcut = Keys.Control | Keys.O };
        var saveBoardCommand = new Command(OnSave) { Parent = this, MenuText = "&Save", Shortcut = Keys.Control | Keys.S };
        saveBoardCommand.BindDataContext(x => x.Enabled, (MainViewModel m) => m.EnableSave, DualBindingMode.OneWay);
        var saveBoardAsCommand = new Command(OnSaveAs) { MenuText = "Save &As...", Shortcut = Keys.Control | Keys.Shift | Keys.S };
        var exitCommand = new Command((_, _) => Application.Instance.Quit()) { MenuText = "E&xit", Shortcut = Keys.Alt | Keys.F4 };
        var fileSubMenu = new SubMenuItem { Text = "&File", Items = { openBoardCommand, saveBoardCommand, saveBoardAsCommand, new SeparatorMenuItem(), exitCommand } };
        var pinFileCommand = new Command(OnPinFile) { MenuText = "Pin F&iles...", Shortcut = Keys.Control | Keys.I };
        var pasteCommand = new Command(OnPaste) { MenuText = "&Paste", Shortcut = Keys.Control | Keys.V };
        var boardSubMenu = new SubMenuItem { Text = "&Board", Items = { pinFileCommand, pasteCommand } };

        Menu = new MenuBar(fileSubMenu, boardSubMenu);
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
                _boardView.Add(new Uri(filename));
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
                _boardView.Add(uri);
    }
}

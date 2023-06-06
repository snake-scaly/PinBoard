using Eto.Drawing;
using Eto.Forms;
using PinBoard.Services;
using PinBoard.Util;
using ReactiveUI;
using Splat;

namespace PinBoard;

public class MainWindow : Form
{
    private readonly IBoardFileService _boardFileService;
    private Settings _settings = new();
    private readonly BoardView _boardView;
    private readonly Board _board;

    public MainWindow(IBoardFileService? boardFileService = null)
    {
        _boardFileService = boardFileService ?? Locator.Current.GetRequiredService<IBoardFileService>();

        ClientSize = new Size(800, 600);
        _board = new Board();
        _boardView = new BoardView(_board, _settings);

        var scaleLabel = new Label { Text = "100%",  };
        var scalePanel = new Panel { Content = scaleLabel, Padding = new Padding(6, 4) };
        var toolbar = new TableLayout(
            new TableRow(
                new TableCell(new Panel(), scaleWidth: true),
                new TableCell(scalePanel)));

        Content = new TableLayout(
            new TableRow(new TableCell(_boardView)) { ScaleHeight = true },
            new TableRow(new TableCell(toolbar)));

        var openBoardCommand = new Command(OnOpen) { MenuText = "&Open...", Shortcut = Keys.Control | Keys.O };
        var saveBoardAsCommand = new Command(OnSaveAs) { MenuText = "Save &As...", Shortcut = Keys.Control | Keys.Shift | Keys.S };
        var exitCommand = new Command((_, _) => Close()) { MenuText = "E&xit", Shortcut = Keys.Alt | Keys.F4 };
        var fileSubMenu = new SubMenuItem { Text = "&File", Items = { openBoardCommand, saveBoardAsCommand, new SeparatorMenuItem(), exitCommand } };
        var pinFileCommand = new Command(OnPinFile) { MenuText = "Pin F&iles...", Shortcut = Keys.Control | Keys.I };
        var pasteCommand = new Command(OnPaste) { MenuText = "&Paste", Shortcut = Keys.Control | Keys.V };
        var boardSubMenu = new SubMenuItem { Text = "&Board", Items = { pinFileCommand, pasteCommand } };

        Menu = new MenuBar(fileSubMenu, boardSubMenu);

        _boardView.WhenAnyValue(x => x.ViewModel.Scale).Subscribe(x => scaleLabel.Text = x.ToString("P1"));
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

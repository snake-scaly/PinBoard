using Eto.Drawing;
using Eto.Forms;
using ReactiveUI;

namespace PinBoard;

public class MainWindow : Form
{
    private Settings _settings = new();
    private readonly BoardView _board;

    public MainWindow()
    {
        ClientSize = new Size(800, 600);
        _board = new BoardView(_settings);
        var scaleLabel = new Label { Text = "100%",  };
        var scalePanel = new Panel { Content = scaleLabel, Padding = new Padding(6, 4) };
        var toolbar = new TableLayout(
            new TableRow(
                new TableCell(new Panel(), scaleWidth: true),
                new TableCell(scalePanel)));

        Content = new TableLayout(
            new TableRow { Cells = { new TableCell(_board) }, ScaleHeight = true },
            new TableRow { Cells = { new TableCell(toolbar) }, ScaleHeight = false });

        var pinFileCommand = new Command(OnPinFile)
        {
            MenuText = "Pin F&iles...",
            Shortcut = Keys.Control | Keys.I,
        };

        var boardSubMenu = new SubMenuItem { Text = "&Board", Items = { pinFileCommand } };

        Menu = new MenuBar(boardSubMenu);

        _board.WhenAnyValue(x => x.ViewModel.Scale).Subscribe(x => scaleLabel.Text = x.ToString("P1"));
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
                _board.Add(new Bitmap(filename));
            }
            catch (Exception exception)
            {
                errors.Add(exception.Message);
            }
        }
        if (errors.Any())
            MessageBox.Show(this, string.Join("\n", errors), "Couldn't open some files", MessageBoxType.Warning);
    }
}

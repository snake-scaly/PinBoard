using PinBoard.Models;

namespace PinBoard.Services;

public interface IBoardFileService
{
    void Load(Board board, string filename);

    void Save(Board board, string filename);
}

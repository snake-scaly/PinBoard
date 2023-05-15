using DynamicData;

namespace PinBoard;

public class Board
{
    public SourceList<Pin> Pins { get; } = new();
}

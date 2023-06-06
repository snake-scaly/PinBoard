namespace PinBoard;

public class BoardFormatException : Exception
{
    public BoardFormatException(string? message = null, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

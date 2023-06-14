namespace PinBoard.Util;

public interface IObservableListFocusController<in T>
{
    void SetFocus(T item);

    void ResetFocus();
}

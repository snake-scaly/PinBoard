using System.Collections;
using System.Reactive.Disposables;
using DynamicData;
using DynamicData.Kernel;
using ReactiveUI;

namespace PinBoard.Util;

public static class ObservableListFocusExtensions
{
    /// <summary>
    /// Transforms an observable list such that a given item is always the last element.
    /// </summary>
    /// <param name="source">Source observable list.</param>
    /// <param name="action">Receives a controller that can be used to select the focused item.</param>
    /// <typeparam name="T">The item type.</typeparam>
    /// <returns>A rearranged observable list.</returns>
    /// <remarks>
    /// The application is expected to retain the controller it receives in the <paramref name="action"/>
    /// and use it to change the current focused item.
    /// </remarks>
    public static IObservable<IChangeSet<T>> Focus<T>(this IObservable<IChangeSet<T>> source, Action<IObservableListFocusController<T>> action)
    {
        var controller = new Controller<T>();
        action(controller);
        return new FocusObservable<T>(source, controller);
    }

    private class FocusObservable<T> : IObservable<IChangeSet<T>>
    {
        private readonly IObservable<IChangeSet<T>> _source;
        private readonly Controller<T> _controller;
        private readonly List<T> _items = new();
        private Optional<T> _focusedItem;
        private int _focusedIndex = int.MaxValue;

        public FocusObservable(IObservable<IChangeSet<T>> source, Controller<T> controller)
        {
            _source = source;
            _controller = controller;
        }

        public IDisposable Subscribe(IObserver<IChangeSet<T>> observer)
        {
            var subscription = _source.Subscribe(x => HandleChange(x, observer));
            var focus = _controller.Focus.WhenAnyValue(x => x.Value).Subscribe(x => HandleFocus(x, observer));
            return new CompositeDisposable(subscription, focus);
        }

        private void HandleChange(IChangeSet<T> change, IObserver<IChangeSet<T>> observer)
        {
            var changeSet = new ChangeSet<T>();

            foreach (var c in change)
            {
                switch (c.Reason)
                {
                    case ListChangeReason.Add:
                    {
                        _items.Insert(c.Item.CurrentIndex, c.Item.Current);
                        var i = c.Item.CurrentIndex;
                        if (_focusedItem.HasValue)
                        {
                            if (i <= _focusedIndex)
                                _focusedIndex++;
                            else
                                i--;
                        }
                        changeSet.AddChange(new Change<T>(ListChangeReason.Add, c.Item.Current, i));
                        break;
                    }

                    case ListChangeReason.AddRange:
                    {
                        _items.InsertRange(c.Range.Index, c.Range);
                        var i = c.Range.Index;
                        if (_focusedItem.HasValue)
                        {
                            if (i <= _focusedIndex)
                                _focusedIndex += c.Range.Count;
                            else
                                i--;
                        }
                        changeSet.AddChange(new Change<T>(ListChangeReason.AddRange, c.Range, i));
                        break;
                    }

                    case ListChangeReason.Replace:
                    {
                        _items[c.Item.CurrentIndex] = c.Item.Current;
                        var i = c.Item.CurrentIndex;
                        if (_focusedItem.HasValue)
                        {
                            if (i == _focusedIndex)
                            {
                                _focusedItem = default;
                                if (_focusedIndex != _items.Count - 1)
                                {
                                    changeSet.AddChange(new Change<T>(ListChangeReason.Remove, c.Item.Previous.Value, _items.Count - 1));
                                    changeSet.AddChange(new Change<T>(ListChangeReason.Add, c.Item.Current, i));
                                    break;
                                }
                            }
                            if (i > _focusedIndex)
                                i--;
                        }
                        changeSet.AddChange(new Change<T>(ListChangeReason.Replace, c.Item.Current, c.Item.Previous, i));
                        break;
                    }

                    case ListChangeReason.Remove:
                    {
                        _items.RemoveAt(c.Item.CurrentIndex);
                        var i = c.Item.CurrentIndex;
                        if (_focusedItem.HasValue)
                        {
                            if (i == _focusedIndex)
                            {
                                i = _items.Count;
                                _focusedItem = default;
                            }
                            else if (i < _focusedIndex)
                                _focusedIndex--;
                            else
                                i--;
                        }
                        changeSet.AddChange(new Change<T>(ListChangeReason.Remove, c.Item.Current, i));
                        break;
                    }

                    case ListChangeReason.RemoveRange:
                    {
                        var i = c.Range.Index;
                        if (_focusedItem.HasValue)
                        {
                            var first = c.Range.Index;
                            var last = first + c.Range.Count;
                            if (first <= _focusedIndex && last > _focusedIndex)
                            {
                                var last1 = _focusedIndex;
                                var first2 = _focusedIndex + 1;
                                var rangeItems = _items.Skip(first).Take(last1 - first).Concat(_items.Skip(first2).Take(last - first2));
                                if (last == _items.Count)
                                    rangeItems = rangeItems.Append(_focusedItem.Value);
                                else
                                    changeSet.AddChange(new Change<T>(ListChangeReason.Remove, _focusedItem.Value, _items.Count - 1));
                                changeSet.AddChange(new Change<T>(ListChangeReason.RemoveRange, rangeItems.ToList(), first));
                                _items.RemoveRange(c.Range.Index, c.Range.Count);
                                _focusedItem = default;
                                break;
                            }
                            if (i < _focusedIndex)
                                _focusedIndex -= c.Range.Count;
                            else
                                i--;
                        }
                        changeSet.AddChange(new Change<T>(ListChangeReason.RemoveRange, c.Range, i));
                        _items.RemoveRange(c.Range.Index, c.Range.Count);
                        break;
                    }

                    case ListChangeReason.Moved:
                    {
                        _items.RemoveAt(c.Item.PreviousIndex);
                        _items.Insert(c.Item.CurrentIndex, c.Item.Current);
                        var from = c.Item.PreviousIndex;
                        var to = c.Item.CurrentIndex;
                        if (from == to)
                            break;
                        if (_focusedItem.HasValue)
                        {
                            if (from == _focusedIndex)
                            {
                                _focusedIndex = to;
                                break;
                            }

                            if (from < _focusedIndex)
                                _focusedIndex--;
                            else
                                from--;
                            if (to <= _focusedIndex)
                                _focusedIndex++;
                            else
                                to--;
                        }

                        changeSet.AddChange(new Change<T>(ListChangeReason.Moved, c.Item.Current, default, to, from));
                        break;
                    }

                    case ListChangeReason.Clear:
                        if (_focusedItem.HasValue)
                        {
                            _items.RemoveAt(_focusedIndex);
                            _items.Add(_focusedItem.Value);
                            _focusedItem = default;
                        }
                        changeSet.AddChange(new Change<T>(ListChangeReason.Clear, _items.ToArray()));
                        _items.Clear();
                        break;

                    case ListChangeReason.Refresh:
                    default:
                        throw new InvalidOperationException($"Unexpected {c.Reason} from observable list");
                }

                if (changeSet.Any())
                    observer.OnNext(changeSet);
            }
        }

        private void HandleFocus(Optional<T> item, IObserver<IChangeSet<T>> observer)
        {
            if (Equals(item, _focusedItem))
                return;

            var changeSet = new ChangeSet<T>();

            if (_focusedItem.HasValue)
            {
                if (_focusedIndex != _items.Count - 1)
                    changeSet.AddChange(new Change<T>(ListChangeReason.Moved, _focusedItem.Value, default, _focusedIndex, _items.Count - 1));
                _focusedItem = default;
            }

            if (item.HasValue)
            {
                _focusedIndex = _items.IndexOf(item.Value);
                if (_focusedIndex == -1)
                    throw new InvalidOperationException("Item is not in the list");
                _focusedItem = item;
                if (_focusedIndex != _items.Count - 1)
                    changeSet.AddChange(new Change<T>(ListChangeReason.Moved, _focusedItem.Value, default, _items.Count - 1, _focusedIndex));
            }

            if (changeSet.Any())
                observer.OnNext(changeSet);
        }
    }

    private class Controller<T> : IObservableListFocusController<T>
    {
        public ReactiveValue<Optional<T>> Focus { get; } = new();

        public void SetFocus(T item)
        {
            Focus.Value = item;
        }

        public void ResetFocus()
        {
            Focus.Value = default;
        }
    }

    private class ChangeSet<T> : IChangeSet<T>
    {
        private readonly List<Change<T>> _changes = new();

        public int Count => _changes.Count;

        public int Capacity
        {
            get => _changes.Capacity;
            set => _changes.Capacity = value;
        }

        public int Adds { get; private set; }
        public int Moves { get; private set; }
        public int Refreshes { get; private set; }
        public int Removes { get; private set; }
        public int Replaced { get; private set; }
        public int TotalChanges { get; private set; }

        public void AddChange(Change<T> change)
        {
            _changes.Add(change);

            switch (change.Reason)
            {
                case ListChangeReason.Add:
                    Adds++;
                    TotalChanges++;
                    break;
                case ListChangeReason.AddRange:
                    Adds += change.Range.Count;
                    TotalChanges += change.Range.Count;
                    break;
                case ListChangeReason.Replace:
                    Replaced++;
                    TotalChanges++;
                    break;
                case ListChangeReason.Remove:
                    Removes++;
                    TotalChanges++;
                    break;
                case ListChangeReason.RemoveRange:
                    Removes += change.Range.Count;
                    TotalChanges += change.Range.Count;
                    break;
                case ListChangeReason.Refresh:
                    Refreshes++;
                    TotalChanges++;
                    break;
                case ListChangeReason.Moved:
                    Moves++;
                    TotalChanges++;
                    break;
                case ListChangeReason.Clear:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public IEnumerator<Change<T>> GetEnumerator()
        {
            return _changes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _changes.GetEnumerator();
        }
    }
}

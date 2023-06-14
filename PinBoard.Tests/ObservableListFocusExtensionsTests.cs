using System;
using DynamicData;
using PinBoard.Util;
using Xunit;

namespace PinBoard.Tests;

public sealed class ObservableListFocusExtensionsTests : IDisposable
{
    private readonly SourceList<int> _source = new();
    private readonly SourceList<int> _target = new();
    private readonly IDisposable _sub;
    private IObservableListFocusController<int> _controller;

    public ObservableListFocusExtensionsTests()
    {
        _sub = _source.Connect()
            .Focus(x => _controller = x)
            .PopulateInto(_target);
    }

    public void Dispose()
    {
        _sub.Dispose();
    }

    [Fact]
    public void Add()
    {
        _source.Add(1);
        Assert.Equal(new[] { 1 }, _target.Items);
    }

    [Fact]
    public void AddRange()
    {
        _source.AddRange(new[] { 1, 2, 3 });
        Assert.Equal(new[] { 1, 2, 3 }, _target.Items);
    }

    [Fact]
    public void Replace()
    {
        _source.AddRange(new[] { 1, 2, 3 });
        _source.Replace(2, 4);
        Assert.Equal(new[] { 1, 4, 3 }, _target.Items);
    }

    [Fact]
    public void Remove()
    {
        _source.AddRange(new[] { 1, 2, 3 });
        _source.Remove(2);
        Assert.Equal(new[] { 1, 3 }, _target.Items);
    }

    [Fact]
    public void RemoveRange()
    {
        _source.AddRange(new[] { 1, 2, 3, 4 });
        _source.RemoveRange(1, 2);
        Assert.Equal(new[] { 1, 4 }, _target.Items);
    }

    [Fact]
    public void Move()
    {
        _source.AddRange(new[] { 1, 2, 3, 4 });
        _source.Move(1, 2);
        Assert.Equal(new[] { 1, 3, 2, 4 }, _target.Items);
    }

    [Fact]
    public void Clear()
    {
        _source.AddRange(new[] { 1, 2, 3, 4 });
        _source.Clear();
        Assert.Empty(_target.Items);
    }

    [Theory]
    [InlineData(ListChangeReason.Add)]
    [InlineData(ListChangeReason.AddRange)]
    [InlineData(ListChangeReason.Replace)]
    [InlineData(ListChangeReason.Remove)]
    [InlineData(ListChangeReason.RemoveRange)]
    [InlineData(ListChangeReason.Moved)]
    [InlineData(ListChangeReason.Clear)]
    public void SetFocus(ListChangeReason construction)
    {
        switch (construction)
        {
            case ListChangeReason.Add:
                _source.Add(1);
                _source.Add(2);
                _source.Add(3);
                break;
            case ListChangeReason.AddRange:
                _source.AddRange(new[] { 1, 2, 3 });
                break;
            case ListChangeReason.Replace:
                _source.AddRange(new[] { 1, 4, 3 });
                _source.ReplaceAt(1, 2);
                break;
            case ListChangeReason.Remove:
                _source.AddRange(new[] { 1, 4, 2, 3 });
                _source.RemoveAt(1);
                break;
            case ListChangeReason.RemoveRange:
                _source.AddRange(new[] { 1, 4, 5, 2, 3 });
                _source.RemoveRange(1, 2);
                break;
            case ListChangeReason.Moved:
                _source.AddRange(new[] { 1, 3, 2 });
                _source.Move(2, 1);
                break;
            case ListChangeReason.Clear:
                _source.AddRange(new[] { 4, 5, 6 });
                _source.Clear();
                _source.AddRange(new[] { 1, 2, 3 });
                break;
        }

        _controller.SetFocus(2);

        Assert.Equal(new[] { 1, 3, 2 }, _target.Items);
    }

    [Theory]
    [InlineData(ListChangeReason.Add)]
    [InlineData(ListChangeReason.AddRange)]
    [InlineData(ListChangeReason.Replace)]
    [InlineData(ListChangeReason.Remove)]
    [InlineData(ListChangeReason.RemoveRange)]
    [InlineData(ListChangeReason.Moved)]
    [InlineData(ListChangeReason.Clear)]
    public void ClearFocus(ListChangeReason construction)
    {
        switch (construction)
        {
            case ListChangeReason.Add:
                _source.Add(1);
                _source.Add(2);
                _source.Add(3);
                break;
            case ListChangeReason.AddRange:
                _source.AddRange(new[] { 1, 2, 3 });
                break;
            case ListChangeReason.Replace:
                _source.AddRange(new[] { 1, 4, 3 });
                _source.ReplaceAt(1, 2);
                break;
            case ListChangeReason.Remove:
                _source.AddRange(new[] { 1, 4, 2, 3 });
                _source.RemoveAt(1);
                break;
            case ListChangeReason.RemoveRange:
                _source.AddRange(new[] { 1, 4, 5, 2, 3 });
                _source.RemoveRange(1, 2);
                break;
            case ListChangeReason.Moved:
                _source.AddRange(new[] { 1, 3, 2 });
                _source.Move(2, 1);
                break;
            case ListChangeReason.Clear:
                _source.AddRange(new[] { 4, 5, 6 });
                _source.Clear();
                _source.AddRange(new[] { 1, 2, 3 });
                break;
        }

        _controller.SetFocus(2);
        _controller.ResetFocus();

        Assert.Equal(new[] { 1, 2, 3 }, _target.Items);
    }

    [Theory]
    [InlineData(ListChangeReason.Add)]
    [InlineData(ListChangeReason.AddRange)]
    [InlineData(ListChangeReason.Replace)]
    [InlineData(ListChangeReason.Remove)]
    [InlineData(ListChangeReason.RemoveRange)]
    [InlineData(ListChangeReason.Moved)]
    [InlineData(ListChangeReason.Clear)]
    public void ChangeFocus(ListChangeReason construction)
    {
        switch (construction)
        {
            case ListChangeReason.Add:
                _source.Add(1);
                _source.Add(2);
                _source.Add(3);
                _source.Add(4);
                break;
            case ListChangeReason.AddRange:
                _source.AddRange(new[] { 1, 2, 3, 4 });
                break;
            case ListChangeReason.Replace:
                _source.AddRange(new[] { 1, 5, 3, 4 });
                _source.ReplaceAt(1, 2);
                break;
            case ListChangeReason.Remove:
                _source.AddRange(new[] { 1, 5, 2, 3, 4 });
                _source.RemoveAt(1);
                break;
            case ListChangeReason.RemoveRange:
                _source.AddRange(new[] { 1, 5, 6, 2, 3, 4 });
                _source.RemoveRange(1, 2);
                break;
            case ListChangeReason.Moved:
                _source.AddRange(new[] { 1, 3, 2, 4 });
                _source.Move(2, 1);
                break;
            case ListChangeReason.Clear:
                _source.AddRange(new[] { 5, 6 });
                _source.Clear();
                _source.AddRange(new[] { 1, 2, 3, 4 });
                break;
        }

        _controller.SetFocus(2);
        _controller.SetFocus(3);

        Assert.Equal(new[] { 1, 2, 4, 3 }, _target.Items);
    }

    [Theory]
    [InlineData(ListChangeReason.Add)]
    [InlineData(ListChangeReason.AddRange)]
    [InlineData(ListChangeReason.Replace)]
    [InlineData(ListChangeReason.Remove)]
    [InlineData(ListChangeReason.RemoveRange)]
    [InlineData(ListChangeReason.Moved)]
    public void MutateBeforeFocus(ListChangeReason reason)
    {
        switch (reason)
        {
            case ListChangeReason.Add:
                _source.Add(2);
                _source.Add(3);
                _source.Add(4);
                _controller.SetFocus(3);
                _source.Insert(0, 1);
                break;
            case ListChangeReason.AddRange:
                _source.AddRange(new[] { 3, 4 });
                _controller.SetFocus(3);
                _source.InsertRange(new[] { 1, 2 }, 0);
                break;
            case ListChangeReason.Replace:
                _source.AddRange(new[] { 1, 5, 3, 4 });
                _controller.SetFocus(3);
                _source.ReplaceAt(1, 2);
                break;
            case ListChangeReason.Remove:
                _source.AddRange(new[] { 1, 5, 2, 3, 4 });
                _controller.SetFocus(3);
                _source.RemoveAt(1);
                break;
            case ListChangeReason.RemoveRange:
                _source.AddRange(new[] { 1, 5, 6, 2, 3, 4 });
                _controller.SetFocus(3);
                _source.RemoveRange(1, 2);
                break;
            case ListChangeReason.Moved:
                _source.AddRange(new[] { 2, 1, 3, 4 });
                _controller.SetFocus(3);
                _source.Move(1, 0);
                break;
        }

        Assert.Equal(new[] { 1, 2, 4, 3 }, _target.Items);
    }

    [Theory]
    [InlineData(ListChangeReason.Add)]
    [InlineData(ListChangeReason.AddRange)]
    [InlineData(ListChangeReason.Replace)]
    [InlineData(ListChangeReason.Remove)]
    [InlineData(ListChangeReason.RemoveRange)]
    [InlineData(ListChangeReason.Moved)]
    public void MutateBeforeFocusThenResetFocus(ListChangeReason reason)
    {
        switch (reason)
        {
            case ListChangeReason.Add:
                _source.Add(2);
                _source.Add(3);
                _source.Add(4);
                _controller.SetFocus(3);
                _source.Insert(0, 1);
                break;
            case ListChangeReason.AddRange:
                _source.AddRange(new[] { 3, 4 });
                _controller.SetFocus(3);
                _source.InsertRange(new[] { 1, 2 }, 0);
                break;
            case ListChangeReason.Replace:
                _source.AddRange(new[] { 1, 5, 3, 4 });
                _controller.SetFocus(3);
                _source.ReplaceAt(1, 2);
                break;
            case ListChangeReason.Remove:
                _source.AddRange(new[] { 1, 5, 2, 3, 4 });
                _controller.SetFocus(3);
                _source.RemoveAt(1);
                break;
            case ListChangeReason.RemoveRange:
                _source.AddRange(new[] { 1, 5, 6, 2, 3, 4 });
                _controller.SetFocus(3);
                _source.RemoveRange(1, 2);
                break;
            case ListChangeReason.Moved:
                _source.AddRange(new[] { 2, 1, 3, 4 });
                _controller.SetFocus(3);
                _source.Move(1, 0);
                break;
        }

        _controller.ResetFocus();

        Assert.Equal(new[] { 1, 2, 3, 4 }, _target.Items);
    }

    [Theory]
    [InlineData(ListChangeReason.Add)]
    [InlineData(ListChangeReason.AddRange)]
    [InlineData(ListChangeReason.Replace)]
    [InlineData(ListChangeReason.Remove)]
    [InlineData(ListChangeReason.RemoveRange)]
    [InlineData(ListChangeReason.Moved)]
    public void MutateAfterFocus(ListChangeReason reason)
    {
        switch (reason)
        {
            case ListChangeReason.Add:
                _source.AddRange(new[] { 1, 2, 3 });
                _controller.SetFocus(2);
                _source.Add(4);
                _source.Add(5);
                break;
            case ListChangeReason.AddRange:
                _source.AddRange(new[] { 1, 2, 3 });
                _controller.SetFocus(2);
                _source.AddRange(new[] { 4, 5 });
                break;
            case ListChangeReason.Replace:
                _source.AddRange(new[] { 1, 2, 3, 6, 5 });
                _controller.SetFocus(2);
                _source.ReplaceAt(3, 4);
                break;
            case ListChangeReason.Remove:
                _source.AddRange(new[] { 1, 2, 3, 4, 6, 5 });
                _controller.SetFocus(2);
                _source.RemoveAt(4);
                break;
            case ListChangeReason.RemoveRange:
                _source.AddRange(new[] { 1, 2, 3, 4, 6, 7, 5 });
                _controller.SetFocus(2);
                _source.RemoveRange(4, 2);
                break;
            case ListChangeReason.Moved:
                _source.AddRange(new[] { 1, 2, 3, 5, 4 });
                _controller.SetFocus(2);
                _source.Move(4, 3);
                break;
        }

        Assert.Equal(new[] { 1, 3, 4, 5, 2 }, _target.Items);
    }

    [Theory]
    [InlineData(ListChangeReason.Replace)]
    [InlineData(ListChangeReason.Remove)]
    [InlineData(ListChangeReason.Clear)]
    public void RemoveFocused(ListChangeReason reason)
    {
        switch (reason)
        {
            case ListChangeReason.Replace:
                _source.AddRange(new[] { 1, 4, 3 });
                _controller.SetFocus(4);
                _source.ReplaceAt(1, 2);
                break;
            case ListChangeReason.Remove:
                _source.AddRange(new[] { 1, 2, 4, 3 });
                _controller.SetFocus(4);
                _source.RemoveAt(2);
                break;
            case ListChangeReason.Clear:
                _source.AddRange(new[] { 1, 2, 3 });
                _controller.SetFocus(2);
                _source.Clear();
                _source.AddRange(new[] { 1, 2, 3 });
                break;
        }

        Assert.Equal(new[] { 1, 2, 3 }, _target.Items);
        _controller.ResetFocus();
        Assert.Equal(new[] { 1, 2, 3 }, _target.Items);
        _controller.SetFocus(2);
        Assert.Equal(new[] { 1, 3, 2 }, _target.Items);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void RemoveRangeWithFocused(int focusedItem)
    {
        _source.AddRange(new[] { 1, 2, 4, 5, 6, 3 });
        _controller.SetFocus(focusedItem);
        _source.RemoveRange(2, 3);

        Assert.Equal(new[] { 1, 2, 3 }, _target.Items);
        _controller.ResetFocus();
        Assert.Equal(new[] { 1, 2, 3 }, _target.Items);
        _controller.SetFocus(2);
        Assert.Equal(new[] { 1, 3, 2 }, _target.Items);
    }

    [Fact]
    public void MoveFocused()
    {
        _source.AddRange(new[] { 1, 2, 3, 4 });
        _controller.SetFocus(2);
        _source.Move(1, 2);
        Assert.Equal(new[] { 1, 3, 4, 2 }, _target.Items);
        _controller.ResetFocus();
        Assert.Equal(new[] { 1, 3, 2, 4 }, _target.Items);
    }

    [Fact]
    public void MoveFromBeforeToAfterFocused()
    {
        _source.AddRange(new[] { 1, 2, 3, 4, 5 });
        _controller.SetFocus(3);
        _source.Move(1, 2);
        Assert.Equal(new[] { 1, 2, 4, 5, 3 }, _target.Items);
        _controller.ResetFocus();
        Assert.Equal(new[] { 1, 3, 2, 4, 5 }, _target.Items);
    }

    [Fact]
    public void MoveFromAfterToBeforeFocused()
    {
        _source.AddRange(new[] { 1, 2, 3, 4, 5 });
        _controller.SetFocus(3);
        _source.Move(3, 2);
        Assert.Equal(new[] { 1, 2, 4, 5, 3 }, _target.Items);
        _controller.ResetFocus();
        Assert.Equal(new[] { 1, 2, 4, 3, 5 }, _target.Items);
    }
}

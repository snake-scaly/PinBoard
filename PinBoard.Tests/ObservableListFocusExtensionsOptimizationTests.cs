using System;
using System.Collections.Generic;
using System.Linq;
using DynamicData;
using PinBoard.Util;
using Xunit;

namespace PinBoard.Tests;

public sealed class ObservableListFocusExtensionsOptimizationTests : IDisposable
{
    private readonly SourceList<int> _source = new();
    private readonly List<IChangeSet<int>> _changes = new();
    private readonly IDisposable _sub;
    private IObservableListFocusController<int> _controller;

    public ObservableListFocusExtensionsOptimizationTests()
    {
        _sub = _source.Connect()
            .Focus(x => _controller = x)
            .Subscribe(x => _changes.Add(x));
    }

    public void Dispose()
    {
        _sub.Dispose();
    }

    [Fact]
    public void SetFocus_LastItem_NoChange()
    {
        _source.AddRange(new[] { 1, 2 });
        _changes.Clear();
        _controller.SetFocus(2);
        Assert.Empty(_changes);
    }

    [Fact]
    public void ResetFocus_LastItem_NoChange()
    {
        _source.AddRange(new[] { 1, 2 });
        _controller.SetFocus(2);
        _changes.Clear();
        _controller.ResetFocus();
        Assert.Empty(_changes);
    }

    [Fact]
    public void SetFocus_SameItem_NoOp()
    {
        _source.AddRange(new[] { 1, 2 });
        _controller.SetFocus(1);
        _changes.Clear();
        _controller.SetFocus(1);
        Assert.Empty(_changes);
    }

    [Fact]
    public void ReplaceFocused_LastItem_EmitsReplace()
    {
        _source.AddRange(new[] { 1, 2 });
        _controller.SetFocus(2);
        _changes.Clear();
        _source.ReplaceAt(1, 3);

        var cs = Assert.Single(_changes);
        var c  = Assert.Single(cs);
        Assert.Equal(ListChangeReason.Replace, c.Reason);
    }

    [Fact]
    public void RemoveRangeWithFocus_DisjointFocus_OneRemoveRangeAndOneRemove()
    {
        _source.AddRange(new[] { 1, 2, 3, 4, 5 });
        _controller.SetFocus(3);
        _changes.Clear();
        _source.RemoveRange(1, 3);

        var cs = Assert.Single(_changes);
        Assert.Equal(2, cs.Count);

        var c1 = cs.ElementAt(0);
        Assert.Equal(ListChangeReason.Remove, c1.Reason);
        Assert.Equal(4, c1.Item.CurrentIndex);
        Assert.Equal(3, c1.Item.Current);

        var c2 = cs.ElementAt(1);
        Assert.Equal(ListChangeReason.RemoveRange, c2.Reason);
        Assert.Equal(1, c2.Range.Index);
        Assert.Equal(new[] { 2, 4 }, c2.Range);
    }

    [Fact]
    public void RemoveRangeWithFocus_AdjointFocus_OneRemoveRange()
    {
        _source.AddRange(new[] { 1, 2, 3, 4, 5 });
        _controller.SetFocus(3);
        _changes.Clear();
        _source.RemoveRange(2, 3);

        var cs = Assert.Single(_changes);
        Assert.Equal(1, cs.Count);

        var c = cs.First();
        Assert.Equal(ListChangeReason.RemoveRange, c.Reason);
        Assert.Equal(2, c.Range.Index);
        Assert.Equal(new[] { 4, 5, 3 }, c.Range);
    }

    [Fact]
    public void Move_SameItem_NoOp()
    {
        _source.AddRange(new[] { 1, 2, 3 });
        _changes.Clear();
        _source.Move(1, 1);
        Assert.Empty(_changes);
    }
}

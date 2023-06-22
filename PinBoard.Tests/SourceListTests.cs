using System;
using System.Collections.Generic;
using DynamicData;
using PinBoard.Util;
using Xunit;

namespace PinBoard.Tests;

public class SourceListTests
{
    [Fact]
    public void SubscribeMany_UnsubscribesOnRemove()
    {
        var sourceList = new SourceList<object>();
        var log = new List<string>();
        sourceList.Connect()
            .SubscribeMany(
                _ =>
                {
                    log.Add("subscribed");
                    return new CallbackDisposable(() => log.Add("unsubscribed"));
                })
            .Subscribe(_ => log.Add("change"));
        var o = new object();

        sourceList.Add(o);
        sourceList.Remove(o);

        Assert.Equal(new[] { "subscribed", "change", "unsubscribed", "change" }, log);
    }

    [Fact]
    public void PopulateInto_DoesNotClearTargetListWhenDisposed()
    {
        var source = new SourceList<string>();
        var target = new SourceList<string>();

        source.AddRange(new[] { "a", "b" });

        var sub = source.Connect().PopulateInto(target);
        Assert.Equal(new[] { "a", "b" }, target.Items);
        sub.Dispose();
        Assert.Equal(new[] { "a", "b" }, target.Items);
    }
}

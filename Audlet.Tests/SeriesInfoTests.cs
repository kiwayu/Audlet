using Audlet.Library;
using Audlet.Models;

namespace Audlet.Tests;

public class SeriesInfoTests
{
    [Theory]
    [InlineData("Dune 03", null, "Dune", 3)]
    [InlineData("Dune - Book 3", null, "Dune", 3)]
    [InlineData("Dune #3", null, "Dune", 3)]
    [InlineData("Dune, Vol. 3", null, "Dune", 3)]
    [InlineData("03 - Dune Messiah", "Dune", "Dune", 3)]
    [InlineData("Piranesi", null, null, null)]
    [InlineData("1984", null, null, null)]
    [InlineData("03 - Dune Messiah", null, null, null)]
    public void ParsesSeriesAndNumber(string name, string? parent, string? series, int? index)
    {
        var (s, i) = SeriesInfo.Parse(name, parent);
        Assert.Equal(series, s);
        Assert.Equal((double?)index, i);
    }

    [Fact]
    public void NextPicksTheFollowingNumberAndSkipsMissing()
    {
        Book B(string id, double n, bool missing = false) => new() { Id = id, Series = "Dune", SeriesIndex = n, Missing = missing };
        var one = B("1", 1); var two = B("2", 2, missing: true); var three = B("3", 3); var five = B("5", 5);
        var other = new Book { Id = "x", Series = "Foundation", SeriesIndex = 2 };
        var all = new[] { five, other, three, one, two };
        Assert.Same(three, SeriesInfo.Next(all, one));
        Assert.Same(five, SeriesInfo.Next(all, three));
        Assert.Null(SeriesInfo.Next(all, five));
        Assert.Null(SeriesInfo.Next(all, new Book { Id = "none" }));
    }

    [Fact]
    public void LabelsCountBooksPerSeries()
    {
        var a = new Book { Id = "a", Series = "Dune", SeriesIndex = 1 };
        var b = new Book { Id = "b", Series = "dune", SeriesIndex = 2 };
        var c = new Book { Id = "c" };
        SeriesInfo.Label([a, b, c]);
        Assert.Equal("Dune 1 of 2", a.SeriesLabel);
        Assert.Equal("dune 2 of 2", b.SeriesLabel);
        Assert.Null(c.SeriesLabel);
    }
}

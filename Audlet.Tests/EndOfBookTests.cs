using Audlet.Models;
using Audlet.Player;

namespace Audlet.Tests;

public class EndOfBookTests
{
    private readonly Book _one = new() { Id = @"C:\Books\Saga 1", Title = "Saga 1", Series = "Saga", SeriesIndex = 1, State = BookState.Started, FileIndex = 1, PositionSec = 30 };
    private readonly Book _two = new() { Id = @"C:\Books\Saga 2", Title = "Saga 2", Series = "Saga", SeriesIndex = 2 };
    private readonly Book _other = new() { Id = @"C:\Books\Other", Title = "Other" };
    private Book[] Books => [_one, _two, _other];

    [Fact]
    public void TheFinishedBookIsResetAndLeavesTheQueue()
    {
        var settings = new Settings { Queue = [@"c:\books\saga 1"] };

        EndOfBook.Finish(_one, Books, settings);

        Assert.Equal((BookState.Finished, 0, 0.0), (_one.State, _one.FileIndex, _one.PositionSec));
        Assert.Empty(settings.Queue);
    }

    [Fact]
    public void TheHeadOfTheQueueComesNextAndIsTakenOff()
    {
        var settings = new Settings { Queue = [_one.Id, _other.Id, _two.Id], EndOfBookAction = EndOfBookAction.NextBookPlay };

        Assert.Equal((_other, true, "Finished. Next: Other"), EndOfBook.Finish(_one, Books, settings));
        Assert.Equal([_two.Id], settings.Queue);
    }

    [Fact]
    public void WithNothingQueuedTheSeriesContinues()
    {
        var settings = new Settings { EndOfBookAction = EndOfBookAction.NextBook };

        Assert.Equal((_two, false, "Finished. Next: Saga 2"), EndOfBook.Finish(_one, Books, settings));
    }

    [Fact]
    public void StopLoadsNothingAndLeavesTheQueueAlone()
    {
        var settings = new Settings { EndOfBookAction = EndOfBookAction.Stop };
        Assert.Equal(((Book?)null, false, "Finished. Next in the series: Saga 2"), EndOfBook.Finish(_one, Books, settings));

        settings.Queue = [_other.Id];
        EndOfBook.Finish(_two, Books, settings);
        Assert.Equal([_other.Id], settings.Queue);
    }

    [Fact]
    public void WithNothingNextTheBookIsSimplyFinished()
    {
        var settings = new Settings { EndOfBookAction = EndOfBookAction.NextBookPlay };

        Assert.Equal(((Book?)null, false, "The book is finished."), EndOfBook.Finish(_other, Books, settings));
    }
}

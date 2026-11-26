using System.Windows.Threading;
using Audlet.Player;

namespace Audlet.Views;

public sealed class UiTimer : DispatcherTimer, ITicker
{
    public static ITicker Every(TimeSpan interval) => new UiTimer { Interval = interval };
}

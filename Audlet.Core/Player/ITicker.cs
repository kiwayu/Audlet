namespace Audlet.Player;

// A repeating timer. The app uses UiTimer, a DispatcherTimer, so ticks run on the UI thread.
public interface ITicker
{
    event EventHandler? Tick;
    void Start();
    void Stop();
}

# Windows Audiobook Player: Design

Date: 2026-09-20

## Goal

A Windows 11 desktop audiobook player with the feature set of Smart AudioBook Player (Android) and a frosted-glass look in the style of macOS. Personal use, single user, local files only.

## Scope

In the first version:

- Library from one or more root folders, book states New / Started / Finished, sort, search, book queue
- Player with big and small rewind/forward, previous/next file, chapters, position in file or book, time left
- Speed (pitch preserved), pitch shift, five-band equalizer, volume boost, skip silence, mono, balance
- Auto-rewind after a pause, skip start/end of each file, lock buttons, end-of-book action
- Sleep timer with fadeout and daily schedule
- Bookmarks with notes, playback history (undo accidental jumps), playback statistics, character list per book
- Windows media overlay and media keys, in-app keyboard shortcuts
- Light and dark theme following Windows

Deferred to later versions: SRT subtitles, companion text/PDF files, MP3 to Opus conversion, backup to external drive, cover and description search on the web, minimize to tray.

Not applicable on desktop: Chromecast, Android Auto, home screen widgets, headset button mapping, shake to reset the timer, rotate to pause, phone call handling.

## Technology

- .NET 9, WPF, Fluent theme with Mica window backdrop
- CommunityToolkit.Mvvm for view models
- ManagedBass with the BASS core plus bass_fx, bassflac, bassopus and basswv native libraries shipped in the app folder. Windows Media Foundation, used by BASS automatically, decodes AAC, M4A, M4B and WMA.
- ATL (atldotnet) for tags, durations, embedded covers, and chapters (MP4 chapters and ID3v2 CHAP frames)
- System.Text.Json for storage
- xUnit for tests

Supported audio extensions: mp3, m4a, m4b, aac, ogg, opus, flac, wav, wv, wma.
Supported cover files in a book folder: jpg, jpeg, png, webp. A file named cover or folder wins; otherwise the first match in name order.

## Architecture

One WPF project, `WindowsAudiobookPlayer`, plus one test project, `WindowsAudiobookPlayer.Tests`.

| Folder | Contents |
|---|---|
| `Audio/` | `BassPlayer`: the only class that touches BASS. One stream at a time. Play, Pause, Seek, Speed, Pitch, Boost, EqBands, Mono, Balance, Level. Events: PositionChanged, Ended, Failed. |
| `Library/` | `LibraryScanner` (folder walk and grouping), `MetadataReader` (ATL), `CoverCache` (resized JPEG per book under LocalAppData). |
| `Models/` | `Book`, `AudioFile`, `Chapter`, `Bookmark`, `Character`, `HistoryEntry`, `MonthStats`, `Settings`. Plain records. |
| `Data/` | `AppStore`: load and save `library.json`, `settings.json`, `stats.json`. |
| `Player/` | `PlaybackController`: current book, transport, rewind rules, auto-rewind, skip start/end, skip silence, sleep timer, end-of-book action, history, statistics, media overlay. |
| `ViewModels/` | One per view. |
| `Views/` | `MainWindow`, `LibraryView`, `NowPlayingView`, `BookmarksView`, `CharactersView`, `StatisticsView`, `SettingsView`, `FirstRunView`. |

Dependency direction: Views bind to ViewModels; ViewModels call `PlaybackController` and `AppStore`; `PlaybackController` calls `BassPlayer`; `LibraryScanner` calls `MetadataReader` and `CoverCache`. Nothing calls upward.

## Data model

Under each root folder the scanner walks depth-first and applies these rules:

- A folder that directly contains audio files is a **folder book**. It includes audio files in its sub-folders too (CD1, CD2), sorted by natural path order.
- A folder that contains no audio files directly, only sub-folders, is a **grouping folder** (author, series). It is not a book. Its name appears as breadcrumb text on the books under it.
- Exception to the first rule: if a folder directly contains audio files **and** sub-folders that contain audio, the directly contained files are each a **standalone book** (a lone M4B sitting next to book folders), and the sub-folders are scanned normally.

Book identity is its absolute path (folder or file). Title and author come from the tags of the first file; the folder name (or file name) is the fallback title.

```
Book
  id             string   absolute path
  title, author  string
  files          AudioFile[]   ordered
  chapters       Chapter[]     from tags, or one per file
  coverPath      string?       cached JPEG
  state          New | Started | Finished
  fileIndex      int
  positionSec    double
  speed          double?       null = use default
  eqBands        double[5]?    null = use default
  skipStartSec   int
  skipEndSec     int
  bookmarks      Bookmark[]
  characters     Character[]
  history        HistoryEntry[]   capped at 100
  listenedSec    long
  addedUtc       ISO 8601 timestamp, e.g. 2026-09-20T14:03:00Z
  lastPlayedUtc  ISO 8601 timestamp or null
  missing        bool          files not found on last scan

AudioFile   path, durationSec, title (tag or filename)
Chapter     title, fileIndex, startSec, endSec
Bookmark    fileIndex, positionSec, note, createdUtc
Character   name, description
HistoryEntry kind (Seek | Jump | BookSwitch), fileIndex, positionSec, atUtc
MonthStats  month ("2026-09"), listenedSec
Settings    rootFolders, smallRewindSec, bigRewindSec, showPrevNextFile, autoRewind,
            skipSilence, defaultSpeed, defaultEqBands, boost, sleepDefaultMin,
            sleepFadeout, sleepSchedule {enabled, fromTime, toTime},
            endOfBookAction (Stop | NextBook | NextBookPlay), positionMode (File | Book),
            sortMode, queue (book ids), sidebarWidth, chaptersPanelWidth, window bounds
```

Storage: `%LocalAppData%\WindowsAudiobookPlayer\` with `library.json` (all books), `settings.json`, `stats.json` (months), `covers\` (one JPEG per book, named by a hash of the book id). Saves are debounced two seconds and forced on pause, book switch, and exit. Position is written to the in-memory book every five seconds while playing.

Rescan merges by book id: existing books keep their state, position, bookmarks, characters, and history; files are refreshed; books whose path no longer exists are marked missing but kept.

## Audio engine

Each file opens as a BASS decode stream wrapped by a bass_fx tempo stream.

- Speed: 0.5x to 3.0x in 0.05 steps, pitch preserved. Per-book override or global default.
- Pitch: -12 to +12 semitones, per book.
- Equalizer: five peaking filters at 60, 230, 910, 3600 and 14000 Hz, -15 to +15 dB.
- Boost: gain DSP, 0 to +12 dB, with a hard limiter to avoid clipping.
- Mono and balance: channel attributes.

A multi-file book is one continuous timeline. On file end the next file opens and playback continues. Position is stored as file index plus seconds. The UI can show position and time left for the file or for the whole book.

Chapters: from tags when the file has them; otherwise one chapter per file, named from the tag title or the filename.

## Player behavior

- Rewind sizes: small default 10 s, big default 60 s, both from the list 5, 10, 15, 20, 30, 45, 60, 90, 120, 300.
- Auto-rewind on resume by pause length: under 10 s none; up to 2 min 5 s; up to 10 min 15 s; longer 30 s. Can be disabled.
- Skip start/end: per book, trims N seconds from the start and end of every file.
- Skip silence: level sampled every 50 ms; after 400 ms below threshold the tempo is raised to 2x until sound returns, then the user's speed is restored. This is a deliberate simplification over true silence removal.
- Lock buttons: hides all controls except play/pause and the unlock button.
- End of book: Stop, switch to next book in the queue, or switch and play. The book becomes Finished.
- Sleep timer: presets 5, 10, 15, 20, 30, 45, 60, 75, 90 minutes and End of chapter. The last 60 seconds fade volume out. Any click or keypress in the window resets a running timer. Optional daily schedule turns the timer on automatically when playback starts inside the window.
- History: every seek, every jump larger than the small rewind, and every book switch is recorded with time and position, capped at 100 per book. Selecting an entry jumps back.
- Statistics: listened seconds accumulate per book and per calendar month while playing, counted in real time, not sped-up time.
- Book queue: ordered list of book ids. Adding a book appends it; the end-of-book action pops the head.

Media integration: System Media Transport Controls show title, author, cover and transport buttons; media keys work. Shortcuts: Space play/pause, Left/Right small rewind/forward, Shift+Left/Right big, Up/Down speed, B add bookmark, Ctrl+L lock, Escape close Now Playing.

## User interface

One window with a custom title bar, Mica backdrop, following the Windows light or dark setting.

**Main view.** Resizable sidebar on the left (Library, Started, New, Finished, Queue, Bookmarks, Statistics, Settings), a cover grid in the middle, and a persistent player bar along the bottom showing cover, title, chapter, speed, time left, progress, and transport buttons. Clicking the bar opens Now Playing in the content area.

**Now Playing.** Two columns with a draggable splitter, default one third for the left column. Left: a glass panel with tabs Chapters, Bookmarks, History. Right: cover with title and author on top, and a glass controls panel below with progress bar, times, transport row (previous file, big rewind, small rewind, play/pause, small forward, big forward, next file), and a row of tool buttons (speed, sleep, bookmark, equalizer, boost, lock). The backdrop is the cover, blurred and tinted.

**Glass.** Panels are translucent (white in light mode, black in dark mode) with a one-pixel translucent border, 12 px corner radius, and 24 px gutters everywhere. Because WPF has no in-window backdrop blur, glass is a translucent brush drawn over an already blurred cover image, which reads the same.

**Library.** Cards show cover, title, author, state badge, progress bar, and time left. Sort by title, author, recently played, length, date added. Live search. Context menu: Mark as New / Started / Finished, Add to queue, Set cover from file, Open folder. Missing books are dimmed.

**Other views.** Bookmarks list with note editing and jump. Characters list with add, edit, delete, and import from another finished book. Statistics: total listened time, per-month bar list, per-book totals. Settings pages: Playback, Sleep, Library (root folders, rescan), Appearance (theme override).

**First run.** A single screen asking for a root folder, then the first scan with progress.

## Error handling

- A file BASS cannot open is skipped, shown with a toast, and marked in the file list.
- A missing root folder is flagged in Settings; its books are marked missing, never deleted.
- Corrupt JSON is renamed with a `.bad` suffix and the app starts with an empty store.
- Missing native DLLs produce one clear dialog at startup naming the file.
- All file, tag, and cover work runs off the UI thread. Scanning reports progress.

## Testing

xUnit project covering pure logic:

- Scanner grouping: folder book, standalone file book, grouping folder, nested CD folders
- Library merge on rescan keeps state, position, bookmarks; marks missing
- Auto-rewind tiers
- Position to chapter mapping across files
- Sleep timer schedule window, including windows that cross midnight
- History cap at 100 and undo jump target
- JSON round-trip of every model

The BASS wrapper is verified with a manual checklist against sample files, since it needs a real audio device.

## Repository

Public GitHub repository `kiwayu/WindowsAudiobookPlayer`. Small, frequent commits, one per task. README covers what the app does, how to build, and where to place the native BASS libraries. `.gitignore` excludes `bin/`, `obj/`, `.vs/`, `*.user`, `*.apk`, and `.superpowers/`.

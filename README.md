# Windows Audiobook Player

A desktop audiobook player for Windows 11 with a frosted-glass interface. Point it at the folders where you keep your books and it tracks your position, chapters, bookmarks, and listening time per book.

## Features

- Library from any number of root folders; a folder of audio files is a book, a lone M4B is a book
- New / Started / Finished states, sort, search, and a queue for what to play next
- Big and small rewind and forward, chapters, position in file or in book
- Speed with pitch preserved, pitch shift, five-band equalizer, volume boost, skip silence
- Auto-rewind after a pause, skip the start and end of every file, lock buttons
- Sleep timer with fadeout and a daily schedule
- Bookmarks with notes, playback history to undo an accidental jump, listening statistics, a character list per book
- Windows media overlay and media keys
- Follows the Windows light or dark theme

## Building

Requires the .NET 9 SDK.

1. Run `tools\get-bass.ps1` once. It downloads the BASS audio libraries into `lib\bass\`.
2. `dotnet build`
3. `dotnet run --project WindowsAudiobookPlayer`

Tests: `dotnet test` runs the unit tests and the end-to-end suite. The end-to-end project launches the real executable against a throwaway data folder and a generated library of tone files, then drives it through Windows UI Automation. It needs an audio device and takes about fifteen seconds. Run the unit tests alone with `dotnet test WindowsAudiobookPlayer.Tests`.

Release build: `tools\publish.ps1` produces a self-contained folder in `dist\WindowsAudiobookPlayer` that runs on any 64-bit Windows 11 machine without installing .NET. If [Inno Setup](https://jrsoftware.org/isinfo.php) is installed (`winget install JRSoftware.InnoSetup`), the same script also builds `dist\AudiobookPlayer-Setup-<version>.exe`, a per-user installer with Start Menu and optional desktop shortcuts and an uninstaller.

## Supported files

mp3, m4a, m4b, aac, ogg, opus, flac, wav, wv, wma. Chapters are read from M4B files and ID3 chapter frames. Covers come from embedded art or a jpg, png, or webp file in the book folder.

## Keyboard

| Key | Action |
|---|---|
| Space | Play / pause |
| Left / Right | Small rewind / forward |
| Shift + Left / Right | Big rewind / forward |
| Up / Down | Speed up / down by 0.05 |
| B | Add a bookmark |
| Ctrl + L | Lock / unlock the buttons |
| Esc | Back to the library |

The Windows media keys and the media overlay also control playback.

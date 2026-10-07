# Audlet

A desktop audiobook player for Windows 11 with a frosted-glass interface. Point it at the folders where you keep your books and it tracks your position, chapters, bookmarks, and listening time per book.

## Features

- Library from any number of root folders; a folder of audio files is a book, a lone M4B is a book
- Drop folders or audio files onto the window to add them, or pick the folder with Browse in Settings
- New / Started / Finished states, sort, search, and a queue for what to play next
- Rename books, create folders for genres or authors, and move books between them from the library
- Find a cover and blurb online from Audible, Apple Books, Open Library, or Google Books and pick the one you want
- Big and small rewind and forward, chapters, position in file or in book
- Speed with pitch preserved, pitch shift, five-band equalizer, volume boost, skip silence
- Auto-rewind after a pause, skip the start and end of every file, lock buttons
- Sleep timer with fadeout and a daily schedule
- Bookmarks with notes, playback history to undo an accidental jump, listening statistics, a character list per book
- Windows media overlay and media keys, plus global hotkeys that work while another app has focus
- Closing the window keeps playing from the tray, with play, rewind, and forward in the tray menu
- Hover the taskbar button for rewind, play or pause, and forward, with book progress shown on the button
- Sleep timer fades out, and the next play asks whether to rewind to where you dozed off
- Pauses by itself when the audio device disappears, such as headphones being unplugged
- One-click speed presets on the player bar
- Chapter ticks on the seek bar with the chapter name on hover
- Series detection from tags or folder names, "Dune 3 of 6" on the card, and the next book offered when one finishes
- Checks GitHub for a newer version on startup and offers the download
- Themes: dark and light glass that follow Windows, plus Catppuccin Mocha and Latte, Nord, Dracula, Tokyo Night, Rose Pine, Gruvbox Dark, and Solarized Dark

## Building

Requires the .NET 9 SDK.

1. Run `tools\get-bass.ps1` once. It downloads the BASS audio libraries into `lib\bass\`.
2. `dotnet build`
3. `dotnet run --project Audlet`

Tests: `dotnet test` runs everything. The end-to-end tests (`--filter Category=E2E`) launch the real executable against a throwaway data folder and a generated library of tone files, then drive it through Windows UI Automation; they need an audio device and take about half a minute. Skip them with `--filter Category!=E2E`.

Release build: `tools\publish.ps1` produces a self-contained folder in `dist\Audlet` that runs on any 64-bit Windows 11 machine without installing .NET. If [Inno Setup](https://jrsoftware.org/isinfo.php) is installed (`winget install JRSoftware.InnoSetup`), the same script also builds `dist\Audlet-Setup-<version>.exe`, a per-user installer with Start Menu and optional desktop shortcuts and an uninstaller.

## Supported files

mp3, m4a, m4b, aac, ogg, opus, flac, wav, wv, wma. Chapters are read from M4B files and ID3 chapter frames. Covers come from embedded art or a jpg, png, or webp file in the book folder.

## Website

`site/` is the static homepage, served by Cloudflare Pages straight from this repo: production branch `main`, no build command, output directory `site`. The download button reads the latest release from the GitHub API and falls back to the releases page when it cannot.

## Keyboard

Every in-app shortcut can be changed under Settings > Keyboard. The defaults:

| Key | Action |
|---|---|
| Space | Play / pause |
| Left / Right | Small rewind / forward |
| Shift + Left / Right | Big rewind / forward |
| Ctrl + Left | Start of the chapter, or the previous chapter when already at the start |
| Ctrl + Right | Next chapter |
| Up / Down | Speed up / down by 0.05 |
| B | Add a bookmark |
| Ctrl + L | Lock / unlock the buttons |
| Esc | Back to the library |
| Ctrl + Alt + Space | Play / pause from anywhere |
| Ctrl + Alt + Left / Right | Small rewind / forward from anywhere |
| Ctrl + Alt + Shift + Left / Right | Big rewind / forward from anywhere |

The Windows media keys and the media overlay also control playback.

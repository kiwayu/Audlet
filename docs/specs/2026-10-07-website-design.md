# Audlet website

A single static homepage for Audlet, hosted free on Cloudflare Pages from the `site/` folder of this repo. It exists to show what the app looks like and to hand the visitor the installer.

## Goals

- Look like the app: the same dark and light glass, the same colour orbs, the same panel material, following the visitor's system theme.
- One click to the current installer, always the latest release, never a stale link.
- Fast and simple. No framework, no build step, nothing to install to edit it.

## Non-goals

- Multiple pages, a blog, documentation, or a changelog. The GitHub releases page covers release notes.
- A portable download. The installer is the only artifact offered.
- Analytics, cookies, or any third-party script.

## Files

```
site/
  index.html        the whole page
  style.css         tokens, layout, glass material, motion
  main.js           release lookup, scroll reveal, tilt, platform note
  favicon.png       made from Audlet/app.ico
  screenshots/
    now-playing-dark.png   now-playing-light.png
    library-dark.png       library-light.png
  _headers          long cache for screenshots and favicon
```

Cloudflare Pages settings: production branch `main`, build command empty, build output directory `site`. Every push to `main` redeploys. The default address is `audlet.pages.dev`; a custom domain can be attached later without code changes.

## Page content, top to bottom

1. **Nav.** Wordmark on the left. Links: Features, Download, GitHub.
2. **Hero.** Wordmark, a one-line tagline, the download button with the live version, the line "Windows 11 · free" beneath it, and the Now Playing screenshot in a glass frame.
3. **About.** One short paragraph in the README's voice: a desktop player for audiobooks you already own, pointed at your folders, remembering position, chapters, bookmarks and listening time for every book, with no account and nothing uploaded.
4. **Features.** Six glass cards, each a title and one sentence:
   - Your folders are the library. Drop folders or files on the window; a folder of audio or a lone M4B is a book.
   - Chapters, bookmarks, history. Chapter ticks on the seek bar, notes on bookmarks, undo an accidental jump.
   - Sound your way. Speed with pitch preserved, five-band EQ, boost, skip silence, sleep timer with fadeout.
   - Lives in the desktop. Tray playback, media keys, taskbar controls, global hotkeys, pauses when headphones unplug.
   - Series and covers. Series detected from tags or folder names, next book offered, covers and blurbs found online.
   - Ten themes. Dark and light glass that follow Windows, plus Catppuccin, Nord, Dracula, Tokyo Night, Rose Pine, Gruvbox, Solarized.
   The Library screenshot sits alongside the cards.
5. **Download.** The button again. Requirements: Windows 11, 64-bit, nothing else to install. A short note that Windows SmartScreen may show a prompt the first time because the installer is not signed, and that "More info, then Run anyway" continues. A link to all releases on GitHub.
6. **Footer.** GitHub link and a copyright line for Audlet. No personal details.

## Visual system

Tokens are copied from `ThemeCatalog.cs` and `ThemeService.cs` into CSS custom properties on `:root`, redefined under `prefers-color-scheme: light`.

- Background: the three-stop diagonal gradient of Dark glass (`#141826`, `#1C2333`, `#23203A`) and Light glass (`#E9EDF5`, `#DCE3F0`, `#E6DDF2`).
- Orbs: three large radial gradients in the theme's orb colour (`#6E7CF5` dark, `#7C8CFF` light) and its hue shifted by plus and minus 45 degrees, placed behind the content like the app.
- Panels: white at 18% fading to 8% top to bottom on dark, 70% to 54% on light, `backdrop-filter: blur(24px)`, a 1px lit rim that is brightest at the top-left corner, 16px radius, a soft drop shadow below.
- Buttons: the same sheen and rim. The download button is the accent fill with the theme's accent text colour.
- Type: the system font stack, so Windows shows Segoe UI like the app. Headline sizes scale with the viewport.
- Screenshots: the set matching the current colour scheme is shown; the other set is not loaded.

## Motion

- Orbs drift slowly on a 40 to 60 second loop.
- Hero wordmark, tagline, button and screenshot fade and rise on load, staggered.
- Sections reveal once when scrolled into view.
- Buttons lift slightly with a brighter sheen on hover and focus.
- The hero screenshot tilts up to a few degrees toward the pointer on devices with a mouse.
- Everything above is disabled under `prefers-reduced-motion: reduce`.

## Download behaviour

On load, `main.js` fetches `https://api.github.com/repos/kiwayu/Audlet/releases/latest`, finds the asset whose name starts with `Audlet-Setup`, and sets the button's link to that asset's download URL and its label to "Download for Windows · v1.2.3 · 48 MB". Before the fetch, and if it fails or is rate limited, the button links to `https://github.com/kiwayu/Audlet/releases/latest`, which always works, with the label "Download for Windows".

On a platform that is not Windows, a one-line note under the button says Audlet is a Windows app. The button still works.

## Vital details

- Works at phone width with a 16px gutter and no horizontal scroll.
- Visible keyboard focus on every link and button; the page is usable with Tab alone.
- Alt text on every screenshot describing the view shown.
- `<title>`, meta description, Open Graph and Twitter card tags with the dark Now Playing screenshot as the preview image, so the link looks right when shared.
- Screenshots exported at 1400px wide as PNG, lazy loaded below the fold, the hero image eager.
- No external requests except the GitHub API call.

## Screenshots

Captured from the real app at a 1200 by 760 window in Dark glass and Light glass, Now Playing and Library, with a populated library so cards render at full strength. Refresh them when the app's look changes.

## Testing

- Open the page at desktop and phone widths in both colour schemes and compare against the app.
- Check the download button with the API reachable, with it blocked, and with the asset missing.
- Check with reduced motion enabled that nothing moves.
- Validate the HTML and check that every image has alt text and every control is reachable by keyboard.

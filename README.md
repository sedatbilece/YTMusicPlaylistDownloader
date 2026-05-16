# YTPlaylistDownloader

A Windows desktop application for downloading YouTube / YouTube Music playlists as MP3 files.

## Download

1. Download **[YTPlaylistDownloader.zip](YTPlaylistDownloader.zip)** (~82 KB)
2. Extract all files to a folder
3. Run `YTPlaylistDownloader.exe`

> **Requires:** [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) — if not installed, Windows will prompt you to download it automatically on first run.

> **On first use**, the app will offer to download **yt-dlp** and **ffmpeg** (~90 MB total). These are saved next to the executable and only downloaded once.

## Usage

1. Paste a YouTube or YouTube Music playlist URL into the **Playlist URL** field
2. Click **Fetch Songs** — all tracks will be listed and checked by default
3. Uncheck any songs you don't want, or use **Select All / Deselect All**
4. Choose an **Output Folder** (defaults to your Music library)
5. Click **Download Selected** — songs are saved as MP3

You can click **Cancel** at any time to stop the download.

## How it works

| Component | Role |
|-----------|------|
| **yt-dlp** | Fetches playlist metadata and downloads audio streams |
| **ffmpeg** | Converts downloaded audio to MP3 |

Both tools are downloaded automatically from their official GitHub releases on first use.

## Building from source

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download).

```
git clone https://github.com/sedatbilece/YTPlaylistDownloader.git
cd YTPlaylistDownloader/YTPlaylistDownloader
dotnet run
```

To publish:

```
dotnet publish -c Release --self-contained false
```

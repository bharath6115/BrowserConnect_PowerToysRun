# 🚀 BrowserConnect for PowerToys Run

**BrowserConnect** is a modular, high-performance PowerToys Run plugin that allows you to search across custom engines, manage your search history with fuzzy matching, and quickly navigate to URLs—all with full support for incognito mode and a premium, icon-driven interface.

## ✨ Key Features

- **🎯 Custom Search Engines**:
  - **Define Aliases**: Create your own shortcuts (e.g., `yt` for YouTube, `gh` for GitHub).
  - **Custom Queries**: Use `%s` to mark exactly where your search query should be injected into the URL.
- **🕰️ Deep Fuzzy History Search**:
  - **Quick Access**: Access your recent search history using the `!` prefix.
  - **Cache First**: Instantly shows the most recent unique searches from memory.
  - **Fuzzy Matching**: Type after the `!` (e.g., `!code`) to search through your entire history file using advanced fuzzy logic—typos allowed!
  - **Persistent**: Unlike standard history, this is stored in a clean, local `.txt` file and is preserved across plugin updates.
- **🕵️ Incognito Search**:
  - **On-the-Fly**: Append `-i` to any search to open the link in a private browser window.
  - **Smart Parsing**: Smart logic automatically handles incognito flags even at the start or end of raw URLs.
  - **Global Setting**: Enable "Incognito by default" in PowerToys settings to always browse privately.
- **⚙️ Integrated Settings**:
  - **History Display Count**: Choose how many recent results to show at once.
  - **History File Limit**: Automatically truncates your history file (e.g., 3000 lines) to keep performance snappy.
  - **Record History Toggle**: Completely enable or disable history tracking.
  - **Incognito by Default**: Automatically start every search in private mode.
  - **Record Incognito History**: Choose whether private searches should be remembered.
- **🔍 Smart Filtering**:
  - **Instant Suggestions**: Type `@` followed by any letter to instantly see all matching search engines.
- **🚀 Multi-Search Engine Queries**:
  - **Simultaneous Search**: Execute the same search across multiple engines!
  - **Format**: List your engines (with or without `@`), then `:` and your query.
  - _Example_: `yt gh bing : how to bake a cake` searches all three sites at once.
- **🎨 Premium UI/UX**:
  - **Auto-Generated Icons**: Automatically fetches high-resolution favicons for new engines.
  - **Failure Caching**: Intelligent logic stops retrying dead links to prevent log spam.
  - **Adaptive Icons**: Matches your Windows Dark/Light theme perfectly.
  - **Log Viewer**: Access real-time logs directly from the search bar using `-log`.
- **📺 Live YouTube Integration**:
  - **Direct Video Results**: Append `;` to any YouTube search to fetch the top 10 video results directly in PowerToys Run.
  - **Smart Caching**: Results are cached in RAM to save API tokens and provide instant results for repeated searches.
  - **Async Fetching**: Uses `IDelayedExecutionPlugin` to ensure the UI never freezes while talking to Google.
  - **Token Rotation**: Supports multiple API keys in `google_api.txt` for high-volume users.

## ⌨️ Command Guide

| Command             | Action                       | Example               |
| :------------------ | :--------------------------- | :-------------------- |
| `<alias> <query>`   | Search using a custom engine | `yt C# tutorial`      |
| `<e1> <e2> : <query>` | Multi-engine search          | `yt gh : search term` |
| `<URL>`             | Open a URL directly          | `google.com -i`       |
| `<cmd> -i`          | Force Incognito mode         | `yt -i secret`        |
| `yt <query> ;`      | Fetch YouTube video results  | `yt lofi ;`           |
| `!`                 | View recent history          | `!`                   |
| `!<query>`          | Fuzzy search history         | `!cake`               |

### 🛠️ Utility Commands

- `-h` : Show the comprehensive help/utility menu.
- `-a @alias URL` : Add or update an engine instantly.
- `-d @alias` : Delete an engine and its icons.
- `-r` : Reload everything (engines, history, and clears the YouTube cache).
- `-l` : View and edit the `searchEngines.txt` file directly.
- `-his` : View and edit the plugin's history file.
- `-log` : View and edit the plugin's logs.

## 📂 Configuration & Persistence

The plugin stores its data locally in the plugin folder (no registry bloat):

- **`searchEngines.txt`**: Your shortcut definitions.
- **`history.txt`**: Your rich search history (uses a 4-segment format).
- **`Logs.txt`**: Internal logs for troubleshooting.
- **`Images/`**: Cached favicons.

## 🚀 Installation

1. **Build**: Run `build-plugin.bat`.
   - _This performs a clean build, nuking old artifacts for a fresh DLL._
2. **Install**: Run `install-plugin.bat`.
   - _This script is non-destructive:_ it preserves your existing `history.txt` and `searchEngines.txt` even during upgrades.
   - It handles closing and restarting PowerToys automatically.
3. **Setup YouTube (Optional)**:
   - Visit https://console.cloud.google.com and create a [new project](https://console.cloud.google.com/projectcreate)
   - After creating a project, go to "APIs & Services > Library" via the Navigation Menu / Sidebar.
   - Locate "YouTube Data API v3" and hit enable, after it loads the page, create credentials (select the public data option and get the API key).
   - Paste the obtained API key in the `google_api.txt` in the plugin folder (`%LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins\BrowserConnect`).
   - Paste one or more Google API Keys (one per line).
   - This enables the `;` video search feature.

## 📂 Project Structure

```text
BrowserConnect/
├── Main.cs                    # Plugin entry point and logic
├── Handlers/                  # Command handlers modules
├── Services/                  # Services and core engines
├── libs/                      # Dependencies
├── Images/                    # Generated/static icons
├── plugin.json                # Plugin metadata
├── BrowserConnect.csproj      # Project file
├── browserConnect.sln         # Solution file
├── build-plugin.bat           # Build script
└── install-plugin.bat         # Installation script
```

## ⚠️ Troubleshooting

**Plugin doesn't appear in PowerToys Run**

- Check that the plugin is installed in the correct directory. (`%LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins`)
- Restart PowerToys completely.
- Check PowerToys Run settings to ensure the plugin is enabled.

**Search engines not loading**

- Check `%LOCALAPPDATA%\browserConnect\searchEngines.txt` exists.
- Verify the file format is correct (one engine per line: `@alias URL`).

**Browser doesn't open**

- Verify your default browser is installed.
- Check the browser executable is in PATH or standard installation locations.

---

## 📝 Development Notes

- The plugin activates when you type `@` [Editable].
- Multi-engine searches are recorded in history under the `_MULTI` tag.
- The plugin browser primarily using brave, fallsback to default browser if brave is not found. (Should make this to actively use default browser..?)

---

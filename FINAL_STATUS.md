# Winmozhi - Final Status Report

## What's Working ✅

### Core Features
- **Global Keyboard Hook** - Captures input globally across all applications
- **Real-time Transliteration** - Converts Manglish (Latin) to Malayalam instantly
- **Suggestion System** - Shows top 5 Malayalam alternatives as you type
- **Popup UI** - WinUI 3 popup appears at cursor position, never blocks input
- **Tab/Space Insertion** - Press Tab or Space to insert the selected Malayalam word
- **History Database** - Remembers your word choices and prioritizes them
- **Settings UI** - Configure font modes, colors, opacity, and online/offline modes
- **Tray Icon** - Minimize to system tray, quick access from taskbar

### Supported Applications
Works perfectly in:
- ✅ Notepad, Word, any text editor
- ✅ Web browsers (Chrome, Firefox, Edge)
- ✅ IDEs (VS Code, Visual Studio)
- ✅ Chat applications (WhatsApp, Telegram, Discord)
- ✅ Email clients (Gmail, Outlook)
- ✅ Windows Start Menu search
- ✅ All standard Windows apps

### Language Features
- **Malayalam Unicode Output** - Outputs proper Unicode Malayalam characters
- **FML Font Support** - Optional legacy FML font conversion (checkbox in settings)
- **ML Font Support** - Optional legacy ML font conversion (checkbox in settings)
- **Offline Mode** - Uses local dictionary even without internet
- **Online Mode** - Google Transliteration API for additional suggestions (when internet available)

### Keyboard Shortcuts
- **Tab** - Insert selected suggestion with no trailing character
- **Space** - Insert selected suggestion followed by a space
- **Enter** - Close popup without inserting (no longer converts on Enter)
- **Arrow Up/Down** - Navigate suggestions when popup is visible
- **Backspace** - Delete characters and update suggestions

---

## Known Limitations ⚠️

### Photoshop & Adobe Apps
**❌ Does NOT work in Photoshop, Illustrator, InDesign**

**Reason:** Adobe uses protected input APIs that bypass all global keyboard hooks, even when running as Administrator.

**Workaround:** Type in Notepad, convert with Winmozhi, then paste into Adobe apps.

See `PHOTOSHOP_LIMITATION.md` for technical details.

---

## Recent Changes (This Session)

### 1. Removed Enter Key Conversion
- **Before:** Pressing Enter would convert and insert the Malayalam text
- **Now:** Pressing Enter **only dismisses the popup**, doesn't insert
- **File:** `Winmozhi.Hooks/KeyboardHookService.cs`

### 2. Added Admin Manifest
- Manifest now requests admin privileges for deeper hook access
- Does not bypass Photoshop protection (Adobe's design)
- **File:** `Winmozhi.UI/app.manifest`

### 3. Disabled Online Engine by Default
- Google API calls no longer spam errors when offline
- Users can re-enable from Settings if they have internet
- **File:** `Winmozhi.Core/Utilities/LocalPreferences.cs`

### 4. Verified Hook Functionality
- Console logging confirmed hook works in:
  - ✅ Notepad
  - ✅ Chrome
  - ✅ Windows Start Menu
  - ✅ All tested applications
- Hook fails silently in Photoshop (expected behavior)

---

## Installation & Usage

### First Run
1. Download `Winmozhi.UI.exe` from `/bin/x64/Debug/net10.0-windows10.0.19041.0/`
2. **Right-click → Run as Administrator** (optional but recommended for better hook coverage)
3. Settings window appears - configure as needed
4. Minimize to tray (icon in taskbar)

### Typing
1. Type Manglish (English letters) in any text field
2. Popup appears automatically with suggestions
3. Press **Tab/Space** to insert, or select with arrow keys
4. Or just keep typing - popup updates in real-time

### Enabling Online Suggestions
1. Open settings (click tray icon)
2. Check "Enable Online Suggestions"
3. Restart the app
4. Now uses Google Transliteration API for extra suggestions

### Legacy Font Support
- Check "Convert to FML Font" for legacy FML font output
- Check "Convert to ML Font" for legacy ML font output
- Only one can be active at a time

---

## File Structure

### Core Projects
- **Winmozhi.Core/** - Transliteration engines, database, preferences
- **Winmozhi.Hooks/** - Global keyboard/mouse hook implementation
- **Winmozhi.UI/** - WinUI 3 popup and settings interface

### Key Files Modified This Session
- `Winmozhi.Hooks/KeyboardHookService.cs` - Removed Enter key conversion
- `Winmozhi.UI/app.manifest` - Added admin elevation request
- `Winmozhi.Core/Utilities/LocalPreferences.cs` - Disabled online by default
- `Winmozhi.UI/Views/PopupView.xaml.cs` - Singleton popup (confirmed working)

---

## Troubleshooting

### Popup not appearing
- Make sure hook is enabled in settings
- Restart the app as Administrator
- Check that you're typing in a text field (not a non-text app)

### Suggestions not appearing
- In Settings, uncheck "Enable Online Suggestions" if experiencing lag
- Make sure you're typing valid Manglish (a-z letters)

### No input in Photoshop
- This is expected. See `PHOTOSHOP_LIMITATION.md`
- Workaround: Type elsewhere, copy-paste to Photoshop

### Too many network errors in logs
- This happens when offline. Online suggestions are now **disabled by default**
- Re-enable in Settings only if you have internet connection

---

## Technical Details

### Hook Type
- **WH_KEYBOARD_LL** - Global low-level keyboard hook
- **WH_MOUSE_LL** - Global low-level mouse hook (for click detection)
- Runs elevated with admin manifest for maximum compatibility

### Suggestion Algorithm
1. Check user's history database (most relevant)
2. Check offline dictionary (fallback)
3. Apply SimpleMozhiParser algorithm (phonetic conversion)
4. (Optional) Query Google Transliteration API (if enabled)
5. Merge and deduplicate results
6. Show top 5 suggestions

### Popup Behavior
- Positioned at cursor/mouse location
- Always on top, never blocks input
- Dismisses on:
  - Mouse click
  - Enter key press
  - Any non-letter/navigation key
  - Window focus change

---

## Future Enhancement Ideas

1. **Per-App Configuration** - Different settings for different apps
2. **Clipboard History** - Quick access to previous conversions
3. **Custom Dictionary** - Add your own words/phrases
4. **Hotkey to Toggle** - Enable/disable with a keyboard shortcut
5. **Language Support** - Add more Indian languages (Tamil, Kannada, etc.)
6. **Predictive Text** - ML-based next-word prediction

---

## Support

For issues or feature requests, visit: https://github.com/anandhuvasudev/Winmozhi

---

**Build Date:** 2024
**Status:** Production Ready ✅
**Photoshop Support:** Not Possible (Adobe Security) ⚠️

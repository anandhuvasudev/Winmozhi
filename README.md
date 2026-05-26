<div align="center">

<img src="Winmozhi.UI/Assets/Square44x44Logo.scale-200.png" alt="Winmozhi Logo" width="150"/>

# ⌨️ Winmozhi (വിൻമൊഴി)

### The Ultimate Manglish → Malayalam Keyboard for Windows

*Type Malayalam effortlessly across your entire system — fast, native, elegant, and intelligent.*

<br/>

![Platform](https://img.shields.io/badge/Platform-Windows%2011-0078D4?style=for-the-badge&logo=windows)
![Framework](https://img.shields.io/badge/Framework-WinUI%203%20%7C%20.NET%2010-8A2BE2?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-2EA043?style=for-the-badge)
![Status](https://img.shields.io/badge/Status-Active%20Development-orange?style=for-the-badge)

<br/>

### ✨ Native • Fast • Open Source • Beautiful

</div>

---

# 🌟 About Winmozhi

**Winmozhi** is a blazing-fast, system-wide Malayalam transliteration keyboard for Windows that converts **Manglish → Malayalam** in real time.

Built natively using **WinUI 3** and **.NET 10**, Winmozhi integrates deeply with Windows and delivers a smooth, lightweight, and modern typing experience that feels like a natural part of the operating system.

Whether you're chatting, coding, designing, editing videos, or writing documents — Winmozhi works everywhere.

---

# ✨ Features

## 🚀 Hybrid Transliteration Engine

Winmozhi combines multiple intelligent systems to provide extremely accurate Malayalam typing.

### Core Technologies

- ⚡ Ultra-fast zero-allocation parser
- 📚 Offline Trie-based prediction engine
- 🌐 Google Input Tools fallback engine
- 🧠 Smart word ranking & adaptive prediction
- 🔄 Real-time transliteration pipeline

---

## 🧠 Intelligent Fuzzy Matching

Winmozhi automatically understands phonetic variations and typing mistakes.

| You Type | Winmozhi Understands |
|:---|:---|
| `sukham` | സുഖം |
| `sugham` | സുഖം |
| `njan` | ഞാൻ |
| `ente` | എൻ്റെ |
| `malayalm` | മലയാളം |

### Powered By

- Phonetic normalization
- Levenshtein distance matching
- Adaptive ranking logic

---

## ⚡ System-Wide Hotkey

Instantly enable or disable transliteration anywhere using:

<div align="center">

## <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>M</kbd>

</div>

Perfect for switching between English and Malayalam while typing.

---

## 🎨 Beautiful Native Windows UI

Designed specifically for **Windows 11** with a clean and modern native aesthetic.

### UI Highlights

- 🌈 Acrylic & Mica materials
- ✨ Smooth animations
- 🪟 Floating suggestion popup
- 🎯 Cursor-following predictions
- 🌙 Light & dark theme support
- 🔍 Adjustable font scaling
- 🎛️ Opacity customization

---

## 🔤 Legacy Malayalam Font Support

Winmozhi supports direct typing into legacy Malayalam font workflows.

### Compatible With

- Adobe Photoshop
- Adobe Premiere Pro
- PageMaker
- Legacy DTP software
- FML / ML font mappings

Perfect for professional Malayalam publishing workflows.

---

## 📖 Personal Dictionary & Learning

Winmozhi learns from your typing habits over time.

### Smart Learning Features

- 💾 Local SQLite-based storage
- 🧠 Personalized predictions
- 📈 Frequency-based ranking
- 🔒 Fully offline learning
- ⚡ Faster suggestions over time

Your data stays completely on your device.

---

# 📸 Screenshots

<div align="center">

| Settings Window | Suggestion Popup |
|:---:|:---:|
| <img src="https://via.placeholder.com/500x300.png?text=Settings+Window" width="450"/> | <img src="https://via.placeholder.com/500x300.png?text=Suggestion+Popup" width="450"/> |

</div>

<br/>

> Replace the placeholder screenshots above with actual application screenshots.

---

# 🚀 Installation

## Option 1 — Direct Download *(Coming Soon)*

1. Visit the Releases page
2. Download:
   - `Winmozhi_Installer.exe`
   - `.msix` package
3. Install & launch 🎉

Winmozhi will automatically appear in your system tray.

---

## Option 2 — Build From Source

### Requirements

- Visual Studio 2022 (17.8+)
- .NET 10 SDK
- Windows App SDK
- WinUI 3 workload

### Clone Repository

```bash
git clone https://github.com/anandhuvasudev/Winmozhi.git
```

### Build Steps

```bash
# Open the solution
Winmozhi.slnx

# Select architecture
x64 / ARM64

# Set startup project
Winmozhi.UI

# Run project
F5
```

---

# 🕹️ How to Use

## 1️⃣ Launch Winmozhi

The `മ` icon will appear in your system tray.

---

## 2️⃣ Open Any Application

Winmozhi works seamlessly in:

- Browsers
- Microsoft Word
- WhatsApp
- VS Code
- Photoshop
- Premiere Pro
- Notepad
- Discord
- And almost everywhere on Windows

---

## 3️⃣ Start Typing Manglish

Example:

```text
njan malayali aanu
```

Suggestions will automatically appear near your text cursor.

---

## 4️⃣ Select Suggestions

| Key | Action |
|---|---|
| `Space` | Insert top suggestion |
| `Enter` | Confirm exact word |
| `↑ ↓` | Navigate suggestions |

---

## 5️⃣ Toggle Transliteration

Press:

```text
Ctrl + Shift + M
```

to instantly switch back to normal English typing.

---

# ⚙️ Architecture & Tech Stack

| Component | Technology |
|---|---|
| Frontend | WinUI 3 |
| Backend | C# 13/14 + .NET 10 |
| Architecture | MVVM |
| Database | SQLite (`sqlite-net-pcl`) |
| Prediction Engine | Trie + DFS |
| Parsing Engine | `ReadOnlySpan<char>` |
| Keyboard Hooks | Win32 `WH_KEYBOARD_LL` |

---

# 🧩 Performance Focus

Winmozhi is engineered for speed and responsiveness.

### Optimizations

- ⚡ Sub-millisecond keystroke processing
- 🧠 Cached prediction pipeline
- 📦 Zero-allocation parsing
- 🪶 Lightweight rendering
- 🔌 Native Win32 hooks

The result is an incredibly smooth typing experience without interrupting your workflow.

---

# 🤝 Contributing

Winmozhi is proudly **Open Source ❤️**

Contributions are always welcome.

---

## Ways to Contribute

### 📚 Improve the Dictionary

Expand the offline corpus by editing:

```text
ManglishCorpus.csv
```

### 🐞 Fix Bugs

Check the Issues section and help solve problems.

### 💡 Suggest Features

Open an issue or start a discussion.

---

## Contribution Workflow

```bash
# Fork the repository

# Create a feature branch
git checkout -b feature/AmazingFeature

# Commit your changes
git commit -m "Add AmazingFeature"

# Push changes
git push origin feature/AmazingFeature

# Open a Pull Request 🚀
```

---

# 📜 License

Distributed under the **MIT License**.

See the `LICENSE` file for more information.

---

# 🙏 Acknowledgements

Special thanks to:

- **H.NotifyIcon** — system tray implementation
- **Google Input Tools** — transliteration fallback
- The amazing Malayalam open-source community ❤️

---

<div align="center">

# 💙 Winmozhi

### Bringing beautiful Malayalam typing to Windows.

Made with ❤️ for the Malayalam community.

</div>

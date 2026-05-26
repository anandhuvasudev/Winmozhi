⌨️ Winmozhi (വിൻമൊഴി)
<div align="center"> <img src="Winmozhi.UI/Assets/Square150x150Logo.png" alt="Winmozhi Logo" width="140"/>
The Ultimate Manglish → Malayalam Keyboard for Windows

Type Malayalam effortlessly anywhere across your system — fast, native, and beautiful.

<br/>










<br/>

Winmozhi is a blazing-fast system-wide transliteration keyboard that converts Manglish into Malayalam in real-time.
Built natively for Windows using WinUI 3 and .NET 10, it works silently in the background and feels like a true part of the OS.

</div>
✨ Features
🚀 Hybrid Transliteration Engine

Winmozhi combines multiple intelligent systems to deliver extremely accurate Malayalam typing:

⚡ Ultra-fast zero-allocation parser
📚 Predictive offline Trie dictionary with DFS
🌐 Google Input Tools fallback engine
🧠 Smart word prediction & ranking
🧠 Intelligent Fuzzy Matching

Typos? No problem.

Winmozhi understands phonetic variations automatically.

You Type	Winmozhi Understands
sukham	സുഖം
sugham	സുഖം
njan	ഞാൻ
ente	എൻ്റെ

Powered by:

Phonetic normalization
Levenshtein distance calculations
Adaptive prediction logic
⚡ System-Wide Hotkey

Quickly toggle transliteration anywhere:

Ctrl + Shift + M

Perfect when switching between English and Malayalam instantly.

🎨 Beautiful Native UI

Designed specifically for Windows 11.

Features:

Acrylic / Mica materials
Smooth animations
Floating suggestion popup
Custom opacity & theme settings
Adjustable font scaling

Minimal. Elegant. Native.

🔤 Legacy Font Support (FML / ML)

Type directly into legacy Malayalam font workflows used in:

Adobe Photoshop
Premiere Pro
PageMaker
Old DTP software

Supports:

FML
ML
Other legacy keyboard mappings
📖 Personal Dictionary

Winmozhi learns from your typing habits over time.

Stores frequently used words locally
Prioritizes your vocabulary
Fully offline SQLite-based learning system
🛠️ System Tray Integration

Runs quietly in the background with:

Quick enable/disable access
Settings shortcut
Exit controls
Lightweight memory usage
📸 Screenshots

Replace the placeholders below with actual screenshots.

<div align="center">
Settings Window	Suggestion Popup

	
</div>
🚀 Installation
Option 1 — Direct Download (Coming Soon)
Go to the Releases page
Download:
Winmozhi_Installer.exe
or .msix
Install & launch 🎉

Winmozhi will automatically appear in your system tray.

Option 2 — Build From Source
Requirements
Visual Studio 2022 (17.8+)
.NET 10 SDK
Windows App SDK
WinUI 3 workload
Clone the Repository
git clone https://github.com/anandhuvasudev/Winmozhi.git
Build & Run
Open Winmozhi.slnx
Set platform architecture:
x64
or ARM64
Set Winmozhi.UI as Startup Project

Select:

Winmozhi (Unpackaged)
Press F5
🕹️ How to Use
Launch Winmozhi
The മ icon appears in the system tray
Open any app:
Browser
Word
WhatsApp
Photoshop
VS Code
Start typing in Manglish:
njan malayali aanu
Suggestions appear automatically near your cursor
Press:
Space → Insert top suggestion
Enter → Confirm word
↑ ↓ → Navigate suggestions
Toggle Transliteration
Ctrl + Shift + M

Disable instantly whenever you need normal English typing.

⚙️ Architecture & Tech Stack

Winmozhi is engineered for speed, responsiveness, and native Windows integration.

Component	Technology
Frontend	WinUI 3
Backend	C# 13/14 + .NET 10
Architecture	MVVM
Database	SQLite (sqlite-net-pcl)
Offline Prediction	Trie + DFS
Parsing Engine	ReadOnlySpan<char>
Keyboard Hook	Win32 WH_KEYBOARD_LL
🧩 Performance Focus

Winmozhi intercepts and processes keystrokes in under 1ms without interrupting system workflows.

Optimizations include:

Zero-allocation parsing
Cached prediction pipelines
Native Win32 hooks
Lightweight rendering
🤝 Contributing

Winmozhi is proudly Open Source ❤️

Contributions are always welcome.

Ways You Can Help
📚 Expand the Dictionary

Improve the offline corpus:

ManglishCorpus.csv
🐞 Fix Bugs

Check the Issues tab and help solve open problems.

💡 Suggest Features

Have an idea?

Open an Issue
Create a Pull Request
Start a discussion
Contribution Steps
# Fork the project

# Create your feature branch
git checkout -b feature/AmazingFeature

# Commit changes
git commit -m "Add AmazingFeature"

# Push branch
git push origin feature/AmazingFeature

Then open a Pull Request 🚀

📜 License

Distributed under the MIT License.

See the LICENSE file for more information.

🙏 Acknowledgements

Special thanks to:

H.NotifyIcon — System tray implementation
Google Input Tools — Online transliteration fallback
The amazing Malayalam open-source community ❤️
<div align="center">
Made with ❤️ for മലയാളം
Fast. Native. Beautiful.
</div>

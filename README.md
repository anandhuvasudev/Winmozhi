⌨️ Winmozhi (വിൻമൊഴി)
<div align="center">
<img src="Winmozhi.UI/Assets/Square150x150Logo.png" alt="Winmozhi Logo" width="120" />
<br/>
<strong>The Ultimate Manglish to Malayalam Keyboard for Windows.</strong>
<br/>
<i>Type Malayalam effortlessly, anywhere across your system.</i>
<br/><br/>
![alt text](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D7?logo=windows&style=flat-square)

![alt text](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&style=flat-square)

![alt text](https://img.shields.io/badge/UI-WinUI%203-00569E?style=flat-square)

![alt text](https://img.shields.io/badge/License-MIT-success?style=flat-square)

![alt text](https://img.shields.io/badge/PRs-welcome-brightgreen.svg?style=flat-square)
</div>
<br/>
Winmozhi is a blazing-fast, system-wide transliteration tool that converts English (Manglish) keystrokes into beautiful Malayalam text in real-time. Built natively for Windows using WinUI 3 and .NET 10, it operates silently in the background, offering an incredibly smooth and native typing experience.
✨ Features
🚀 Hybrid Transliteration Engine: Combines an ultra-fast zero-allocation algorithmic parser, a predictive offline Trie dictionary with Deep-First Search (DFS), and Google's Input Tools API for unmatched precision.
🧠 Intelligent Fuzzy Matching: Makes typos a thing of the past. Typing sugham or sukham instantly recognizes the correct word using Phonetic Normalization & Levenshtein distance calculations.
⚡ Global Hotkey: Instantly toggle transliteration ON or OFF system-wide by pressing Ctrl + Shift + M.
🎨 Beautiful Native UI: An elegant, un-intrusive suggestion popup utilizing Windows 11 Acrylic/Mica materials. Fully customizable (colors, opacity, font scale).
🔤 Legacy Font Support (FML / ML): Native output routing for legacy fonts like FML and ML, making Malayalam typing effortless in software like Adobe Photoshop and Premiere Pro.
📖 Personal Dictionary: Learns from your typing habits using a lightweight local SQLite database to prioritize the words you use most.
🛠️ System Tray Integration: Quick access, settings, and silent background operation right from your taskbar.
📸 Screenshots
(Note to developer: Add your screenshots here by replacing the placeholder links!)
<p align="center">
<img src="https://via.placeholder.com/600x400.png?text=Settings+Window+Screenshot" width="48%" />
<img src="https://via.placeholder.com/600x400.png?text=Typing+Popup+Screenshot" width="48%" />
</p>
🚀 Installation
Option 1: Direct Download (Coming Soon)
Go to the Releases page.
Download the latest Winmozhi_Installer.exe or .msix file.
Install and run! Winmozhi will appear in your System Tray.
Option 2: Build from Source
Ensure you have the following installed:
Visual Studio 2022 (Version 17.8+)
.NET 10 SDK
Windows App SDK & WinUI 3 workload via Visual Studio Installer.
Steps:
Clone the repository:
code
Bash
git clone https://github.com/anandhuvasudev/Winmozhi.git
Open Winmozhi.slnx in Visual Studio.
Ensure the platform architecture is set to x64 (or ARM64 for Snapdragon devices).
Set Winmozhi.UI as the Startup Project.
Select the Winmozhi (Unpackaged) launch profile.
Press F5 to build and run!
🕹️ How to Use
Launch Winmozhi. The മ logo will appear in your system tray.
Open any app (Word, Browser, WhatsApp, Photoshop).
Start typing in Manglish (e.g., njan malayali aanu).
A beautiful popup will appear near your cursor.
Press Space or Enter to insert the first suggestion, or use the Up/Down Arrow Keys to choose another word.
Toggle ON/OFF: Press Ctrl + Shift + M at any time to temporarily disable the keyboard hook and type normally in English.
⚙️ Architecture & Tech Stack
Winmozhi is engineered for absolute performance. The core intercepts keystrokes via low-level Win32 API hooks (WH_KEYBOARD_LL) in under 1ms without interrupting system workflows.
Frontend: WinUI 3 / Windows App SDK (XAML/C#).
Backend: C# 13/14, .NET 10.
Offline Engine: Predictive Trie Dictionary (DFS) + ReadOnlySpan<char> zero-allocation parser.
Database: sqlite-net-pcl for lightning-fast localized user-learning.
Architecture Pattern: MVVM using CommunityToolkit.Mvvm.
🤝 Contributing
Winmozhi is proudly Open Source! We welcome contributions from the community to make Malayalam typing better for everyone.
How you can help:
Add to the Corpus: Help us expand the offline ManglishCorpus.csv dictionary.
Bug Fixes: Check the Issues tab and tackle open bugs.
Feature Requests: Have a cool idea? Open a PR or an Issue!
To contribute:
Fork the Project.
Create your Feature Branch (git checkout -b feature/AmazingFeature).
Commit your Changes (git commit -m 'Add some AmazingFeature').
Push to the Branch (git push origin feature/AmazingFeature).
Open a Pull Request.
📜 License
This project is licensed under the MIT License. See the LICENSE file for more information.
🙏 Acknowledgements
H.NotifyIcon for the beautiful system tray implementation.
Google Input Tools for the online fallback engine.
The amazing Malayalam open-source community.
<br/>
<div align="center">
<i>Made with ❤️ for Malayalam</i>
</div>
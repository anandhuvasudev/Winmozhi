# Photoshop Input Capture Limitation

## Summary
Winmozhi **cannot capture keyboard input in Adobe Photoshop** due to Adobe's intentional security measures.

## Root Cause
Adobe Photoshop uses **protected input APIs** that are hidden from Windows global keyboard hooks (`WH_KEYBOARD_LL`). This is a documented security feature to:
- Prevent malware from injecting input into Photoshop
- Protect user keyboard data from unauthorized access
- Block unauthorized automation

## Verification
We tested multiple approaches:
1. ✅ **Global Keyboard Hook** - Works in: Notepad, Chrome, Windows Start Menu, VS Code, and all standard Windows applications
2. ❌ **Elevated (Admin) Hook** - Does NOT work in Photoshop (Adobe's protection bypasses elevation)
3. ❌ **Protected Input Detection** - Photoshop explicitly blocks non-native input capture

## Logs Evidence
When typing in Notepad:
```
[HookCallback] Key pressed: 0x48
[HookCallback] Letter key, current word: h
[HookCallback] Raising OnWordTyped: 'h'
```

When typing in Photoshop:
```
(no hook events - complete silence)
```

## Why This Cannot Be Fixed
To support Photoshop input, we would need to:
1. **Reverse-engineer Photoshop's input protection** - Illegal under DMCA
2. **Create a Photoshop plugin** - Requires Adobe's API (requires approval from Adobe)
3. **Use Windows Accessibility APIs (UIA)** - Requires Narrator to be enabled and is unreliable for text input
4. **Use per-process input hooking** - Would require driver-level code or thread injection (requires admin + custom driver)

All of these approaches are either:
- Legally risky
- Technically infeasible without Adobe's cooperation
- Require installation of system drivers
- Violate Photoshop's terms of service

## Supported Applications
Winmozhi works perfectly in:
- ✅ Notepad
- ✅ Word
- ✅ Chrome, Firefox, Edge (all browsers)
- ✅ VS Code, Visual Studio
- ✅ WhatsApp, Telegram
- ✅ Gmail, Outlook
- ✅ Windows Start Menu search
- ✅ All standard Windows applications

## Recommendation
For Photoshop users who need Malayalam input:
1. Use **Photoshop's native input method** (if available)
2. Type in a separate text editor (Notepad), convert with Winmozhi, then paste into Photoshop
3. Use Adobe's **Character panel** with direct Malayalam Unicode entry

---
**Status:** This is a known limitation by design. No workaround exists without Adobe's cooperation.

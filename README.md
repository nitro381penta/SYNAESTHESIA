# SYNAESTHESIA
## VR Sound Visualizer for Meta Quest

![SYNAESTHESIA](Assets/Docs/Synasthesia_2.png)

### Features
- VR-native, world-space UI for playback (play/pause/next/loop/mute/settings, etc.)
- Six music visualization styles (Sparkles, Fireworks, Waves, Butterfly, Psychedelic/Kaleido, etc.)
- Microphone mode (Quest mic, live reactive visuals)
- Spatial ambients near bubbles and central music player
- Works in Editor (PC/Link) and on Meta Quest 3 (OpenXR, URP)

### Requirements
- Unity 6 (6000 LTS) with URP
- Packages: OpenXR, XR Interaction Toolkit, XR Hands
- Target: Meta Quest 3 (Android, OpenXR, Single Pass Instanced)
- Audio: 48 kHz system sample rate

### Quick start
1. Clone the repo (enable Git LFS).
2. Open in Unity 6 (URP). When prompted, let Unity upgrade materials.
3. Open scene: Assets/_Scenes/Soundspace.unity.
4. Project Settings → Audio
- System Sample Rate: 48,000 Hz
- DSP Buffer Size: Default or Best Latency
5. Player Settings → Android
- Scripting Backend: IL2CPP
- Target Architectures: ARM64
- XR: OpenXR (Meta Quest)
- Stereo: Single Pass Instanced (SPI)
6. Mic permission. Make sure Plugins/Android/AndroidManifest.xml contains: <uses-permission android:name="android.permission.RECORD_AUDIO"/>
- Add bootstrap
  using:UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif
public class MicPermissionBootstrap : MonoBehaviour {
  void Start() {
    #if UNITY_ANDROID && !UNITY_EDITOR
    if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        Permission.RequestUserPermission(Permission.Microphone);
    #endif
  }
}
7. Run in Editor (PC or Link): click the Microphone Bubble, then Record.
Build to Quest: first launch will show the Android mic prompt—allow it.

### Controls
- Tap a bubble → enter music mode for that bubble’s playlist.
- Tap the Microphone Bubble → show mic canvas; press Record to visualize your own sound.
- Settings button → choose visualizer mode; press again to go back.
- Home button → exit to all bubbles.

### Build instructions (Quest)
- File → Build Settings → Android → Switch Platform.
- Add Soundspace.unity to Scenes in Build.
- Player Settings: see Requirements above (ARM64, OpenXR, SPI, Linear).
- Build & Run (USB or Wi-Fi).
- First mic use → Android prompt → Allow.

### Demo
[![Watch the demo](Assets/Docs/Synasthesia_2.png)](Assets/Docs/copy_2FD6CD2A-0B09-4F3A-95BE-4CF0491170C9.mov)


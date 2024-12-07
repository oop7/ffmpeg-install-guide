# ffmpeg-install-guide

## **Overview**

This repository provides scripts and instructions for installing FFmpeg on Windows, macOS, and various Linux distributions. It includes batch files for Windows, detailed installation guides for macOS and Linux, and a video tutorial.

## **Installation Instructions**

### **Windows**

**Method 1:** Using `install_ffmpeg_7z.bat`

**Requirements:** 7-zip or any 7-zip fork or WinRAR installed.

**Instructions:**

- [Download](https://github.com/oop7/ffmpeg-install-guide/releases/download/v1.0/ffmpeg_7z.bat) the batch file.
- prompted by User Account Control (UAC), click Yes to grant the necessary permissions.
- Done.

**Method 2:** Using ``install_ffmpeg_zip.bat``

**Requirements:** No additional software needed.

**Instructions:**

- [Download](https://github.com/oop7/ffmpeg-install-guide/releases/download/v1.0/ffmpeg_zip.bat) the batch file.
- prompted by User Account Control (UAC), click Yes to grant the necessary permissions.
- Done.

**Verify Installation:**
```bash
ffmpeg -version
```

**Method 3:**

1. Go to the [FFmpeg official website](https://ffmpeg.org/download.html) or [Codex FFmpeg](https://github.com/GyanD/codexffmpeg/releases).

- Under the "Windows Build" section, download the latest `ffmpeg-release-essentials.zip` or `ffmpeg-release-full.zip` depending on your needs.

2. **Extract the Files:**

- Extract the downloaded ZIP file to a folder, e.g., `C:\ffmpeg`.

3. **Add FFmpeg to System Path:**

- Right-click on "This PC" or "Computer" on the desktop or in File Explorer and select "Properties."
- Click on "Advanced system settings" on the left side.
- Click on the "Environment Variables" button.
- In the "System variables" section, find the `Path` variable and select it, then click "Edit."
- Add a new entry with the path to the `bin` folder inside your FFmpeg directory (e.g., `C:\ffmpeg\bin`).
- Click "OK" to close all dialogs.

**Verify Installation:**
```bash
ffmpeg -version
```

### **Video Tutorial For Windows**

Link:[FFmpeg Installation Tutorial](https://youtu.be/5xgegeBL0kw?si=0CyS9BNYHeBiPDxd)

## **Linux**

**Debian/Ubuntu**

```bash
sudo apt install ffmpeg
```

**Fedora**

```bash
sudo dnf install ffmpeg
```

**Arch Linux**

```bash
sudo pacman -S ffmpeg
```

**Snap (Cross-Distribution)**

```bash
sudo snap install ffmpeg
```

**Verify Installation:**
```bash
ffmpeg -version
```

### **macOS**

Using Homebrew (Recommended)

1. Install Homebrew (if not installed):
```bash
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
```

2. Install FFmpeg:
```bash
brew install ffmpeg
```

3. Verify Installation:

```bash
ffmpeg -version
```

Using MacPorts (Alternative)

1. Install MacPorts from [here](https://www.macports.org/install.php).

2. Install FFmpeg:
```bash
sudo port install ffmpeg
```

3. Verify Installation:
```bash
ffmpeg -version
```

### **Video Tutorial For MacOS**

Link:[FFmpeg Installation Tutorial](https://youtu.be/dJ8y-VlMNAo?si=dLee6hrVVoJeCvVO)

### License

This project is licensed under the MIT License - see the [LICENSE] file for details.

### Acknowledgments

Thank you to the FFmpeg community for their ongoing development and support.

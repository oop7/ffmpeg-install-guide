# FFmpeg Installation Guide

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A comprehensive guide and automation scripts for installing FFmpeg across multiple operating systems. This repository provides easy-to-follow instructions and automated installation scripts to simplify the FFmpeg setup process.

## Table of Contents
- [Features](#features)
- [Windows Installation](#windows-installation)
  - [Automated Installation](#automated-installation)
  - [Manual Installation](#manual-installation)
- [macOS Installation](#macos-installation)
- [Linux Installation](#linux-installation)
- [Verification](#verification)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)
- [License](#license)

## Features
- Automated installation scripts for Windows
- Cross-platform installation guides
- Video tutorials for Windows and macOS
- Multiple installation methods for each operating system
- System path configuration included

## Windows Installation

### Automated Installation

#### Method 1: Using 7-Zip (Recommended)
**Prerequisites:**
- 7-Zip, WinRAR, or any 7-Zip fork installed

**Steps:**
1. Download [`install_ffmpeg_7z.bat`](https://github.com/oop7/ffmpeg-install-guide/releases/download/v1.0/ffmpeg_7z.bat)
2. Run the script as administrator (UAC prompt will appear)
3. Wait for the installation to complete

#### Method 2: Using ZIP (No Dependencies)
**Prerequisites:**
- None (uses built-in Windows tools)

**Steps:**
1. Download [`install_ffmpeg_zip.bat`](https://github.com/oop7/ffmpeg-install-guide/releases/download/v1.0/ffmpeg_zip.bat)
2. Run the script as administrator (UAC prompt will appear)
3. Wait for the installation to complete

### Manual Installation
1. Download FFmpeg from one of these sources:
   - [FFmpeg Official Website](https://ffmpeg.org/download.html)
   - [Codex FFmpeg Releases](https://github.com/GyanD/codexffmpeg/releases)

2. Choose the appropriate package:
   - `ffmpeg-release-essentials.zip` for basic functionality
   - `ffmpeg-release-full.zip` for complete feature set

3. System Setup:
   1. Extract the ZIP file to a permanent location (e.g., `C:\ffmpeg`)
   2. Add FFmpeg to System Path:
      - Open System Properties (Right-click `This PC` → Properties)
      - Click `Advanced system settings`
      - Click `Environment Variables`
      - Under `System variables`, select `Path` and click `Edit`
      - Add new entry: `C:\ffmpeg\bin` (adjust path if needed)
      - Click `OK` on all windows

📺 **Video Tutorial:** [FFmpeg Windows Installation Guide](https://youtu.be/5xgegeBL0kw)

## macOS Installation

### Using Homebrew (Recommended)
```bash
# Install Homebrew (if not installed)
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"

# Install FFmpeg
brew install ffmpeg
```

### Using MacPorts
1. Install [MacPorts](https://www.macports.org/install.php)
2. Run the installation command:
```bash
sudo port install ffmpeg
```

📺 **Video Tutorial:** [FFmpeg macOS Installation Guide](https://youtu.be/dJ8y-VlMNAo)

## Linux Installation

### Package Managers

**Ubuntu/Debian:**
```bash
sudo apt update
sudo apt install ffmpeg
```

**Fedora:**
```bash
sudo dnf install ffmpeg
```

**Arch Linux:**
```bash
sudo pacman -S ffmpeg
```

### Universal Installation
**Using Snap:**
```bash
sudo snap install ffmpeg
```

## Verification
After installation, verify FFmpeg is correctly installed:
```bash
ffmpeg -version
```

## Troubleshooting

### Common Issues
1. **Windows Path Not Updated:**
   - Log out and log back in
   - Or restart your computer

2. **Permission Denied:**
   - Ensure you're running scripts as administrator
   - Check file permissions

3. **Installation Failed:**
   - Try the alternative installation method
   - Check your internet connection
   - Verify system requirements

## Contributing
Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments
- FFmpeg development team and community
- Contributors to this installation guide
- Package maintainers across different platforms

# FFmpeg Installation Guide

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A comprehensive guide and automation scripts for installing FFmpeg across multiple operating systems. This repository provides easy-to-follow instructions and automated installation scripts to simplify the FFmpeg setup process.

## Table of Contents
- [Features](#features)
- [Windows Installation](#windows-installation)
  - [Automated Installation (Recommended)](#automated-installation-recommended)
  - [Manual Installation](#manual-installation)
- [macOS Installation](#macos-installation)
- [Linux Installation](#linux-installation)
- [Verification](#verification)
- [Building from Source](#building-from-source)
- [Contributing](#contributing)
- [License](#license)

## Features
- **Modern GUI installer for Windows** with automatic updates
- Cross-platform installation guides
- Video tutorials for Windows and macOS
- Multiple installation methods for each operating system
- System path configuration included
- Real-time download progress and hash verification

## Windows Installation

### Automated Installation (Recommended)

#### FFmpeg Installer GUI
**The easiest way to install FFmpeg on Windows**

![FFmpeg Installer Screenshot](https://github.com/user-attachments/assets/5514c647-80c4-4f8e-a651-93a035b9ac69)

**Features:**
- Clean, modern Windows Forms interface
- Real-time download progress with speed indicator
- Automatic file integrity verification (SHA256)
- System PATH configuration
- Administrator privilege handling
- Built-in update checker
- Multiple extraction methods (portable 7z, COM objects, ZIP)

**Download & Usage:**
1. Download the latest [`FFmpegInstaller.exe`](https://github.com/oop7/ffmpeg-install-guide/releases/latest) from releases
2. Run as administrator (UAC prompt will appear automatically)
3. Click "Install FFmpeg" and wait for completion
4. Restart your command prompt to use `ffmpeg`

**Version:** 2.0.0  
**Developer:** oop7  
**Source Code:** Available in this repository  
**Requirements:** .NET 6.0 Runtime (usually pre-installed on Windows 10/11)

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

## Building from Source

### Prerequisites
- .NET 6.0 SDK or later
- Windows 10/11
- Visual Studio 2022 or VS Code (optional)

### Build Instructions
```powershell
# Clone the repository
git clone https://github.com/oop7/ffmpeg-install-guide.git
cd ffmpeg-install-guide

# Build the application
dotnet publish FFmpegInstaller.csproj -c Release -p:PublishSingleFile=true --output ./publish

# The executable will be in the ./publish directory
```

### Development
```powershell
# Run in development mode
dotnet run --project FFmpegInstaller.csproj

# Or open in Visual Studio
start FFmpegInstaller.sln
```

### Common Issues
1. **Windows Path Not Updated:**
   - Restart your command prompt
   - Log out and log back in
   - Or restart your computer

2. **Permission Denied:**
   - Ensure you're running as administrator
   - Check antivirus software blocking the installation

3. **FFmpeg command not found after installation:**
   - Restart command prompt/PowerShell
   - Check if PATH was updated: `echo $env:PATH` (PowerShell) or `echo %PATH%` (CMD)
   - Manually add to PATH if needed

## Contributing
Contributions are welcome! Please feel free to submit a Pull Request.

### How to Contribute:
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

# 📝 StickyNotes-inator

> **A beautiful, modern sticky notes application for Windows with Material Design styling**

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey.svg)](https://www.microsoft.com/windows)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Build Status](https://img.shields.io/badge/Build-Passing-brightgreen.svg)]()

StickyNotes-inator is a lightweight, elegant sticky notes application that brings the convenience of digital sticky notes to your Windows desktop. Built with C# and Windows Forms, it features a modern Material Design interface with beautiful color themes, smooth animations, and seamless system tray integration.

## ✨ Features

### 🎨 **Modern Design**
- **Material Design styling** with rounded corners and shadows
- **Clean, minimalist interface** that doesn't clutter your desktop

### 📝 **Note Management**
- **Create unlimited sticky notes** with a single click
- **Drag and drop** notes anywhere on your screen
- **Auto-save functionality** - never lose your notes
- **Persistent storage** - notes survive system restarts

### 🔧 **System Integration**
- **System tray integration** - always accessible
- **Show/Hide all notes** with tray menu options
- **Minimal resource usage** - lightweight and fast

### 💾 **Data Management**
- **Automatic saving** every 2 seconds after changes
- **JSON-based storage** for easy backup and migration
- **Note visibility states** - hide notes without deleting
- **Bulk operations** - show/hide/delete all notes

## 🖼️ Screenshots

## 🚀 Quick Start

### Prerequisites
- **Windows 10/11** (64-bit)
- **.NET 8.0 Runtime** or later
- **4GB RAM** (minimum)
- **50MB disk space**

### Installation

#### Option 1: Download Release (Recommended)
1. Go to the [Releases](https://github.com/nonaxanon/stickynotes-inator/releases) page
2. Download the latest `StickyNotesInator.zip`
3. Extract to your preferred location
4. Run `StickyNotesInator.exe`

#### Option 2: Build from Source
```bash
# Clone the repository
git clone https://github.com/nonaxanon/stickynotes-inator.git
cd stickynotes-inator

# Restore dependencies
dotnet restore

# Build the application
dotnet build --configuration Release

# Run the application
dotnet run
```

## 📖 Usage Guide

### Getting Started
1. **Launch the application** - it will appear in your system tray
2. **Create your first note** - right-click the tray icon → "Create New Note"
3. **Start typing** - notes auto-save as you type
4. **Move notes around** - drag by the colored title bar

### System Tray Menu
- **📝 Create New Note** - Add a new sticky note
- **👁️ Show All Notes** - Display all hidden notes
- **🙈 Hide All Notes** - Hide all visible notes
- **🗑️ Delete All Notes** - Remove all notes (with confirmation)
- **❌ Exit** - Close the application

### Note Controls
- **✕ Close Button** - Hide the note (saves automatically)
- **📝 Title Bar** - Drag to move the note
- **📄 Text Area** - Click to edit, supports multi-line text
- **🔄 Auto-save** - Changes saved automatically every 2 seconds

### Tips & Tricks
- **Color consistency** - Each note keeps its color theme across sessions
- **Smart positioning** - New notes avoid overlapping existing ones
- **Keyboard shortcuts** - Tab and Enter work normally in text areas
- **Multiple monitors** - Notes work across all connected displays

## 🏗️ Architecture

### Project Structure
```
StickyNotes-inator/
├── 📁 Forms/
│   ├── MainForm.cs              # Application coordinator
│   └── StickyNoteForm.cs        # Individual note interface
├── 📁 Models/
│   ├── Note.cs                  # Note data model
│   └── NoteStorage.cs           # JSON persistence layer
├── 📁 Services/
│   └── TrayService.cs           # System tray management
├── 📁 data/                     # Note storage directory
├── Program.cs                   # Application entry point
├── StickyNotesInator.csproj     # Project configuration
└── README.md                    # This file
```

## 🛠️ Development

### Prerequisites
- **Visual Studio 2022** or **Visual Studio Code**
- **.NET 8.0 SDK**
- **Windows Forms development tools**

### Building
```bash
# Restore dependencies
dotnet restore

# Build in Debug mode
dotnet build

# Build in Release mode
dotnet build --configuration Release

# Run the application
dotnet run
```

### Code Quality
This project follows clean code principles:
- **Clear separation of concerns**
- **Comprehensive error handling**
- **Well-documented code**
- **Maintainable architecture**
- **Native Windows Forms** for optimal performance

### Contributing
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 🔧 Configuration

### Data Storage
Notes are stored in the `data/` directory as JSON files:
```json
{
  "id": "unique-note-id",
  "content": "Your note content here",
  "x": 100,
  "y": 200,
  "width": 300,
  "height": 250,
  "isVisible": true,
  "timestamp": "2024-01-01T12:00:00Z"
}
```

### Customization
- **Colors**: Modify `MaterialColors` array in `StickyNoteForm.cs`
- **Auto-save interval**: Change timer interval in constructor
- **Default note size**: Adjust in `Note.Create()` method


This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- **Material Design** - Design inspiration and principles
- **Windows Forms** - UI framework
- **.NET Community** - Development tools and libraries
- **Open Source Community** - Inspiration and support

---

<div align="center">

**Made with ❤️ for Windows users**

[⭐ Star this repo](https://github.com/nonaxanon/stickynotes-inator) | [🐛 Report a bug](https://github.com/nonaxanon/stickynotes-inator/issues) | [💡 Request a feature](https://github.com/nonaxanon/stickynotes-inator/issues/new)

</div> 
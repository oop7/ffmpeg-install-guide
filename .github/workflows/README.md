# GitHub Actions Workflows

This directory contains CI/CD workflows for the FFmpeg Installer project.

## Workflows

### 1. Build and Release (`build-release.yml`)

**Trigger**: Push a version tag (e.g., `v2.5.2`)

**Purpose**: Automatically builds the installer and creates a draft GitHub release.

**What it does**:
- ✅ Builds the FFmpeg Installer as a single-file executable
- ✅ Generates release notes from commit history
- ✅ Calculates SHA256 checksums for security verification
- ✅ Creates a draft release with the executable and zip archive
- ✅ Uploads build artifacts for 30 days

**How to use**:

```bash
# Create and push a version tag
git tag v2.5.2
git push origin v2.5.2

# Or create an annotated tag with message
git tag -a v2.5.2 -m "Release version 2.5.2 - Added build selection feature"
git push origin v2.5.2
```

After the workflow completes:
1. Go to [Releases](../../releases)
2. Find the draft release
3. Review the release notes and artifacts
4. Edit if needed
5. Click "Publish release" when ready

### 2. Build and Test (`build-test.yml`)

**Trigger**: 
- Push to `main` branch
- Pull requests to `main` branch

**Purpose**: Continuous integration testing to ensure builds work correctly.

**What it does**:
- ✅ Builds the project in Debug and Release configurations
- ✅ Verifies the executable is created successfully
- ✅ Uploads build artifacts for commits to main branch
- ✅ Runs on every PR to catch issues early

## Version Tag Format

Use semantic versioning with a `v` prefix:
- `v2.5.2` - Major.Minor.Patch
- `v3.0.0` - Major version bump
- `v2.5.1` - Patch release

The workflow extracts the version (removes the `v`) for file naming.

## Requirements

These workflows require:
- **Windows runner**: Builds .NET Windows Forms application
- **.NET 6.0 SDK**: Automatically installed by the workflow
- **GitHub token**: Automatically provided (`GITHUB_TOKEN`)

## Artifacts

The workflows produce:
- `FFmpegInstaller.exe` - Standalone executable
- `FFmpegInstaller-{version}.zip` - Compressed archive
- `checksums.txt` - SHA256 hashes for verification
- `release_notes.md` - Generated release notes

## Customization

### Change retention period:
Edit the `retention-days` value in the workflow files.

### Add more build configurations:
Add additional `dotnet publish` commands with different runtime identifiers:
```yaml
- dotnet publish -r win-x86  # 32-bit Windows
- dotnet publish -r win-arm64  # ARM64 Windows
```

### Auto-publish releases:
Change `draft: true` to `draft: false` in `build-release.yml` to auto-publish.

## Troubleshooting

### Build fails with "FFmpegInstaller.exe not found"
- Check that the project file is named `FFmpegInstaller.csproj`
- Verify the output path in the publish command

### Release not created
- Ensure you have `contents: write` permission
- Check that the tag follows the `v*.*.*` format
- Verify the `GITHUB_TOKEN` has sufficient permissions

### Wrong version in release
- Check that your tag follows semantic versioning
- The version is extracted from the tag name (removes `v` prefix)

## Example Workflow Run

```bash
# 1. Make your changes and commit
git add .
git commit -m "Add new feature X"
git push origin main

# 2. Create and push version tag
git tag v2.5.2
git push origin v2.5.2

# 3. Workflow automatically:
#    - Builds the installer
#    - Creates draft release
#    - Uploads FFmpegInstaller.exe
#    - Generates checksums

# 4. Review and publish the draft release on GitHub
```

## Notes

- The installer always downloads the **latest FFmpeg version** from gyan.dev, regardless of when it was built
- Workflows run on GitHub-hosted Windows runners (free for public repositories)
- Build artifacts are stored for 7-30 days depending on the workflow
- Draft releases allow you to test before publishing to users

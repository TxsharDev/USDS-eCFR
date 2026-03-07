# Changelog

All notable changes to the eCFR Regulatory Analyzer are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) with [Semantic Versioning](https://semver.org/).

---

## [1.0.0] — 2026-03-06

**Commit**: `chore: initial project structure and gitignore`

### Added
- ASP.NET Core 9 Minimal API project scaffold (`backend/EcfrAnalyzer.csproj`)
- Solution file (`EcfrAnalyzer.sln`)
- `.gitignore` configured for .NET, Node, SQLite, IDE, and Office temp files
- Minimal `Program.cs` with root health-check endpoint
- `appsettings.json` with fixed port binding (`http://localhost:5000`)
- This changelog

### Technical Notes
- Project targets `net9.0` with nullable reference types and implicit usings enabled
- No external dependencies at this stage, baseline scaffold only

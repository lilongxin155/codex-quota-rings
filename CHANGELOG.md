# Changelog

## 1.1.0

- Release the widget UI and quota-reader process while Codex is not on screen; resume with an immediate refresh when it becomes visible. Keep reading when partially exposed, and conservatively ignore potentially translucent layered occluders.
- Reduce monitor memory by separating its entry point from WPF and using a sleeping loop instead of a UI dispatcher.
- Use Windows process-image queries for window discovery, avoiding repeated managed process enumeration.
- Set software rendering before the widget UI starts. Refresh, window tracking, update avoidance and pause/resume behavior are preserved.
- Wait for the previous widget process to exit before replacing or removing its executable during installation and uninstallation.

## 1.0.0

- Two remaining-usage rings with 10-second refresh and reset-time tooltips.
- Window tracking for moving, maximized and minimized Codex windows.
- Automatic avoidance for overlapping English or Chinese update buttons.
- English and Chinese interface text, a per-user installer and an uninstaller.
- MIT-licensed source and an illustrative screenshot with fictional data.

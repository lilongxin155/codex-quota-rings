Set shell = CreateObject("WScript.Shell")
Set files = CreateObject("Scripting.FileSystemObject")
folder = files.GetParentFolderName(WScript.ScriptFullName)
shell.CurrentDirectory = folder
shell.Run Chr(34) & files.BuildPath(folder, "CodexQuotaWidget.exe") & Chr(34) & " --resume", 0, False

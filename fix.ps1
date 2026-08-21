using System.Text.RegularExpressions;
$text = [System.IO.File]::ReadAllText("Assets/_WORK/Scripts/Dialogue/Editor/ArgumentDrawerRegistry.cs");
$text = [Regex]::Replace($text, '(\{parameter\.DisplayName\}[^\"]*?), \(\) =>', '$1", () =>');
[System.IO.File]::WriteAllText("Assets/_WORK/Scripts/Dialogue/Editor/ArgumentDrawerRegistry.cs", $text);

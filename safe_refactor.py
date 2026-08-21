import os

dirs_to_process = [
    r'Assets\_WORK\Scripts\NPCScripts',
    r'Assets\_WORK\Scripts\PlayerScripts',
    r'Assets\_WORK\Scripts\UI_Scripts',
    r'Assets\_WORK\Scripts\DialogueGameIntegration'
]

def replace_in_file(filepath):
    try:
        with open(filepath, 'r', encoding='utf-8-sig') as f:
            content = f.read()
            encoding = 'utf-8-sig'
    except Exception:
        try:
            with open(filepath, 'r', encoding='utf-8') as f:
                content = f.read()
                encoding = 'utf-8'
        except Exception:
            return

    original = content
    content = content.replace('using UniversalDialogue;', 'using UniversalGraph;')
    content = content.replace('using UniversalDialogue.Editor;', 'using UniversalGraph.Editor;')
    
    if original != content:
        with open(filepath, 'w', encoding=encoding) as f:
            f.write(content)

for d in dirs_to_process:
    if not os.path.exists(d): continue
    for root, _, files in os.walk(d):
        for file in files:
            if file.endswith('.cs'):
                replace_in_file(os.path.join(root, file))

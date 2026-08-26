import os

dirs_to_process = [
    r"Assets\_WORK\Scripts\DIY_Graph\Data",
    r"Assets\_WORK\Scripts\DIY_Graph\Editor",
    r"Assets\_WORK\Scripts\DIY_Graph\1_Dialogue",
    r"Assets\_WORK\Scripts\DIY_Graph\2_Quest",
    r"Assets\_WORK\Scripts\QuestIntegration",
    r"Assets\_WORK\Tests",
    r"Tools\UniversalGraph.Generator"
]

def replace_in_file(filepath):
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
    except UnicodeDecodeError:
        try:
            with open(filepath, 'r', encoding='euc-kr') as f:
                content = f.read()
        except UnicodeDecodeError:
            with open(filepath, 'r', encoding='utf-8-sig') as f:
                content = f.read()

    original = content

    content = content.replace("using UniversalDialogue.Editor;", "using UniversalGraph.Editor;\nusing UniversalGraph.Dialogue.Editor;\nusing UniversalGraph.Quest.Editor;")
    content = content.replace("using UniversalDialogue;", "using UniversalGraph;\nusing UniversalGraph.Dialogue;\nusing UniversalGraph.Quest;")

    if r"DIY_Graph\1_Dialogue" in filepath or r"UniversalGraph.Generator" in filepath:
        content = content.replace("namespace UniversalDialogue.Editor", "namespace UniversalGraph.Dialogue.Editor")
        content = content.replace("namespace UniversalDialogue", "namespace UniversalGraph.Dialogue")
        content = content.replace('"UniversalDialogue.', '"UniversalGraph.Dialogue.')
        content = content.replace('global::UniversalDialogue.', 'global::UniversalGraph.Dialogue.')
    elif r"DIY_Graph\2_Quest" in filepath:
        content = content.replace("namespace UniversalDialogue.Editor", "namespace UniversalGraph.Quest.Editor")
        content = content.replace("namespace UniversalDialogue", "namespace UniversalGraph.Quest")
    elif r"Tests\UniversalDialogue" in filepath:
        content = content.replace("namespace UniversalDialogue.Tests", "namespace UniversalGraph.Tests")
        content = content.replace("namespace UniversalDialogue", "namespace UniversalGraph.Dialogue")
    else:
        content = content.replace("namespace UniversalDialogue.Editor", "namespace UniversalGraph.Editor")
        content = content.replace("namespace UniversalDialogue", "namespace UniversalGraph")

    content = content.replace("UniversalDialogue.IQuestController", "UniversalGraph.Quest.IQuestController")

    if original != content:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)

for d in dirs_to_process:
    for root, _, files in os.walk(d):
        for file in files:
            if file.endswith(".cs"):
                replace_in_file(os.path.join(root, file))

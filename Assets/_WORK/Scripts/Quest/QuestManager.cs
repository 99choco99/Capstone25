
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;




public class QuestManager
{
    public static QuestManager Instance { get; private set; }
    public Dictionary<int, QuestTemplate> QuestTemplates { get; private set; } = new();

    public static void Init(string jsonString)
    {
        if(Instance != null) { return; }
        Instance = new();

        Instance.QuestTemplates.Clear();
        var data = JsonConvert.DeserializeObject<List<QuestTemplate>>(jsonString);

        if (data != null)
        {
            foreach (QuestTemplate template in data)
            {
                Instance.QuestTemplates[template.id] = template;
            }
        }
    }

    //퀘스트 원본 데이터 가져오기
    public QuestTemplate GetQuestTemplate(int questId) => QuestTemplates.GetValueOrDefault(questId);

}

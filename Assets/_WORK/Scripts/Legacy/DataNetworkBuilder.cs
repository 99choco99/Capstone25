using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

public class DataNetworkBuilder : EditorWindow
{
    // [국룰 설정 1] 구글 스프레드시트 고유 문서 ID (본인의 구글 시트 주소창에서 복사하세요!)
    private const string SPREADSHEET_ID = "1gGZoyYR-5Aer21BaxatTupXk0rUIZBzJFUNFBRre9vM";

    // [국룰 설정 2] 각 시트 탭 하단의 고유 GID (구글 시트에서 탭을 클릭했을 때 주소창 맨 뒤 gid=XXXX 번호 입력)
    private const string GID_EQUIPMENT = "969617668";
    private const string GID_CONSUMPTION = "987458510"; // 본인 시트의 gid로 수정
    private const string GID_OTHER = "415677983";       // 본인 시트의 gid로 수정

    private const string GID_DIALOGUE = "1018973760";


    // [국룰 설정 3] 배포 경로 투트랙 뚫기
    private const string serverJsonPath = @"C:\Users\3corps\Desktop\Server\gameData\itemData.json"; // 본인 Node.js 서버 데이터 폴더 경로로 수정!

    [MenuItem("Tools/마스터 데이터/데이터 다운로드 및 배포", false, 1)]
    public static void ShowWindow() => GetWindow<DataNetworkBuilder>("아이템 빌더");

    private void OnGUI()
    {
        GUILayout.Label("구글 시트 ➔ 실시간 다운로드 ➔ JSON 멀티 배포 파이프라인", EditorStyles.boldLabel);
        GUILayout.Space(20);

        if (GUILayout.Button("아이템 데이터 배포", GUILayout.Height(50)))
        {
            DownloadAndBakeItem();
        }
        if (GUILayout.Button("퀘스트 데이터 배포", GUILayout.Height(50)))
        {
            DownloadAndBakeDialogue();
        }
    }

    //CSV 파일 가져오기
    private async Awaitable<string> FetchCSVFromServer(string gid)
    {
        // 구글 스프레드시트의 텍스트 사출 포맷 URL 조립
        string url = $"https://docs.google.com/spreadsheets/d/{SPREADSHEET_ID}/export?format=csv&gid={gid}";
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            await webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                return webRequest.downloadHandler.text;
            }
            else
            {
                Debug.LogError($"[구글 연결 실패] GID: {gid}를 가져오지 못했습니다. 원인: {webRequest.error}");
                return string.Empty;
            }
        }
    }
    

    private async void DownloadAndBakeDialogue()
    {
        List<DialogueLine> dialouges = new List<DialogueLine>();
        Awaitable<string> dialogueTask = FetchCSVFromServer(GID_DIALOGUE);

        string dialogueCSV = await dialogueTask;

        ParseDialogue(dialogueCSV, dialouges);
    }

    public void ParseDialogue(string csvText, List<DialogueLine> list)
    {
        if (string.IsNullOrEmpty(csvText)) { return; }
    }


    private async void DownloadAndBakeItem()
    {
        List<ItemBase> allItems = new List<ItemBase>();

        // 1. 구글 서버로부터 웹 요청을 통해 CSV 데이터를 즉석에서 긁어옵니다.
        Awaitable<string> equipTask = FetchCSVFromServer(GID_EQUIPMENT);
        Awaitable<string> consumeTask = FetchCSVFromServer(GID_CONSUMPTION);
        Awaitable<string> otherTask = FetchCSVFromServer(GID_OTHER);

        string equipCSV = await equipTask;
        string consumeCSV = await consumeTask;
        string otherCSV = await otherTask;


        // 2. 긁어온 CSV 문자열을 파싱하여 다형성 인스턴스로 변환
        ParseEquipment(equipCSV, allItems);
        ParseConsumption(consumeCSV, allItems);
        ParseOther(otherCSV, allItems);

        if (allItems.Count == 0)
        {
            EditorUtility.DisplayDialog("오류", "파싱된 데이터가 없습니다. 구글 시트 ID와 GID를 확인하십시오.", "확인");
            return;
        }

        // 3. 다형성 구조를 유지한 채 완벽한 JSON 문자열로 베이킹
        string jsonResult = JsonConvert.SerializeObject(allItems, Formatting.Indented);

        // 4. 현업 프로그래머의 멀티 포트 포커싱: 클라폴더와 서버폴더에 단 1바이트의 오차도 없이 동시 사출!
        try
        {
            // 서버 폴더 배포
            string serverDir = Path.GetDirectoryName(serverJsonPath);
            if (!Directory.Exists(serverDir)) Directory.CreateDirectory(serverDir);
            File.WriteAllText(serverJsonPath, jsonResult);

            AssetDatabase.Refresh();
            Debug.Log($"<color=green><b>[배포 성공]</b></color> 서버({serverJsonPath}) 동시 적재 완료! 총 {allItems.Count}개 아이템.");
            EditorUtility.DisplayDialog("성공", $"총 {allItems.Count}개의 아이템 데이터가 클라이언트와 서버에 동시 배포되었습니다!", "확인");
        }
        catch (Exception e)
        {
            Debug.LogError($"[배포 실패] 로컬 파일 쓰기 중 참사 발생: {e.Message}");
            Debug.Log($"[디버깅] 배포 시도 경로: {serverJsonPath}");
            Debug.Log($"[디버깅] 폴더 존재 여부: {Directory.Exists(Path.GetDirectoryName(serverJsonPath))}");
        }
    }


    private void ParseEquipment(string csvText, List<ItemBase> list)
    {
        if (string.IsNullOrEmpty(csvText)) return;
        string[] lines = csvText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        for (int i = 1; i < lines.Length; i++)
        {
            // 👑 1차 방어막: 콤마(,)를 다 지웠는데도 빈 줄이면 완벽한 쓰레기 줄이므로 스킵!
            if (string.IsNullOrWhiteSpace(lines[i].Replace(",", ""))) continue;

            string[] cols = lines[i].Split(',');

            // 👑 2차 방어막: 컬럼 개수가 부족하면 배열 에러(IndexOutOfRange)가 나므로 스킵!
            if (cols.Length < 7) continue;

            // 👑 3차 방어막 (핵심): ID가 숫자가 아니거나 빈칸이면 에러 로그만 예쁘게 띄우고 시스템은 살린다!
            if (!int.TryParse(cols[0].Trim(), out int parsedId))
            {
                Debug.LogWarning($"[Equipment 무시됨] {i + 1}번째 줄의 ID '{cols[0]}'가 숫자가 아닙니다.");
                continue;
            }

            EquipmentBaseData equip = new EquipmentBaseData();
            equip.id = parsedId;
            equip.itemName = cols[1].Trim();
            equip.type = SlotType.Equipment;
            equip.description = cols[2].Trim();

            // Enum이나 float도 안전하게 파싱 (실패하면 기본값 0)
            Enum.TryParse(cols[4].Trim(), true, out EquipmentType parsedEquipType);
            equip.equipmentType = parsedEquipType;

            float.TryParse(cols[5].Trim(), out float hp);
            float.TryParse(cols[6].Trim(), out float posture);

            // 공격력 성장은 PlayerStats가 담당하고 숫자형 방어력은 사용하지 않습니다.
            // 기존 시트 열은 호환을 위해 그대로 두고 장비에는 생존 수치만 보관합니다.
            equip.baseStats = new ItemSpec { maxHp = hp, posture = posture };
            list.Add(equip);
        }
    }

    private void ParseConsumption(string csvText, List<ItemBase> list)
    {
        if (string.IsNullOrEmpty(csvText)) return;
        string[] lines = csvText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i].Replace(",", ""))) continue;
            string[] cols = lines[i].Split(',');
            if (cols.Length < 6) continue;

            if (!int.TryParse(cols[0].Trim(), out int parsedId)) continue;

            ConsumptionBaseData cons = new ConsumptionBaseData();
            cons.id = parsedId;
            cons.itemName = cols[1].Trim();
            cons.type = SlotType.Consumption;
            cons.description = cols[2].Trim();

            float.TryParse(cols[3].Trim(), out float heal);
            float.TryParse(cols[4].Trim(), out float dur);
            float.TryParse(cols[5].Trim(), out float cool);

            cons.amount = heal;
            cons.duration = dur;
            cons.coolTime = cool;
            list.Add(cons);
        }
    }

    private void ParseOther(string csvText, List<ItemBase> list)
    {
        if (string.IsNullOrEmpty(csvText)) return;
        string[] lines = csvText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i].Replace(",", ""))) continue;
            string[] cols = lines[i].Split(',');
            if (cols.Length < 3) continue;

            if (!int.TryParse(cols[0].Trim(), out int parsedId)) continue;

            OtherBaseData other = new OtherBaseData();
            other.id = parsedId;
            other.itemName = cols[1].Trim();
            other.type = SlotType.Other;
            other.description = cols[2].Trim();
            list.Add(other);
        }
    }
}

using Newtonsoft.Json;
using System.Net;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class LoginAPI
{
    
    //로그인
    public async Awaitable<LoginResponse> RequestLogin(string id)
    {
        string url = $"{APIConstants.BASE_API_URL}/login";

        string json = JsonConvert.SerializeObject(new { userId = id });

        using (UnityWebRequest webRequest = new UnityWebRequest(url,"POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            await webRequest.SendWebRequest();

            try
            {
                return JsonConvert.DeserializeObject<LoginResponse>(webRequest.downloadHandler.text);
            }
            catch
            {
                return new LoginResponse { success = false };
            }
        }
    }



    //회원가입 요청
    public async Awaitable<bool> RequestRegister(string id, string password)
    {
        string url = $"{APIConstants.BASE_API_URL}/register";

        var registerJson = new {id, password};
        string json = JsonConvert.SerializeObject(registerJson);

        using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            await webRequest.SendWebRequest();

            try
            {
                var resposne = JsonConvert.DeserializeObject<LoginResponse>(webRequest.downloadHandler.text);


                return webRequest.result == UnityWebRequest.Result.Success;
            }
            catch
            {
                return false;
            }

        }
    }
}

public class LoginResponse
{
    public bool success;
    public string message;
}

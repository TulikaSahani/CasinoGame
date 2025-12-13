using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameListManager : MonoBehaviour
{
    [SerializeField] private Transform contentParent;   // ScrollView Content
    [SerializeField] private GameObject gameItemPrefab; // Prefab with Button + TMP_Text for name/code

    private string baseUrl = "https://casino-backend.realtimevillage.com/api";
    void Start()
    {
        StartCoroutine(FetchGameList());
    }

    IEnumerator FetchGameList()
    {
        string token = PlayerPrefs.GetString("AUTH_KEY", "");
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("Auth token not found. Please login first.");
            yield break;
        }

        string url = baseUrl + "/v1/game?token=" + token;
        UnityWebRequest request = UnityWebRequest.Get(url);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error fetching game list: " + request.error);
        }
        else
        {
            string json = request.downloadHandler.text;
            Debug.Log("Game list response: " + json);

            GameListResponse response = JsonUtility.FromJson<GameListResponse>(json);

            if (response != null && response.status && response.data != null)
            {
                foreach (GameData game in response.data)
                {
                    if (game.status == "active")
                    {
                        GameObject go = Instantiate(gameItemPrefab, contentParent);

                        Image gameImage = go.GetComponentInChildren<Image>();
                        if (gameImage != null)
                        {
                            Sprite gameSprite = GetGameSprite(game.game_code);
                            if (gameSprite != null)
                                gameImage.sprite = gameSprite;
                            else
                                Debug.LogWarning($"No sprite found for game code: {game.game_code}");
                        }
                        // Set the label
                        TMP_Text txt = go.GetComponentInChildren<TMP_Text>();
                        if (txt != null)
                            txt.text = game.game_name + " (" + game.game_code + ")";

                        // Add button click
                        Button btn = go.GetComponent<Button>();
                        if (btn != null)
                        {
                            // capture local copies for lambda closure
                            int selectedGameId = game.id;
                            string selectedGameCode = game.game_code;

                            btn.onClick.AddListener(() =>
                            {
                                PlayerPrefs.SetInt("SelectedGameId", selectedGameId);
                                PlayerPrefs.SetString("SelectedGameCode", selectedGameCode);
                                PlayerPrefs.Save();

                                string sceneToLoad = MapGameCodeToScene(selectedGameCode);
                                if (!string.IsNullOrEmpty(sceneToLoad))
                                {
                                    SceneManager.LoadScene(sceneToLoad);
                                }
                                else
                                {
                                    Debug.LogWarning("No scene mapped for game code: " + selectedGameCode);
                                }
                            });
                        }
                        else
                        {
                            Debug.LogWarning("Prefab missing Button component on root. Add a Button component.");
                        }
                    }
                }
            }
        }
    }
    private Sprite GetGameSprite(string gameCode)
    {
        string spritePath = "GameIcons/"; // Folder in Resources

        switch (gameCode)
        {
            case "CG1": return Resources.Load<Sprite>(spritePath + "spin2win_icon");
            case "CG2": return Resources.Load<Sprite>(spritePath + "lucky12_icon");
            case "CG3": return Resources.Load<Sprite>(spritePath + "lucky16_icon");
            default: return Resources.Load<Sprite>(spritePath + "default_icon");
        }
    }
    private string MapGameCodeToScene(string gameCode)
    {
        switch (gameCode)
        {
            case "CG1": return "Spin2Win";
            case "CG2": return "Lucky12";
            case "CG3": return "Lucky12Scene";
            // add more mappings here
            default: return "";
        }
    }
}


[System.Serializable]
public class GameListResponse
{
    public bool status;
    public string message;
    public List<GameData> data;
}

[System.Serializable]
public class GameData
{
    public int id;
    public string game_name;
    public string game_code;
    public string status;
}

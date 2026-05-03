using System;
using System.IO;
using UnityEngine;

public partial class WidgetDataWriter : MonoBehaviour
{
    [Header("Widget data file name (must match the Kotlin provider)")]
    [SerializeField] private string fileName = "widget_data.json";

    private void Start()
    {
        RecordLogin();
    }

    public void RecordLogin()
    {
        var data = new WidgetData
        {
            lastLoginDate = DateTime.Now.ToString("yyyy-MM-dd"),
            streak = LoadCurrentStreak()
        };

        string json = JsonUtility.ToJson(data, prettyPrint: true);
        string path = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllText(path, json);

        Debug.Log($"[WidgetDataWriter] Saved widget data to {path}");
        RequestWidgetUpdate();
    }

    private int LoadCurrentStreak()
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);

        if (!File.Exists(path))
            return 1;

        try
        {
            string existingJson = File.ReadAllText(path);
            var existing = JsonUtility.FromJson<WidgetData>(existingJson);
            DateTime lastLogin = DateTime.Parse(existing.lastLoginDate);
            DateTime today = DateTime.Now.Date;

            if (lastLogin.Date == today)
            {
                return existing.streak;
            }
            else if (lastLogin.Date == today.AddDays(-1))
            {
                return existing.streak + 1;
            }
            else
            {
                return 1;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[WidgetDataWriter] Could not read existing data: {e.Message}");
            return 1;
        }
    }

    private void RequestWidgetUpdate()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var context = activity.Call<AndroidJavaObject>("getApplicationContext"))
            using (var intent = new AndroidJavaObject("android.content.Intent",
                       "com.unity3d.player.UnityPlayer"))
            {
                using (var componentName = new AndroidJavaObject(
                    "android.content.ComponentName",
                    context,
                    new AndroidJavaClass(
                        "com.RayOfGames.WidgetSample.GameWidgetProvider")))
                {
                    intent.Call<AndroidJavaObject>("setComponent", componentName);
                    intent.Call<AndroidJavaObject>("setAction",
                        "android.appwidget.action.APPWIDGET_UPDATE");

                    using (var appWidgetManager = new AndroidJavaClass(
                        "android.appwidget.AppWidgetManager"))
                    using (var manager =
                        appWidgetManager.CallStatic<AndroidJavaObject>("getInstance",
                            context))
                    {
                        int[] ids = manager.Call<int[]>("getAppWidgetIds", componentName);
                        intent.Call<AndroidJavaObject>("putExtra",
                            "appWidgetIds", ids);
                    }

                    context.Call("sendBroadcast", intent);
                    Debug.Log("[WidgetDataWriter] Widget update broadcast sent.");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[WidgetDataWriter] Could not send widget update: {e.Message}");
        }
#endif
    }
}

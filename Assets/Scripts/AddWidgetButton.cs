using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class AddWidgetButton : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(Button_OnClick);
    }

    private void Button_OnClick()
    {
        RequestPinWidget();
    }

    private void RequestPinWidget()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            // requestPinAppWidget requires API 26 (Android 8.0+)
            using (var buildVersion = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                int sdkInt = buildVersion.GetStatic<int>("SDK_INT");
                if (sdkInt < 26)
                {
                    Debug.LogWarning("[AddWidgetButton] Widget pinning requires Android 8.0+");
                    return;
                }
            }

            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var context = activity.Call<AndroidJavaObject>("getApplicationContext"))
            using (var appWidgetManagerClass = new AndroidJavaClass("android.appwidget.AppWidgetManager"))
            using (var manager = appWidgetManagerClass.CallStatic<AndroidJavaObject>("getInstance", context))
            {
                // Check if the launcher supports pinning widgets
                bool canPin = manager.Call<bool>("isRequestPinAppWidgetSupported");
                if (!canPin)
                {
                    Debug.LogWarning("[AddWidgetButton] Launcher does not support pinning widgets.");
                    return;
                }

                using (var componentName = new AndroidJavaObject(
                    "android.content.ComponentName",
                    context,
                    new AndroidJavaClass("com.RayOfGames.WidgetSample.GameWidgetProvider")))
                {
                    // Request the system to pin the widget — shows a confirmation dialog
                    manager.Call<bool>("requestPinAppWidget", componentName, null, null);
                    Debug.Log("[AddWidgetButton] Pin widget request sent.");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[AddWidgetButton] Could not request pin widget: {e.Message}");
        }
#else
        Debug.Log("[AddWidgetButton] Pin widget is only available on Android devices.");
#endif
    }
}

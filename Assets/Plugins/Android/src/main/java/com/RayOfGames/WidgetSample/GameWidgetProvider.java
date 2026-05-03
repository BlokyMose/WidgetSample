package com.RayOfGames.WidgetSample;

import android.app.PendingIntent;
import android.appwidget.AppWidgetManager;
import android.appwidget.AppWidgetProvider;
import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;
import android.widget.RemoteViews;

import org.json.JSONObject;

import java.io.File;
import java.io.FileInputStream;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Locale;

/**
 * Android home-screen widget that shows whether the player has opened the game today.
 *
 * It reads "widget_data.json" from the app's internal files directory
 * (the same path Unity's Application.persistentDataPath resolves to).
 */
public class GameWidgetProvider extends AppWidgetProvider {

    private static final String WIDGET_DATA_FILE = "widget_data.json";

    @Override
    public void onUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds) {
        for (int appWidgetId : appWidgetIds) {
            updateWidget(context, appWidgetManager, appWidgetId);
        }
    }

    @Override
    public void onReceive(Context context, Intent intent) {
        super.onReceive(context, intent);

        // Also handle explicit update requests from Unity
        if (AppWidgetManager.ACTION_APPWIDGET_UPDATE.equals(intent.getAction())) {
            AppWidgetManager manager = AppWidgetManager.getInstance(context);
            int[] ids = manager.getAppWidgetIds(
                    new ComponentName(context, GameWidgetProvider.class));
            onUpdate(context, manager, ids);
        }
    }

    private void updateWidget(Context context, AppWidgetManager appWidgetManager, int appWidgetId) {
        int layoutId = getResId(context, "widget_layout", "layout");
        RemoteViews views = new RemoteViews(context.getPackageName(), layoutId);

        LoginStatus status = readLoginStatus(context);

        if (status.loggedInToday) {
            views.setImageViewResource(
                    getResId(context, "widget_image", "id"),
                    getResId(context, "widget_happy", "drawable"));
            views.setTextViewText(
                    getResId(context, "widget_text", "id"),
                    "Welcome back! 🔥 " + status.streak + " day streak");
        } else {
            views.setImageViewResource(
                    getResId(context, "widget_image", "id"),
                    getResId(context, "widget_sad", "drawable"));
            views.setTextViewText(
                    getResId(context, "widget_text", "id"),
                    "Come play today!");
        }

        // Tapping the widget opens the game
        Intent launchIntent = context.getPackageManager()
                .getLaunchIntentForPackage(context.getPackageName());
        if (launchIntent != null) {
            PendingIntent pendingIntent = PendingIntent.getActivity(
                    context, 0, launchIntent,
                    PendingIntent.FLAG_UPDATE_CURRENT | PendingIntent.FLAG_IMMUTABLE);
            views.setOnClickPendingIntent(getResId(context, "widget_root", "id"), pendingIntent);
        }

        appWidgetManager.updateAppWidget(appWidgetId, views);
    }

    /**
     * Reads widget_data.json from the app's internal files directory.
     * This is the same directory Unity's Application.persistentDataPath points to.
     */
    private LoginStatus readLoginStatus(Context context) {
        try {
            // Unity's Application.persistentDataPath may point to either:
            // - Internal: context.getFilesDir()  (/data/data/<pkg>/files)
            // - External: context.getExternalFilesDir(null)  (/storage/emulated/0/Android/data/<pkg>/files)
            // Check both locations
            File file = new File(context.getFilesDir(), WIDGET_DATA_FILE);
            if (!file.exists()) {
                File extDir = context.getExternalFilesDir(null);
                if (extDir != null) {
                    file = new File(extDir, WIDGET_DATA_FILE);
                }
            }
            if (!file.exists()) {
                return new LoginStatus(false, 0);
            }

            // Read file contents
            FileInputStream fis = new FileInputStream(file);
            byte[] data = new byte[(int) file.length()];
            fis.read(data);
            fis.close();
            String content = new String(data, "UTF-8");

            JSONObject json = new JSONObject(content);
            String lastLoginDate = json.optString("lastLoginDate", "");
            int streak = json.optInt("streak", 0);

            String todayStr = new SimpleDateFormat("yyyy-MM-dd", Locale.US).format(new Date());
            boolean loggedInToday = lastLoginDate.equals(todayStr);

            return new LoginStatus(loggedInToday, streak);
        } catch (Exception e) {
            e.printStackTrace();
            return new LoginStatus(false, 0);
        }
    }

    /** Helper to resolve resource IDs by name (avoids R class dependency). */
    private int getResId(Context context, String name, String type) {
        return context.getResources().getIdentifier(name, type, context.getPackageName());
    }

    private static class LoginStatus {
        final boolean loggedInToday;
        final int streak;

        LoginStatus(boolean loggedInToday, int streak) {
            this.loggedInToday = loggedInToday;
            this.streak = streak;
        }
    }
}

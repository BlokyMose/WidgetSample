using System;

public partial class WidgetDataWriter
{
    [Serializable]
    private class WidgetData
    {
        public string lastLoginDate;
        public int streak;
    }
}

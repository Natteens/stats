namespace Stats
{
    public static class ResourceSaveInterop
    {
        public static RuntimeResourceSaveData Capture(string key, RuntimeResource resource)
        {
            if (resource == null) return null;
            return new RuntimeResourceSaveData
            {
                key = key,
                maxStatId = resource.MaxStat.ToString(),
                current = resource.Current
            };
        }

        public static void Restore(RuntimeResource resource, RuntimeResourceSaveData data)
        {
            if (resource == null || data == null) return;
            resource.SetCurrent(data.current);
        }
    }
}

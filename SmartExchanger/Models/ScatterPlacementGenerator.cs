namespace SmartExchanger.Models
{
    internal static class ScatterPlacementGenerator
    {
        private const int MaximumCount = 2_000;

        public static ScatterPlacement[] Generate(int count, int seed, float minScale, float maxScale,
            float minRotationDegrees, float maxRotationDegrees)
        {
            count = Math.Clamp(count, 0, MaximumCount);
            NormalizeScaleRange(ref minScale, ref maxScale);
            NormalizeRange(ref minRotationDegrees, ref maxRotationDegrees);

            var placements = new ScatterPlacement[count];
            var random = new ScatterRandom(seed);

            for (int idx = 0; idx < placements.Length; idx++)
            {
                float x = random.NextSingle();
                float y = random.NextSingle();
                float scale = random.Range(minScale, maxScale);
                float rotationDegrees = random.Range(minRotationDegrees, maxRotationDegrees);
                placements[idx] = new ScatterPlacement(x, y, scale, rotationDegrees);
            }

            return placements;
        }

        private static void NormalizeRange(ref float min, ref float max)
        {
            if (!float.IsFinite(min))
            {
                min = 0f;
            }
            if (!float.IsFinite(max))
            {
                max = 0f;
            }
            if (min > max)
            {
                (min, max) = (max, min);
            }
        }

        private static void NormalizeScaleRange(ref float min, ref float max)
        {
            if (!float.IsFinite(min))
            {
                min = 0.02f;
            }
            if (!float.IsFinite(max))
            {
                max = 0.08f;
            }
            min = Math.Clamp(min, 0.001f, 4f);
            max = Math.Clamp(max, 0.001f, 4f);
            NormalizeRange(ref min, ref max);
        }
    }
}

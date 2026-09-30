using UnityEngine;

namespace JJKDemo.Combat.Trial
{
    /// <summary>A single grade the player can earn, defined by a destruction-percent threshold.</summary>
    public readonly struct TrialRank
    {
        public TrialRank(string name, float threshold, Color color)
        {
            Name = name;
            Threshold = threshold;
            Color = color;
        }

        /// <summary>Display name, e.g. "Special Grade".</summary>
        public string Name { get; }

        /// <summary>Minimum destruction percent (0-100) required to earn this rank.</summary>
        public float Threshold { get; }

        /// <summary>Theme colour used by the UI.</summary>
        public Color Color { get; }
    }

    /// <summary>
    /// Jujutsu-themed grade ladder for the Cursed Energy Trial. Thresholds are intentionally
    /// generous at the bottom and steep at the top: anyone gets a low grade, but Special Grade
    /// demands near-total destruction (charged Red/Blue, careful Purple, and chain reactions).
    /// </summary>
    public static class TrialRanks
    {
        public const string UnrankedName = "Unranked";
        public static readonly Color UnrankedColor = new Color(0.55f, 0.57f, 0.62f);

        // Lowest -> highest. Thresholds in destruction percent.
        public static readonly TrialRank[] Ladder =
        {
            new TrialRank("Grade 4",         10f, new Color(0.70f, 0.72f, 0.75f)),
            new TrialRank("Grade 3",         25f, new Color(0.60f, 0.78f, 0.85f)),
            new TrialRank("Semi-Grade 2",    40f, new Color(0.40f, 0.78f, 0.95f)),
            new TrialRank("Grade 2",         55f, new Color(0.30f, 0.65f, 1.00f)),
            new TrialRank("Semi-Grade 1",    70f, new Color(0.55f, 0.55f, 1.00f)),
            new TrialRank("Grade 1",         82f, new Color(0.78f, 0.45f, 1.00f)),
            new TrialRank("Special Grade 1", 92f, new Color(0.95f, 0.35f, 0.95f)),
            new TrialRank("Special Grade",   98f, new Color(1.00f, 0.42f, 0.42f)),
        };

        /// <summary>The highest rank whose threshold the percent meets, or null if below Grade 4.</summary>
        public static TrialRank? Evaluate(float percent)
        {
            TrialRank? earned = null;
            for (int i = 0; i < Ladder.Length; i++)
            {
                if (percent + 0.0001f >= Ladder[i].Threshold)
                {
                    earned = Ladder[i];
                }
            }

            return earned;
        }

        /// <summary>Convenience: resolves to a display name + colour, falling back to "Unranked".</summary>
        public static void Resolve(float percent, out string name, out Color color)
        {
            var rank = Evaluate(percent);
            if (rank.HasValue)
            {
                name = rank.Value.Name;
                color = rank.Value.Color;
            }
            else
            {
                name = UnrankedName;
                color = UnrankedColor;
            }
        }
    }
}

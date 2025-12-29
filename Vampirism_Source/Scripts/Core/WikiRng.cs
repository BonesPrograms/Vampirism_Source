using System;
using XRL;
using XRL.Core;
using XRL.Rules;

//copied straight from the wiki

namespace Nexus.Core
{

[HasGameBasedStaticCache]
static class WikiRng
{
    static Random _rand;

    public static Random Rand
    {
        get
        {
            if (_rand is null)
            {
                if (XRLCore.Core?.Game is null)
                {
                    throw new Exception("Vampirism mod attempted to retrieve Random, but Game is not created yet.");
                }
                else if (XRLCore.Core.Game.IntGameState.ContainsKey("Vampirism:Random"))
                {
                    int seed = XRLCore.Core.Game.GetIntGameState("Vampirism:Random");
                    _rand = new Random(seed);
                }
                else
                {
                    _rand = Stat.GetSeededRandomGenerator("Vampirism");
                }
                XRLCore.Core.Game.SetIntGameState("Vampirism:Random", _rand.Next());
            }
            return _rand;
        }
    }

    [GameBasedCacheInit]
    public static void ResetRandom()
    {
        _rand = null;
    }
    public static int Next(int minInclusive, int maxInclusive)
    {
        return Rand.Next(minInclusive, maxInclusive + 1);
    }

}
}

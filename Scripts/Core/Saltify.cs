using XRL.World;
using XRL.World.Parts;
using static XRL.World.Cell;
using System.Linq;
namespace VampirismSys.Core
{
    internal static class Saltify
    {
        static void FindAndSalt(Cell cell)
        {
            if (cell.Objects.Any(x => x.GetPart<LiquidVolume>()?.ContainsLiquid("blood") ?? false))
                cell.AddObject("SaltDrop");
        }

        internal static void Salt(Cell Cell)
        {
            Cell.GetLocalAdjacentCells().Where(x => x.HasObjectWithPart(nameof(LiquidVolume))).ForEach(FindAndSalt);
        }
    }
}
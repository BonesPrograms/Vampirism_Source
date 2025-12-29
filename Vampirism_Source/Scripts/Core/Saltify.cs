using XRL.World;
using XRL.World.Parts;

namespace Nexus.Core
{
    static class Saltify
    {
        static void FindAndSalt(Cell cell)
        {
            foreach (GameObject pool in cell.GetObjectsWithPart(nameof(LiquidVolume)))
                if (pool.GetPart<LiquidVolume>().ContainsLiquid("blood") && !cell.HasObject("SaltDrop"))
                    cell.AddObject("SaltDrop");
        }
        public static void Salt(GameObject Object)
        {
            foreach (Cell cell in Object.CurrentCell.GetLocalAdjacentCells())
                if (cell.HasObjectWithPart(nameof(LiquidVolume)))
                    FindAndSalt(cell);
        }
    }
}
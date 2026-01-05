using XRL.World;
using XRL.World.Parts;
using System.Collections.Generic;

namespace Nexus.Core
{
    static class Saltify
    {
        static void FindAndSalt(Cell cell)
        {
            List<GameObject> pools = cell.GetObjectsWithPart(nameof(LiquidVolume));
            foreach (GameObject pool in pools)
            {
                if (pool.GetPart<LiquidVolume>().ContainsLiquid("blood") && !cell.HasObject("SaltDrop"))
                    cell.AddObject("SaltDrop");
            }
        }
        public static void Salt(GameObject Object)
        {
            List<Cell> cells = Object.CurrentCell.GetLocalAdjacentCells();
            foreach (Cell cell in cells)
            {
                if (cell.HasObjectWithPart(nameof(LiquidVolume)))
                    FindAndSalt(cell);
            }

        }
    }
}
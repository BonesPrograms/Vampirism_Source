using XRL.World;
using XRL.World.Parts;
using System.Collections.Generic;

namespace Nexus.Core
{
    static class Saltify
    {
        static void FindAndSalt(Cell cell)
        {
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                GameObject pool = cell.Objects[i];
                if (pool.GetPart<LiquidVolume>()?.ContainsLiquid("blood") ?? false && !cell.HasObject("SaltDrop"))
                    cell.AddObject("SaltDrop");
            }
        }
        public static void Salt(Cell Cell)
        {
            List<Cell> cells = Cell.GetLocalAdjacentCells();
            for (int i = 0; i < cells.Count; i++)
            {
                Cell cell = cells[i];
                if (cell.HasObjectWithPart(nameof(LiquidVolume)))
                    FindAndSalt(cell);
            }

        }
    }
}
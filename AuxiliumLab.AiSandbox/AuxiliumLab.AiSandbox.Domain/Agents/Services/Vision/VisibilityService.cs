using AuxiliumLab.AiSandbox.Domain.Agents.Entities;
using AuxiliumLab.AiSandbox.Domain.Maps;
using AuxiliumLab.AiSandbox.Domain.Playgrounds;

namespace AuxiliumLab.AiSandbox.Domain.Agents.Services.Vision;

public class VisibilityService : IVisibilityService
{
    public void UpdateVisibleCells(Agent agent, StandardPlayground playground)
    {
        Cell[,] range = playground.CutMapPart(agent.Coordinates, agent.SightRange);

        agent.VisibleCells.Clear();

        // Find the agent's actual position in the extracted grid
        int agentX = -1, agentY = -1;
        for (int x = 0; x < range.GetLength(0); x++)
        {
            for (int y = 0; y < range.GetLength(1); y++)
            {
                if (range[x, y].Coordinates.X == agent.Coordinates.X &&
                    range[x, y].Coordinates.Y == agent.Coordinates.Y)
                {
                    agentX = x;
                    agentY = y;
                    break;
                }
            }
            if (agentX != -1) break;
        }

        // If agent not found in range (shouldn't happen), fall back to center
        if (agentX == -1 || agentY == -1)
        {
            agentX = range.GetLength(0) / 2;
            agentY = range.GetLength(1) / 2;
        }

        // Iterate through all cells in the sight range
        for (int x = 0; x < range.GetLength(0); x++)
        {
            for (int y = 0; y < range.GetLength(1); y++)
            {
                Cell targetCell = range[x, y];

                // Check if cell is within circular sight range
                int dx = targetCell.Coordinates.X - agent.Coordinates.X;
                int dy = targetCell.Coordinates.Y - agent.Coordinates.Y;
                double distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance > agent.SightRange)
                    continue;

                // Check if there's a clear line of sight to this cell
                if (HasLineOfSight(range, agentX, agentY, x, y))
                {
                    // Get the actual cell from the map to persist the sight changes
                    Cell originalCell = playground.GetCell(targetCell.Coordinates);
                    agent.VisibleCells.Add(originalCell);
                }
            }
        }
    }

    private bool HasLineOfSight(Cell[,] grid, int startX, int startY, int endX, int endY)
    {
        // If checking the same cell, always visible
        if (startX == endX && startY == endY)
            return true;

        int dx = Math.Abs(endX - startX);
        int dy = Math.Abs(endY - startY);
        int sx = startX < endX ? 1 : -1;
        int sy = startY < endY ? 1 : -1;
        int err = dx - dy;

        int currentX = startX;
        int currentY = startY;

        while (true)
        {
            // Bresenham's line algorithm - calculate next step
            int e2 = 2 * err;

            bool stepX = false;
            bool stepY = false;

            if (e2 > -dy)
            {
                stepX = true;
            }
            if (e2 < dx)
            {
                stepY = true;
            }

            // Move to next cell
            if (stepX)
            {
                err -= dy;
                currentX += sx;
            }
            if (stepY)
            {
                err += dx;
                currentY += sy;
            }

            // Check bounds
            if (currentX < 0 || currentX >= grid.GetLength(0) ||
                currentY < 0 || currentY >= grid.GetLength(1))
            {
                return false;
            }

            // If we've reached the target cell, it's visible (even if it's blocking)
            if (currentX == endX && currentY == endY)
                return true;

            // Check if current cell blocks further vision
            Cell currentCell = grid[currentX, currentY];
            if (currentCell != null && !currentCell.Object.Transparent)
            {
                // This cell blocks vision to anything beyond it
                return false;
            }
        }
    }
}
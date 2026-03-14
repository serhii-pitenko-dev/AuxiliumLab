namespace AuxiliumLab.AiSandbox.Domain.Validation;

internal static class MapValidator
{
    internal static void ValidateSize(int width, int height)
    {
        // Validate Width
        if (width < 3 || width > 500)
        {
            throw new ArgumentException("Width must be between 3 and 500.", nameof(width));
        }

        // Validate Height
        if (height < 3 || height > 500)
        {
            throw new ArgumentException("Height must be between 3 and 500.", nameof(height));
        }
    }

    internal static void ValidateElementsProportion(int percentOfBlocks, int percentOfEnemies)
    {
        // Validate PercentOfBlocks
        if (percentOfBlocks < 0 || percentOfBlocks > 80)
        {
            throw new ArgumentException("Percentage of blocks must be between 5% and 80%.", nameof(percentOfBlocks));
        }

        // Validate PercentOfEnemies
        if (percentOfEnemies < 0 || percentOfEnemies > 30)
        {
            throw new ArgumentException("Percentage of enemies must be between 0% and 30%.", nameof(percentOfEnemies));
        }

        // Validate combined percentages
        if (percentOfBlocks + percentOfEnemies > 80)
        {
            throw new ArgumentException("Combined percentage of blocks and enemies cannot exceed 80%.");
        }
    }
}
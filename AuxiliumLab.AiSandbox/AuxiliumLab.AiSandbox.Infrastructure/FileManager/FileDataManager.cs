using AuxiliumLab.AiSandbox.Domain;
using AuxiliumLab.AiSandbox.Domain.Maps;
using AuxiliumLab.AiSandbox.Infrastructure.Configuration;
using AuxiliumLab.AiSandbox.Infrastructure.Converters;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AuxiliumLab.AiSandbox.Infrastructure.FileManager;

public class FileDataManager<T>: IFileDataManager<T>
{
    private readonly string _baseStorageDirectory;
    private readonly JsonSerializerOptions _jsonOptions;
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new();
    
    public FileDataManager(IOptions<FileSourceConfiguration> fileSourceOptions)
    {
        var fileStorage = fileSourceOptions.Value.FileStorage;

        // Use BasePath from appsettings.json, or fallback to default if not configured
        string basePath = !string.IsNullOrWhiteSpace(fileStorage.BasePath)
            ? fileStorage.BasePath
            : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FileStorage");
        
        // Create base folder with the name of generic type T
        _baseStorageDirectory = Path.Combine(basePath, typeof(T).Name);
        
        if (!Directory.Exists(_baseStorageDirectory))
        {
            Directory.CreateDirectory(_baseStorageDirectory);
        }

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            IncludeFields = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            Converters = 
            { 
                new TwoDimensionalArrayConverter<SandboxMapBaseObject>(),
                new TwoDimensionalArrayConverter<Cell>()
            }
        };
    }

    public async Task AppendObjectAsync(Guid id, object obj)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        string filePath = GetFilePath(id);

        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var wrapper = new
        {
            Type = obj.GetType().Name,
            Timestamp = DateTime.UtcNow,
            Data = obj
        };

        string jsonContent = JsonSerializer.Serialize(wrapper, _jsonOptions) + Environment.NewLine;
        var fileLock = _fileLocks.GetOrAdd(filePath, _ => new SemaphoreSlim(1, 1));
        await fileLock.WaitAsync();
        try
        {
            await File.AppendAllTextAsync(filePath, jsonContent);
        }
        finally
        {
            fileLock.Release();
        }
    }

    public async Task SaveOrAppendAsync(Guid id, T obj)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        string filePath = GetFilePath(id);
        
        // Ensure directory exists for this specific object ID
        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        string jsonContent = JsonSerializer.Serialize(obj, _jsonOptions);
        var fileLock = _fileLocks.GetOrAdd(filePath, _ => new SemaphoreSlim(1, 1));
        await fileLock.WaitAsync();
        try
        {
            await File.AppendAllTextAsync(filePath, jsonContent);
        }
        finally
        {
            fileLock.Release();
        }
    }

    public async Task<T> LoadObjectAsync(Guid id)
    {
        string filePath = GetFilePath(id);

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Object with ID {id} not found.");

        string jsonContent = await File.ReadAllTextAsync(filePath);
        T? obj = JsonSerializer.Deserialize<T>(jsonContent, _jsonOptions);

        if (obj == null)
            throw new InvalidOperationException($"Failed to deserialize object with ID {id}.");

        return obj;
    }

    public bool DeleteObject(Guid id)
    {
        string filePath = GetFilePath(id);

        if (!File.Exists(filePath))
            return false;

        try
        {
            File.Delete(filePath);
            
            // Optionally clean up empty directory
            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
            {
                if (!Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory);
                }
            }
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    public IEnumerable<Guid> GetAvailableVersions()
    {
        if (!Directory.Exists(_baseStorageDirectory))
            return Enumerable.Empty<Guid>();

        // Search recursively through subdirectories
        return Directory.GetFiles(_baseStorageDirectory, "*.json", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(filename => Guid.TryParse(filename, out _))
            .Select(filename => Guid.Parse(filename!))
            .ToList();
    }

    public void Clear()
    {
        if (!Directory.Exists(_baseStorageDirectory))
            return;

        foreach (string file in Directory.GetFiles(_baseStorageDirectory, "*.json", SearchOption.AllDirectories))
        {
            try
            {
                File.Delete(file);
            }
            catch
            {
                // Continue clearing other files even if one fails
            }
        }
        
        // Clean up empty subdirectories
        foreach (string directory in Directory.GetDirectories(_baseStorageDirectory))
        {
            try
            {
                if (!Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory);
                }
            }
            catch
            {
                // Continue clearing other directories even if one fails
            }
        }
    }

    private string GetFilePath(Guid id)
    {
        // Create path: {BaseDirectory}/{TypeName}/{ObjectId}/{ObjectId}.json
        return Path.Combine(_baseStorageDirectory, id.ToString(), $"{id}.json");
    }
}

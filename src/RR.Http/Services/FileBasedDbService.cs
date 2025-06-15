using System.Text.Json;
using RR.Http.Models;

namespace RR.Http.Services;

/// <summary>
/// File-based database service for storing users and file metadata
/// </summary>
public class FileBasedDbService {
    private readonly string _dataPath;
    private readonly string _usersFilePath;
    private readonly string _filesFilePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly object _lockObject = new();

    public FileBasedDbService(IConfiguration configuration) {
        var storagePath = configuration["FileStorage:DatabasePath"] ?? "data";
        _dataPath = Path.GetFullPath(storagePath);
        _usersFilePath = Path.Combine(_dataPath, "users.json");
        _filesFilePath = Path.Combine(_dataPath, "files.json");

        _jsonOptions = new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        EnsureDataDirectoryExists();
        InitializeDataFiles();
    }

    private void EnsureDataDirectoryExists() {
        if (!Directory.Exists(_dataPath)) {
            Directory.CreateDirectory(_dataPath);
        }
    }

    private void InitializeDataFiles() {
        if (!File.Exists(_usersFilePath)) {
            SaveUsers(new List<User>());
        }

        if (!File.Exists(_filesFilePath)) {
            SaveFiles(new List<FileEntity>());
        }
    }

    // User operations
    public List<User> GetUsers() {
        lock (_lockObject) {
            if (!File.Exists(_usersFilePath))
                return new List<User>();

            var json = File.ReadAllText(_usersFilePath);
            return JsonSerializer.Deserialize<List<User>>(json, _jsonOptions) ?? new List<User>();
        }
    }

    public User? GetUserById(string id) {
        var users = GetUsers();
        return users.FirstOrDefault(u => u.Id == id);
    }

    public User? GetUserByUsername(string username) {
        var users = GetUsers();
        return users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
    }

    public void SaveUser(User user) {
        lock (_lockObject) {
            var users = GetUsers();
            var existingIndex = users.FindIndex(u => u.Id == user.Id);

            if (existingIndex >= 0) {
                users[existingIndex] = user;
            } else {
                users.Add(user);
            }

            SaveUsers(users);
        }
    }

    public void DeleteUser(string id) {
        lock (_lockObject) {
            var users = GetUsers();
            users.RemoveAll(u => u.Id == id);
            SaveUsers(users);
        }
    }

    private void SaveUsers(List<User> users) {
        var json = JsonSerializer.Serialize(users, _jsonOptions);
        File.WriteAllText(_usersFilePath, json);
    }

    // File operations
    public List<FileEntity> GetFiles() {
        lock (_lockObject) {
            if (!File.Exists(_filesFilePath))
                return new List<FileEntity>();

            var json = File.ReadAllText(_filesFilePath);
            return JsonSerializer.Deserialize<List<FileEntity>>(json, _jsonOptions) ?? new List<FileEntity>();
        }
    }

    public FileEntity? GetFileById(string id) {
        var files = GetFiles();
        return files.FirstOrDefault(f => f.Id == id);
    }

    public List<FileEntity> GetFilesByOwner(string ownerId) {
        var files = GetFiles();
        return files.Where(f => f.OwnerId == ownerId).ToList();
    }

    public List<FileEntity> GetFilesSharedWithUser(string userId) {
        var files = GetFiles();
        return files.Where(f => f.SharedWith.Contains(userId)).ToList();
    }

    public void SaveFile(FileEntity file) {
        lock (_lockObject) {
            var files = GetFiles();
            var existingIndex = files.FindIndex(f => f.Id == file.Id);

            if (existingIndex >= 0) {
                files[existingIndex] = file;
            } else {
                files.Add(file);
            }

            SaveFiles(files);
        }
    }

    public void DeleteFile(string id) {
        lock (_lockObject) {
            var files = GetFiles();
            files.RemoveAll(f => f.Id == id);
            SaveFiles(files);
        }
    }

    private void SaveFiles(List<FileEntity> files) {
        var json = JsonSerializer.Serialize(files, _jsonOptions);
        File.WriteAllText(_filesFilePath, json);
    }
}

public interface ISaveStorage
{
    bool Exists();
    SaveData Load();
    void Save(SaveData saveData);
    void Delete();
}

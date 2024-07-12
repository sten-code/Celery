namespace Celery.Settings;

public abstract class Setting : Core.ViewModel
{
    public string Name { get; }
    public string Description { get; }
    public string Id { get; }

    public Setting(string name, string id, string description)
    {
        Name = name;
        Id = id;
        Description = description;
    }
    
    public abstract object GetValue();
    public abstract void SetValue(object value);
}
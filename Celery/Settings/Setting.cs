namespace Celery.Settings;

public abstract class Setting : Core.ViewModel
{
    public string Name { get; set; }
    public string Id { get; set; }

    public Setting(string name, string id)
    {
        Name = name;
        Id = id;
    }
    
    public abstract object GetValue();
    public abstract void SetValue(object value);
}
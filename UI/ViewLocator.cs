using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using UI.ViewModels;

namespace UI;

public class ViewLocator: IDataTemplate
{
    public Control? Build(object? data)
    {
        if (data == null)
            return null;
        
        var name = data.GetType().FullName!.Replace("ViewModel", "View", StringComparison.InvariantCulture);
        var type = Type.GetType(name);
        
        if (type == null) 
            return null;
        
        var control = Activator.CreateInstance(type) as Control;
        control?.DataContext = data;
        return control;
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using CommunityToolkit.Mvvm.ComponentModel;
using SMTx.ViewModels;
using Dock.Model.Core;

namespace SMTx
{
    public class ViewLocator : IDataTemplate
    {
        public Control Build(object? data)
        {
            var name = data?.GetType().FullName?.Replace("ViewModel", "View");
            if (name is null)
            {
                return new TextBlock { Text = "Invalid Data Type" };
            }
            var type = Type.GetType(name);
            if (type is { })
            {
                var instance = Activator.CreateInstance(type);
                if (instance is { })
                {
                    return (Control)instance;
                }
                else
                {
                    return new TextBlock { Text = "Create Instance Failed: " + type.FullName };
                }
            }
            else
            {
                return new TextBlock { Text = "Not Found: " + name };
            }
        }

        public bool Match(object? data)
        {
            return data is ObservableObject || data is IDockable;
        }
    }
}
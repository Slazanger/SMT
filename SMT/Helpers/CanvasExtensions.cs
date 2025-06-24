using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace SMT.Helpers;

public static class CanvasExtensions
{
    public static void EnsureChild(this Canvas canvas, UIElement element)
    {
        if (element == null) return;
        if (!canvas.Children.Contains(element))
        {
            canvas.Children.Add(element);
        }
    }
    
    public static void ClearChildren(this Canvas canvas, IList<UIElement> elements)
    {
        if (elements == null) return;
        foreach (UIElement childElement in elements.ToList())
        {
            if (canvas.Children.Contains(childElement))
            {
                canvas.Children.Remove(childElement);
            }
        }
        elements.Clear();
    }

    public static void ReleaseChildren<T>(this Canvas canvas, IList<T> elements, ElementPool<T> pool) where T : Shape, new()
    {
        if (elements == null) return;
        foreach (T element in elements.ToList())
        {
            pool.Release(canvas, element);
        }
        elements.Clear();
    }

}
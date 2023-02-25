using Avalonia;

namespace SMTx.Themes;

public interface IThemeManager
{
    void Initialize(Application application);

    void Switch(int index);
}

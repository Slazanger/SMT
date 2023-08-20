using System.Collections.ObjectModel;
using Dock.Model.Mvvm.Controls;
using SMT.EVEData;

namespace SMTx.ViewModels.Tools
{
    public class CharactersViewModel : Tool
    {
        public ObservableCollection<string> LocalCharacters { get; init; }

        public CharactersViewModel()
        {
            LocalCharacters = new ObservableCollection<string>();

            foreach(LocalCharacter lc in EveManager.Instance.LocalCharacters)
            {
                LocalCharacters.Add(lc.Name);
            }
        }
    }
}

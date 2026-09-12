using System.ComponentModel;

namespace EpicRPGBot.UI.Pets
{
    public sealed class PetRow : INotifyPropertyChanged
    {
        private bool _selected;
        private bool _locked;
        private string _protection;
        public PetRecord Pet { get; set; }
        public string Id => Pet.Id;
        public string Species => Pet.Species;
        public int Tier => Pet.Tier;
        public int Score => Pet.Score;
        public string Status => Pet.Status;
        public string Skills => Pet.Skills.Length == 0 ? "Normie" : Pet.Skills;
        public bool Selected { get => _selected; set { _selected = value; Changed(nameof(Selected)); } }
        public bool Locked { get => _locked; set { _locked = value; Changed(nameof(Locked)); } }
        public string Protection { get => _protection; set { _protection = value; Changed(nameof(Protection)); } }
        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
    }
}

namespace Santase.UI.Game
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    using Santase.Logic.Cards;

    public sealed class CardSlot : INotifyPropertyChanged
    {
        private bool isPlayable;

        private bool isPlayed;

        private bool isFaceDown;

        public CardSlot(Card card, bool isFaceDown = false)
        {
            this.Card = card;
            this.isFaceDown = isFaceDown;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public Card Card { get; }

        public string FaceImage => CardImageProvider.For(this.Card);

        public string ImageSource => this.IsFaceDown ? CardImageProvider.BackImage : this.FaceImage;

        public bool IsFaceDown
        {
            get => this.isFaceDown;
            set => this.SetField(ref this.isFaceDown, value, nameof(this.IsFaceDown), nameof(this.ImageSource));
        }

        public bool IsPlayable
        {
            get => this.isPlayable;
            set => this.SetField(ref this.isPlayable, value, nameof(this.IsPlayable), nameof(this.IsDimmed));
        }

        public bool IsPlayed
        {
            get => this.isPlayed;
            set => this.SetField(ref this.isPlayed, value);
        }

        public bool IsDimmed => !this.IsPlayable;

        private void SetField<T>(ref T field, T value, params string[] propertyNames)
        {
            if (Equals(field, value))
            {
                return;
            }

            field = value;
            foreach (var name in propertyNames)
            {
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}

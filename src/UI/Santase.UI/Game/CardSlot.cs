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

        private bool isHinted;

        private string announceText = string.Empty;

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

        /// <summary>Gold outline while this card is being suggested by the hint advisor.</summary>
        public bool IsHinted
        {
            get => this.isHinted;
            set => this.SetField(ref this.isHinted, value, nameof(this.IsHinted));
        }

        /// <summary>"20" / "40" when leading this card would announce a marriage; empty otherwise.</summary>
        public string AnnounceText
        {
            get => this.announceText;
            set => this.SetField(ref this.announceText, value, nameof(this.AnnounceText), nameof(this.HasAnnounce));
        }

        public bool HasAnnounce => this.announceText.Length > 0;

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

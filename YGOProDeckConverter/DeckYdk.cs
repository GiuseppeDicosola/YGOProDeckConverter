using System.Collections.Generic;

namespace YGOProDeckConverter
{
    public class Card
    {
        public int Id { get; set; }
        public int Level { get; set; }

        public Card(int id, int level)
        {
            Id = id;
            Level = level;
        }
    }
    public class DeckYdk
    {
        public List<Card> Main { get; set; }
        public List<Card> Extra { get; set; }
        public List<Card> Side { get; set; }

        public DeckYdk()
        {
            Main = new List<Card>();
            Extra = new List<Card>();
            Side = new List<Card>();
        }
    }
}

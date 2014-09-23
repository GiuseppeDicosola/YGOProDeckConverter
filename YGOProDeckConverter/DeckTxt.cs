using System.Collections.Generic;

namespace YGOProDeckConverter
{
    public class Card1
    {
        public int numCard { get; set; }
        public int nameCard { get; set; }

        public Card1(int n, int name)
        {
            numCard = n;
            nameCard = name;
        }
    }
    public class DeckTxt
    {
        public List<Card1> main { get; set; }
        public List<Card1> extra { get; set; }
        public List<Card1> side { get; set; }

        public DeckTxt()
        {
            main = new List<Card1>();
            extra = new List<Card1>();
            side = new List<Card1>();
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace YGOProDeckConverter
{
    public partial class Form1 : Form
    {
        public string DeckName;        
        private const string Dir = "deck";
        private string Outputdir = Path.Combine(Application.StartupPath,"decklist");

        private readonly OpenFileDialog _open = new OpenFileDialog
        {
            Filter = @"YGO Deck ( .ydk )|*.ydk",
            InitialDirectory = Application.StartupPath + @"\" + Dir,
        };
            
        private const string ConnString = "Data Source={0}; ReadOnly = true; Version=3";
        private SQLiteConnection _conn;

        public string GetCardName(int cardNumber)
        {
            var connString = String.Format(ConnString, Path.Combine(Directory.GetParent(Dir).FullName, "cards.cdb"));
            _conn = new SQLiteConnection(connString);
            _conn.Open();
            if (_conn.State != ConnectionState.Open)
            {
                MessageBox.Show(@"Error opening database");
            }

            var cmd = new SQLiteCommand(_conn)
            {
                CommandText = String.Format("SELECT name FROM texts WHERE id == {0}", cardNumber)
            };

            var reader = cmd.ExecuteReader();
            reader.Read();

            return reader.GetString(0);
        }

        public DeckYdk BuildDeckTxt(string path)
        {
            DeckYdk deck = new DeckYdk();
            path = _open.FileName; 
            using (StreamReader reader = new StreamReader(path))
            {

                string text = reader.ReadToEnd();
                string[] s = text.Split(new[] {"#main", "#extra", "!side"}, StringSplitOptions.None);

                string
                    s1 = s[1],
                    s2 = s[2],
                    s3 = s[3];

                string[]
                    s11 = s1.Split(new[] {"\r\n"}, StringSplitOptions.None),
                    s22 = s2.Split(new[] {"\r\n"}, StringSplitOptions.None),
                    s33 = s3.Split(new[] {"\r\n"}, StringSplitOptions.None);

                foreach (int cardNumber in s11.Where(cardNumber => !String.IsNullOrEmpty(cardNumber)).Select(cardNumber => Convert.ToInt32(cardNumber)))
                {
                    deck.Main.Add(new Card(cardNumber));
                }

                foreach (int cardNumber in s22.Where(cardNumber => !String.IsNullOrEmpty(cardNumber)).Select(cardNumber => Convert.ToInt32(cardNumber)))
                {
                    deck.Extra.Add(new Card(cardNumber));
                }

                foreach (int cardNumber in s33.Where(cardNumber => !String.IsNullOrEmpty(cardNumber)).Select(cardNumber => Convert.ToInt32(cardNumber)))
                {
                    deck.Side.Add(new Card(cardNumber));
                }
            }

            return deck;
        }
      
        private class CardComparer : IEqualityComparer<Card>
        {
            public bool Equals(Card x, Card y)
            {
                return x.Id == y.Id;
            }

            public int GetHashCode(Card obj)
            {
                return obj.Id;
            }
        }

        public void YdkToTxt(DeckYdk deck)
        {
            Dictionary<Card, int>
                mainDeck = new Dictionary<Card, int>(new CardComparer()),
                extraDeck = new Dictionary<Card, int>(new CardComparer()),
                sideDeck = new Dictionary<Card, int>(new CardComparer());

            StringBuilder builder = new StringBuilder();

            List<Card>
                mainList = deck.Main,
                extraList = deck.Extra,
                sideList = deck.Side;

            //main deck
            foreach (Card card in mainList)
            {
                if (mainDeck.ContainsKey(card))
                {
                    mainDeck[card]++;
                }
                else
                {
                    mainDeck.Add(card, 1);
                }
            }

            //extra deck
            foreach (Card card in extraList)
            {
                if (extraDeck.ContainsKey(card))
                {
                    extraDeck[card]++;
                }
                else
                {
                    extraDeck.Add(card, 1);
                }
            }

            //side deck
            foreach (Card card in sideList)
            {
                if (sideDeck.ContainsKey(card))
                {
                    sideDeck[card]++;
                }
                else
                {
                    sideDeck.Add(card, 1);
                }
            }

            builder.AppendLine("#main");
            foreach (KeyValuePair<Card, int> card in mainDeck)
            {
                builder.AppendLine(String.Format("{0} {1}", card.Value, GetCardName(card.Key.Id)));
            }

            builder.AppendLine();
            builder.AppendLine("#extra");
            foreach (KeyValuePair<Card, int> card in extraDeck)
            {
                builder.AppendLine(String.Format("{0} {1}", card.Value, GetCardName(card.Key.Id)));
            }

            builder.AppendLine();
            builder.AppendLine("!side");
            foreach (KeyValuePair<Card, int> card in sideDeck)
            {
                builder.AppendLine(String.Format("{0} {1}", card.Value, GetCardName(card.Key.Id)));
            }

            if (!Directory.Exists(Outputdir)) Directory.CreateDirectory(Outputdir);
            File.WriteAllText(Path.Combine(Outputdir, DeckName), builder.ToString());
        }

        public void DeckConversion(string path)
        {
            DeckName = Path.GetFileName(Path.ChangeExtension(Path.Combine(Outputdir, _open.FileName), "txt"));
            DeckYdk deck = BuildDeckTxt(path);
            YdkToTxt(deck);
            MessageBox.Show(String.Format("{0} Converted", DeckName));
            textBox1.Text = @"Select file to convert";
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {            
            DialogResult dr = _open.ShowDialog();
            if (dr == DialogResult.OK)
                textBox1.Text = _open.SafeFileName;           
        }

        private void btnConvert_Click(object sender, EventArgs e)
        {
            DeckConversion(Outputdir);
        }

        private void Form1_HelpButtonClicked(object sender, CancelEventArgs e)
        {
            MessageBox.Show(@"Created by Giuseppe D.", @"About");
            e.Cancel = true;
        }

        public Form1()
        {
            InitializeComponent();           
        }       
    }
}

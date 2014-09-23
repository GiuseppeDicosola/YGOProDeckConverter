using System;
using System.Collections.Generic;
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
        private const string Path = "deck";

        private OpenFileDialog open = new OpenFileDialog
        {
            Filter = "YGO Deck ( .ydk )|*.ydk|Text ( .txt )|*.txt",
            InitialDirectory = Application.StartupPath + Path,
        };

        private string _currentFile;
        private readonly string[] msg =
        {
            "Select file to convert",
            "Converted file"
        };

        private readonly string[] msgErrors =
        {
            "The card {0} doesn't exist",
            "File does not exist or wrong extension"
        };
        private const string ConnString = "Data Source={0}; ReadOnly = true; Version=3";
        private SQLiteConnection _conn;

        private void Connection()
        {
            var connString = String.Format(ConnString,
                System.IO.Path.Combine(Directory.GetParent(Path).FullName, "cards.cdb"));
            _conn = new SQLiteConnection(connString);
            _conn.Open();
        }
        public string GetCardName(int cardNumber)
        {
            Connection();

            var cmd = new SQLiteCommand(_conn)
            {
                CommandText = String.Format("SELECT name FROM texts WHERE id == {0}", cardNumber)
            };

            var reader = cmd.ExecuteReader();
            reader.Read();

            return reader.GetString(0);
        }
        public int GetCardNumber(string name)
        {
            Connection();
            name = name.Replace("'", "_");

            var cmd = new SQLiteCommand(_conn)
            {
                CommandText = string.Format("SELECT id FROM texts WHERE name LIKE '{0}'", name)
            };

            var reader = cmd.ExecuteReader();
            reader.Read();

            if (reader.IsDBNull(0))
            {
                MessageBox.Show(msgErrors[0], name);
                return 0;
            }

            return reader.GetInt32(0);
        }
        public int GetCardLevel(int cardNumber)
        {
            Connection();

            var cmd = new SQLiteCommand(_conn)
            {
                CommandText = String.Format("SELECT level FROM datas WHERE id == {0}", cardNumber)
            };

            var reader = cmd.ExecuteReader();
            reader.Read();

            return reader.GetInt32(0);
        }
        public int GetCardType(int cardNumber)
        {
            Connection();

            var cmd = new SQLiteCommand(_conn)
            {
                CommandText = String.Format("SELECT type FROM datas WHERE id == {0}", cardNumber)
            };

            var reader = cmd.ExecuteReader();
            reader.Read();

            return reader.GetInt32(0);
        }



        public DeckYdk BuildDeckTxt(string path)
        {
            DeckYdk deck = new DeckYdk();
            path = open.FileName; 
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

                foreach (
                    int cardNumber in
                        s11.Where(cardNumber => !String.IsNullOrEmpty(cardNumber))
                            .Select(cardNumber => Convert.ToInt32(cardNumber)))
                {
                    deck.Main.Add(new Card(cardNumber, GetCardLevel(cardNumber)));
                }

                foreach (
                    int cardNumber in
                        s22.Where(cardNumber => !String.IsNullOrEmpty(cardNumber))
                            .Select(cardNumber => Convert.ToInt32(cardNumber)))
                {
                    deck.Extra.Add(new Card(cardNumber, GetCardLevel(cardNumber)));
                }

                foreach (
                    int cardNumber in
                        s33.Where(cardNumber => !String.IsNullOrEmpty(cardNumber))
                            .Select(cardNumber => Convert.ToInt32(cardNumber)))
                {
                    deck.Side.Add(new Card(cardNumber, GetCardLevel(cardNumber)));
                }
            }

            return deck;
        }
        public DeckTxt BuildDeckYdk(string path)
        {
            DeckTxt deck = new DeckTxt();
            path = open.FileName;
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

                foreach (string t in s11)
                {
                    if (t == String.Empty) continue;
                    string n1 = Convert.ToString(t[0]);
                    decimal x1 = Convert.ToDecimal(n1);
                    string title1 = t.Remove(0, 2);

                    for (int j = 0; j < x1; j++)
                    {
                        deck.main.Add(new Card1((int) x1, GetCardNumber(title1)));
                    }
                }

                foreach (string t in s22)
                {
                    if (t == String.Empty) continue;
                    string n2 = Convert.ToString(t[0]);
                    decimal x2 = Convert.ToDecimal(n2);
                    string title2 = t.Remove(0, 2);

                    for (int j = 0; j < x2; j++)
                    {
                        deck.extra.Add(new Card1((int) x2, GetCardNumber(title2)));
                    }
                }

                foreach (string t in s33)
                {
                    if (t == String.Empty) continue;
                    string n3 = Convert.ToString(t[0]);
                    decimal x3 = Convert.ToDecimal(n3);
                    string title3 = t.Remove(0, 2);

                    for (int j = 0; j < x3; j++)
                    {
                        deck.side.Add(new Card1((int) x3, GetCardNumber(title3)));
                    }
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
                return obj.Id ^ obj.Level;
            }
        }

        public void YdkToTxt(DeckYdk deck)
        {
            Dictionary<Card, int>
                mainDeck = new Dictionary<Card, int>(new CardComparer()),
                extraDeck = new Dictionary<Card, int>(new CardComparer()),
                sideDeck = new Dictionary<Card, int>(new CardComparer());

            StringBuilder builder = new StringBuilder();

            IOrderedEnumerable<Card>
                mainList = (deck.Main.OrderByDescending(card => card.Level)),
                extraList = (deck.Extra.OrderByDescending(card => card.Level)),
                sideList = (deck.Side.OrderByDescending(card => card.Level));

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

            File.WriteAllText(System.IO.Path.Combine(Path, DeckName), builder.ToString());
        }
        public void TxtToYdk(DeckTxt deck1)
        {
            List<Card1>
                mainList = deck1.main,
                extraList = deck1.extra,
                sideList = deck1.side;

            StringBuilder builder = new StringBuilder();

            builder.AppendLine("#created by ...");
            builder.AppendLine("#main");
            foreach (Card1 card in mainList)
            {
                builder.AppendLine(string.Format("{0}", card.nameCard));
            }

            builder.AppendLine("#extra");
            foreach (Card1 card in extraList)
            {
                builder.AppendLine(string.Format("{0}", card.nameCard));
            }

            builder.AppendLine("!side");
            foreach (Card1 card in sideList)
            {
                builder.AppendLine(string.Format("{0}", card.nameCard));
            }

            File.WriteAllText(System.IO.Path.Combine(Path, DeckName), builder.ToString());
        }



        public void DeckConversion(string path)
        {
            
            string ext = System.IO.Path.GetExtension(open.FileName);

            switch (ext)
            {
                case ".ydk":
                {
                    DeckName = System.IO.Path.ChangeExtension(_currentFile, ".txt");
                    DeckYdk deck = BuildDeckTxt(path);
                    YdkToTxt(deck);
                    MessageBox.Show(msg[1]);
                    textBox1.Text = msg[0];
                }
                    break;
                case ".txt":
                {
                    DeckName = System.IO.Path.ChangeExtension(_currentFile, ".ydk");
                    DeckTxt deck1 = BuildDeckYdk(path);
                    TxtToYdk(deck1);
                    MessageBox.Show(msg[1]);
                    textBox1.Text = msg[0];
                }
                    break;
                default:
                    MessageBox.Show(msgErrors[1]);
                    break;
            }
        }


        
        private void btnSearch_Click(object sender, EventArgs e)
        {            
            DialogResult dr = open.ShowDialog();
            if (dr == DialogResult.OK)
                textBox1.Text = open.SafeFileName;
            _currentFile = open.FileName;
        }

        private void btnConvert_Click(object sender, EventArgs e)
        {
            DeckConversion(Path);
        }

        public Form1()
        {
            InitializeComponent();            
        }

        private void Form1_HelpButtonClicked(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBox.Show("Created by Giuseppe D.", "About");
            e.Cancel = true;
        }
        
        
    }
}

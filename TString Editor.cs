using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TString_Editor
{
    public partial class TString_Editor : Form
    {
        private List<Structure> TString = new List<Structure>();

        private StringBinaryReader br;
        private StringBinaryWriter bw;

        #region StringBinaryReader
        public class StringBinaryReader : BinaryReader
        {
            public StringBinaryReader(Stream input) : base(input)
            {

            }

            public override string ReadString()
            {
                uint num = ReadByte();
                if (num == 255)
                {
                    num = ReadUInt16();
                }

                if (num == 65535)
                {
                    num = ReadUInt32();
                }

                return Encoding.Default.GetString(ReadBytes((int)num));
            }
        }
        #endregion

        #region StringBinaryWriter
        public class StringBinaryWriter : BinaryWriter
        {
            public StringBinaryWriter(Stream input) : base(input)
            {

            }

            public override void Write(String value)
            {
                uint length = (uint)value.Length;
                if (length < 255)
                {
                    Write((byte)value.Length);
                }
                else
                {
                    Write(byte.MaxValue);
                    if (length < 65535)
                    {
                        Write((short)value.Length);
                    }
                    else
                    {
                        Write(ushort.MaxValue);
                        Write(value.Length);
                    }
                }

                Write(Encoding.Default.GetBytes(value));
            }
        }
        #endregion

        #region Structure
        private class Structure
        {
            public UInt16 ID;
            public String String;

            public Structure(UInt16 ID = 0, String String = "New string")
            {
                this.ID = ID;
                this.String = String;
            }
        }
        #endregion

        public TString_Editor()
        {
            InitializeComponent();
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OFD.FileName = "TString.tcd";
            OFD.Filter = "TString.tcd|TString*.tcd|All tcd|*.tcd|All Files|*.*";
            OFD.Title = "Open TString";

            if (OFD.ShowDialog() == DialogResult.OK)
            {
                Load(OFD.OpenFile());
            }
        }

        private new void Load(Stream direction)
        {
            br = new StringBinaryReader(direction);
            UInt16 count = br.ReadUInt16();
            for (int i = 0; i < count; i++)
            {
                TString.Add(new Structure());
                TString[i].ID = br.ReadUInt16();
                TString[i].String = br.ReadString();
            }
            br.Close();
            LoadListBox();
        }

        private void LoadListBox()
        {
            ListBox.Items.Clear();

            for (int i = 0; i < TString.Count; i++)
            {
                ListBox.Items.Add(i + ": " + TString[i].String);
            }

            CountToolStripStatusLabel.Text = "Count: " + ListBox.Items.Count.ToString();
        }

        private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            int si = ListBox.SelectedIndex;

            if (si >= 0 && si < TString.Count)
            {
                IDTextBox.Text = TString[si].ID.ToString();
                StringTextBox.Text = TString[si].String.ToString();
            }
        }

        private void SearchTextBox_TextChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(SearchTextBox.Text))
            {
                int index = ListBox.FindString(SearchTextBox.Text);

                if (index != -1)
                    ListBox.SetSelected(index, true);
                else
                    MessageBox.Show("The search string did not match any items in the ListBox");
            }
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            TString.Add(new Structure());
            LoadListBox();
            ListBox.SelectedIndex = ListBox.Items.Count - 1;
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            int si = ListBox.SelectedIndex;

            if (si >= 0 && si < TString.Count)
            {
                TString.RemoveAt(si);
                LoadListBox();
            }

            ListBox.SelectedIndex = si - 1;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            try
            {
                int si = ListBox.SelectedIndex;

                if (si >= 0 && si < TString.Count)
                {
                    TString[si].ID = Convert.ToUInt16(IDTextBox.Text);
                    TString[si].String = Convert.ToString(StringTextBox.Text);
                    LoadListBox();
                }

                ListBox.SelectedIndex = si;
            }
            catch
            {
                MessageBox.Show("An error occurred while saving.");
            }
        }

        private void Save(Stream direction)
        {
            bw = new StringBinaryWriter(direction);
            bw.Write(Convert.ToUInt16(TString.Count));
            foreach (Structure s in TString)
            {
                bw.Write(s.ID);
                bw.Write(s.String);
            }
            bw.Close();
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SFD.FileName = "TString.tcd";
            SFD.Filter = "TString.tcd|TString*.tcd|All tcd|*.tcd|All files|*.*";
            SFD.Title = "Save TString";

            if (SFD.ShowDialog() == DialogResult.OK)
            {
                Save(SFD.OpenFile());
                MessageBox.Show("Saved");
            }
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}

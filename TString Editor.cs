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
    public partial class TString_Editor : Form // Defining a partial class for the TString Editor that inherits from Form
    {
        private List<Structure> TString = new List<Structure>(); // List to hold string structures

        private StringBinaryReader br; // Instance of StringBinaryReader for reading strings from a binary stream
        private StringBinaryWriter bw; // Instance of StringBinaryWriter for writing strings to a binary stream

        #region StringBinaryReader
        public class StringBinaryReader : BinaryReader // Custom BinaryReader for reading strings
        {
            public StringBinaryReader(Stream input) : base(input) // Constructor accepting a stream
            {
            }

            public override string ReadString() // Overriding the ReadString method
            {
                try
                {
                    // Check if the current position is at the end of the stream
                    if (BaseStream.Position >= BaseStream.Length)
                        throw new EndOfStreamException("Unexpected end of stream while reading string length.");

                    uint length = ReadByte(); // Read the first byte as the length

                    // Check for special length cases
                    if (length == 255)
                    {
                        // Ensure enough data to read a UInt16 length
                        if (BaseStream.Length - BaseStream.Position < sizeof(ushort))
                            throw new InvalidDataException("Not enough data to read UInt16 length.");

                        length = ReadUInt16(); // Read the length as UInt16
                    }

                    if (length == 65535)
                    {
                        // Ensure enough data to read a UInt32 length
                        if (BaseStream.Length - BaseStream.Position < sizeof(uint))
                            throw new InvalidDataException("Not enough data to read UInt32 length.");

                        length = ReadUInt32(); // Read the length as UInt32
                    }

                    // Validate the length of the string
                    if (length > int.MaxValue)
                        throw new InvalidDataException("String length is too large.");

                    int totalBytesToRead = (int)length; // Calculate total bytes to read

                    // Ensure enough data to read the string
                    if (BaseStream.Length - BaseStream.Position < totalBytesToRead)
                        throw new InvalidDataException($"Not enough data to read string of length {totalBytesToRead} bytes.");

                    byte[] bytes = ReadBytes(totalBytesToRead); // Read the string bytes
                    return Encoding.Default.GetString(bytes); // Convert bytes to string
                }
                catch (Exception ex) // Catch any exceptions
                {
                    MessageBox.Show("Error when reading a line: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); // Show error message
                    throw; // Rethrow the exception
                }
            }
        }
        #endregion

        #region StringBinaryWriter
        public class StringBinaryWriter : BinaryWriter // Custom BinaryWriter for writing strings
        {
            public StringBinaryWriter(Stream input) : base(input) // Constructor accepting a stream
            {
            }

            public override void Write(string value) // Overriding the Write method
            {
                if (value == null) throw new ArgumentNullException(nameof(value)); // Check for null value

                try
                {
                    uint length = (uint)value.Length; // Get the length of the string

                    // Write the length based on its size
                    if (length < 255)
                    {
                        Write((byte)length); // Write length as a byte
                    }
                    else if (length < 65535)
                    {
                        Write((byte)255); // Write special byte for length
                        Write((ushort)length); // Write length as UInt16
                    }
                    else
                    {
                        Write((ushort)65535); // Write special value for length
                        Write((uint)length); // Write length as UInt32
                    }

                    byte[] bytes = Encoding.Default.GetBytes(value); // Convert string to bytes
                    Write(bytes); // Write the bytes to the stream
                }
                catch (Exception ex) // Catch any exceptions
                {
                    MessageBox.Show("Error when writing a string: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); // Show error message
                    throw; // Rethrow the exception
                }
            }
        }
        #endregion

        #region Structure
        private class Structure // Class to define a structure for storing ID and string
        {
            public UInt16 ID; // ID field
            public String String; // String field

            public Structure(UInt16 ID = 0, String String = "New string") // Constructor with default values
            {
                this.ID = ID; // Assign ID
                this.String = String; // Assign string
            }
        }
        #endregion

        public TString_Editor() // Constructor for TString_Editor
        {
            InitializeComponent(); // Initialize the form components
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e) // Event handler for opening a file
        {
            OFD.FileName = "TString.tcd"; // Set default file name
            OFD.Filter = "TString.tcd|TString*.tcd|All tcd|*.tcd|All Files|*.*"; // Set file filter
            OFD.Title = "Open TString"; // Set dialog title

            if (OFD.ShowDialog() == DialogResult.OK) // Show dialog and check result
            {
                Load(OFD.OpenFile()); // Load the selected file
            }
        }

        private new void Load(Stream direction) // Load method to read from a stream
        {
            try
            {
                TString.Clear(); // Clear the existing list
                IDTextBox.Clear(); // Clear the ID text box
                StringTextBox.Clear(); // Clear the string text box

                using (br = new StringBinaryReader(direction)) // Use StringBinaryReader to read from the stream
                {
                    UInt16 count = br.ReadUInt16(); // Read the count of structures

                    for (int i = 0; i < count; i++) // Loop through the count
                    {
                        UInt16 id = br.ReadUInt16(); // Read the ID
                        string str = br.ReadString(); // Read the string

                        TString.Add(new Structure(id, str)); // Add new structure to the list
                    }

                    LoadListBox(); // Load the list box with new data
                }
            }
            catch (IOException ioEx) // Catch IO exceptions
            {
                MessageBox.Show("File reading error: " + ioEx.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); // Show error message
            }
            catch (Exception ex) // Catch any other exceptions
            {
                MessageBox.Show("An error has occurred: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); // Show error message
            }
        }

        private void LoadListBox() // Method to load items into the list box
        {
            ListBox.Items.Clear(); // Clear existing items in the list box

            for (int i = 0; i < TString.Count; i++) // Loop through the TString list
            {
                ListBox.Items.Add(i + ": " + TString[i].String); // Add items to the list box
            }

            CountToolStripStatusLabel.Text = "Count: " + ListBox.Items.Count.ToString(); // Update the count label
        }

        private void ListBox_SelectedIndexChanged(object sender, EventArgs e) // Event handler for list box selection change
        {
            int si = ListBox.SelectedIndex; // Get the selected index

            if (si >= 0 && si < TString.Count) // Check if the index is valid
            {
                IDTextBox.Text = TString[si].ID.ToString(); // Set ID text box
                StringTextBox.Text = TString[si].String.ToString(); // Set string text box
            }
        }

        private void SearchTextBox_TextChanged(object sender, EventArgs e) // Event handler for search text box change
        {
            if (!string.IsNullOrEmpty(SearchTextBox.Text)) // Check if search text is not empty
            {
                int index = ListBox.FindString(SearchTextBox.Text); // Find the string in the list box

                if (index != -1) // Check if a match is found
                {
                    ListBox.SetSelected(index, true); // Select the matched item
                }
                else
                {
                    ToolStripStatusLabel.Text = "No matches found"; // Update status label if no match
                }
            }
            else
            {
                ToolStripStatusLabel.Text = ""; // Clear status label if search text is empty
            }
        }

        private void AddButton_Click(object sender, EventArgs e) // Event handler for add button click
        {
            TString.Add(new Structure()); // Add a new structure to the list
            LoadListBox(); // Refresh the list box
            ListBox.SelectedIndex = ListBox.Items.Count - 1; // Select the newly added item
        }

        private void DeleteButton_Click(object sender, EventArgs e) // Event handler for delete button click
        {
            int si = ListBox.SelectedIndex; // Get the selected index

            if (si >= 0 && si < TString.Count) // Check if the index is valid
            {
                TString.RemoveAt(si); // Remove the selected structure from the list
                LoadListBox(); // Refresh the list box

                if (TString.Count > 0) // Check if there are remaining items
                {
                    ListBox.SelectedIndex = Math.Min(si, TString.Count - 1); // Select the next item
                }
            }
        }

        private void SaveButton_Click(object sender, EventArgs e) // Event handler for save button click
        {
            int si = ListBox.SelectedIndex; // Get the selected index

            if (si >= 0 && si < TString.Count) // Check if the index is valid
            {
                try
                {
                    // Validate the ID input
                    if (!ushort.TryParse(IDTextBox.Text, out ushort id))
                    {
                        MessageBox.Show("Invalid ID value. It must be an integer from 0 to 65535.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning); // Show warning message
                        return; // Exit the method
                    }

                    TString[si].ID = id; // Update the ID of the selected structure
                    TString[si].String = StringTextBox.Text; // Update the string of the selected structure

                    LoadListBox(); // Refresh the list box
                    ListBox.SelectedIndex = si; // Reselect the updated item
                }
                catch (Exception ex) // Catch any exceptions
                {
                    MessageBox.Show("Couldn't save changes:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); // Show error message
                }
            }
        }

        private void Save(Stream direction) // Method to save data to a stream
        {
            try
            {
                using (bw = new StringBinaryWriter(direction)) // Use StringBinaryWriter to write to the stream
                {
                    bw.Write(Convert.ToUInt16(TString.Count)); // Write the count of structures

                    foreach (Structure s in TString) // Loop through each structure
                    {
                        bw.Write(s.ID); // Write the ID
                        bw.Write(s.String); // Write the string
                    }
                }
            }
            catch (IOException ioEx) // Catch IO exceptions
            {
                MessageBox.Show("File saving error: " + ioEx.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); // Show error message
            }
            catch (Exception ex) // Catch any other exceptions
            {
                MessageBox.Show("Error when saving: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); // Show error message
            }
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e) // Event handler for save menu item click
        {
            SFD.FileName = "TString.tcd"; // Set default file name
            SFD.Filter = "TString.tcd|TString*.tcd|All tcd|*.tcd|All files|*.*"; // Set file filter
            SFD.Title = "Save TString"; // Set dialog title

            if (SFD.ShowDialog() == DialogResult.OK) // Show dialog and check result
            {
                Save(SFD.OpenFile()); // Save the selected file
                MessageBox.Show("Saved"); // Show confirmation message
            }
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e) // Event handler for exit menu item click
        {
            Application.Exit(); // Exit the application
        }
    }
}
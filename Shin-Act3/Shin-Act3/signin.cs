using System;
using System.Windows.Forms;

namespace SIGN_IN
{
    public partial class signin : Form
    {
        public signin()
        {
            InitializeComponent();
            Text = "Register";
            button2.Click += Register_Click;
            label8.Cursor = Cursors.Hand;
            maskedTextBox3.PasswordChar = '●';
            maskedTextBox4.PasswordChar = '●';
            AcceptButton = button2;
        }

        private void Register_Click(object sender, EventArgs e)
        {
            string fullName = maskedTextBox1.Text.Trim();
            string username = maskedTextBox2.Text.Trim();
            string password = maskedTextBox3.Text;
            string confirm = maskedTextBox4.Text;
            if (fullName.Length == 0 || username.Length == 0 || password.Length == 0 || confirm.Length == 0)
            {
                MessageBox.Show("Complete all fields before registering.", "Register", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!string.Equals(password, confirm, StringComparison.Ordinal))
            {
                MessageBox.Show("Passwords do not match.", "Register", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                maskedTextBox4.Clear();
                maskedTextBox4.Focus();
                return;
            }
            if (password.Length < 6)
            {
                MessageBox.Show("Use a password with at least 6 characters.", "Register", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                string message;
                if (Database.Register(fullName, username, password, out message))
                {
                    MessageBox.Show(message, "Register", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Close();
                }
                else
                {
                    MessageBox.Show(message, "Register", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Database Connection", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void label1_Click(object sender, EventArgs e) { }
        private void label6_Click(object sender, EventArgs e) { }
        private void label8_Click(object sender, EventArgs e) { Close(); }
        private void label5_Click(object sender, EventArgs e) { }
    }
}

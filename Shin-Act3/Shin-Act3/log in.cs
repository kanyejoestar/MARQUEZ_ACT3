using System;
using System.Windows.Forms;

namespace SIGN_IN
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            Text = "Log In";
            button2.Click += Login_Click;
            label8.Cursor = Cursors.Hand;
            label8.Click += RegisterLink_Click;
            maskedTextBox4.PasswordChar = '●';
            AcceptButton = button2;
        }

        private void Login_Click(object sender, EventArgs e)
        {
            string username = maskedTextBox2.Text.Trim();
            string password = maskedTextBox4.Text;
            if (username.Length == 0 || password.Length == 0)
            {
                MessageBox.Show("Enter your username and password.", "Log In", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                string fullName;
                string message;
                if (Database.ValidateLogin(username, password, out fullName, out message))
                {
                    Hide();
                    using (dashboard form = new dashboard(fullName, username)) form.ShowDialog();
                    maskedTextBox4.Clear();
                    Show();
                    Activate();
                }
                else
                {
                    MessageBox.Show(message, "Log In", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Database Connection", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RegisterLink_Click(object sender, EventArgs e)
        {
            Hide();
            using (signin form = new signin()) form.ShowDialog();
            Show();
            Activate();
        }

        private void Form1_Load(object sender, EventArgs e) { }
        private void panel1_Paint(object sender, PaintEventArgs e) { }
        private void maskedTextBox4_MaskInputRejected(object sender, MaskInputRejectedEventArgs e) { }
        private void label6_Click(object sender, EventArgs e) { }
        private void label5_Click(object sender, EventArgs e) { }
        private void maskedTextBox2_MaskInputRejected(object sender, MaskInputRejectedEventArgs e) { }
    }
}

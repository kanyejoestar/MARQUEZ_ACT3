using System;
using System.Drawing;
using System.Windows.Forms;

namespace SIGN_IN
{
    public partial class dashboard : Form
    {
        private readonly string currentUsername;

        public dashboard() : this("User", "") { }

        public dashboard(string fullName, string username)
        {
            InitializeComponent();
            currentUsername = username;
            Text = "Dashboard";
            BackColor = Color.FromArgb(18, 22, 20);
            label1.Text = "Welcome, " + (string.IsNullOrWhiteSpace(fullName) ? username : fullName) + "!";
            label1.ForeColor = Color.White;
            label1.Font = new Font("Microsoft Sans Serif", 28F, FontStyle.Bold);
            label1.AutoSize = false;
            label1.TextAlign = ContentAlignment.MiddleCenter;
            label1.Location = new Point(60, 105);
            label1.Size = new Size(680, 100);
            button2.Text = "Sign Out";
            button2.BackColor = Color.FromArgb(0, 90, 48);
            button2.FlatStyle = FlatStyle.Flat;
            button2.FlatAppearance.BorderSize = 0;
            button2.Font = new Font("Microsoft Sans Serif", 14F, FontStyle.Bold);
            button2.Location = new Point(300, 245);
            button2.Size = new Size(200, 48);
            button2.Click += SignOut_Click;
        }

        private void SignOut_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}

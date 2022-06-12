using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Wordle
{
    public partial class Menu : Form
    {
        public Menu()
        {
            InitializeComponent();
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            GameUI GameJoin = new GameUI();
            GameJoin.Show();
        }

        private void btnHowToPlay_Click(object sender, EventArgs e)
        {
            Howtoplay howtoplay = new Howtoplay();
            howtoplay.Show();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Thank you for playing!", "Exit", MessageBoxButtons.OK);
            Application.Exit();
        }
    }
}

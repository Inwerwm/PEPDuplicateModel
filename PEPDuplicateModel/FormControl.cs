using PEPExtensions;
using PEPlugin;
using PEPlugin.Pmx;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PEPDuplicateModel
{
    public partial class FormControl : Form
    {
        Duplicater Duplicater { get; }

        public FormControl(Duplicater duplicater)
        {
            Duplicater = duplicater;

            InitializeComponent();
            Reload();
        }

        internal void Reload()
        {
            // TODO: プラグインの再読み込み処理を記述します。
        }

        private void FormControl_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var duplicated = Duplicater.Duplicate((int)numericUpDown1.Value, checkBoxAddAllParent.Checked, checkBoxSetLocalAxis.Checked);
            Duplicater.Update(duplicated);
        }
    }
}

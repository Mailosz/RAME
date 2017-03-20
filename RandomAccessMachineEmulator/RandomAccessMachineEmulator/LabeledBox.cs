using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RandomAccessMachineEmulator
{
    public partial class LabeledBox : UserControl
    {
        public string Label
        {
            set { label.Text = value; }
        }
        public int Value
        {
            get
            {
                int result;
                if (int.TryParse(valueBox.Text, out result))
                {
                    return result;
                }
                else throw new NotImplementedException();
            }
        }
        public LabeledBox()
        {
            InitializeComponent();
        }

        public LabeledBox(string title, string value)
        {
            InitializeComponent();

            label.Text = title;
            valueBox.Text = value;
        }
    }
}

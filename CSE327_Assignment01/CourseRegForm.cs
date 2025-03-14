﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CSE327_Assignment01
{
    public partial class RegistrationForm : Form, ISubscriber
    {
        private static RegisterCourseController rcc;
        public static Label l;
        public static List<Label> labels;
        public static int row;
        public static List<CheckBox> checks;
        public static CourseFactory cf;
        public bool saved;

        public RegistrationForm()
        {
            InitializeComponent();
            rcc = new RegisterCourseController();
            labels = new List<Label>();
            cf = new CourseFactory();
            row = 0;
            saved = false;
        }

        private void CourseRegForm_Load(object sender, EventArgs e)
        {
            comboBox1.Text = "Best for NSU";
            checks = new List<CheckBox>() { chkAE, chkFF, chkAM };
            foreach (CheckBox c in checks)
            {
                c.Click += C_Click;
                c.Enabled = false;
            }
            comboBox1.Enabled = false;
            calcDiscButton.Enabled = false;
            cf.getCList().ForEach(x => Txt.Items.Add(x.getId()));

        }

        private void C_Click(object sender, EventArgs e)
        {
            int i = 0;
            foreach (CheckBox c in checks) if (c.Checked) i++;
            comboBox1.Enabled = (i > 1);
            calcDiscButton.Enabled = (i > 0);
        }

        private void AddCourseBttn_Click(object sender, EventArgs e)
        {
            try
            {
                if (rcc.addCourse(Txt.Text) && Program.success)
                {
                    Course c = rcc.getCourse(Txt.Text);
                    int y = 10 + row * 25;
                    l = new Label() { Text = (row + 1).ToString(), Location = new Point(sl.Location.X, y), Size = new Size(25, 13) };
                    labels.Add(l);
                    panel1.Controls.Add(l);

                    l = new Label() { Text = c.getTitle(), Location = new Point(coursetitle.Location.X, y), Size = new Size(225, 13) };
                    labels.Add(l);
                    panel1.Controls.Add(l);

                    l = new Label() { Text = c.getCredit().ToString(), Location = new Point(credit.Location.X, y) };
                    labels.Add(l);
                    panel1.Controls.Add(l);

                    l = new Label() { Text = c.getTuitionPerCredit().ToString(), Location = new Point(tpc.Location.X, y) };
                    labels.Add(l);
                    panel1.Controls.Add(l);

                    l = new Label() { Text = c.getSubTotal().ToString(), Location = new Point(subtotal.Location.X, y), Size = new Size(35, 13) };
                    labels.Add(l);
                    panel1.Controls.Add(l);

                    
                    
                    row++;
                    GetChanged(rcc.getRegistration());
                }
            }
            catch (NullReferenceException n)
            {
                MessageBox.Show(n.GetType().ToString());

            }
            catch (FreedomFighterCourseLimitExceeded ffcle)
            {
                MessageBox.Show("Student who is a progeny of a Freedom Fighter cannot take more than 5 courses.", "Course limit exceeded! " + ffcle.ToString(), MessageBoxButtons.OK, MessageBoxIcon.Stop);

            }
            finally
            {
                checks.ForEach(x => x.Enabled = true);
                calcDiscButton.Enabled = true;
            }
        }

        public void newregistration()
        {
            foreach (Label l in labels)
                panel1.Controls.Remove(l);
            row = 0;
            totalLabel.Text = "0";
            discountLabel.Text = "0";
            gTotalLabel.Text = "0";
            rcc.makeNewRegistration();
            comboBox1.Enabled = false;
            calcDiscButton.Enabled = false;
            foreach (CheckBox c in checks) c.Checked = false;
            studentID.Text = "";
            Txt.Text = "";
            saved = false;
        }

        private void NewRegBttn_Click(object sender, EventArgs e)
        {
            string msg = "The current registration session has not been saved. " +
                "Do you want to discard this registration session and start a new one?";
            string ttl = "Save before new registration?";
            if (saved)
            {
                newregistration();
                Program.play = false;
            }
            else if (MessageBox.Show(msg, ttl, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                newregistration();
                Program.play = false;
            }
                
        }

        private void calcDiscButton_Click(object sender, EventArgs e)
        {
            int i = 0;
            checks.ForEach(x => i += x.Checked ? 1 : 0);
            if (i == 0)
            {
                return;
            }
                
            IDiscountStrategy disc_strat = null;

            if (i == 1)
            {
                if (chkAE.Checked)
                    disc_strat = new AcademicExcellenceDiscount();
                else if (chkAM.Checked)
                    disc_strat = new AboriginalMinorityDiscount();
                else // if (chkFF.Checked)
                    disc_strat = new FreedomFighterDiscount();
            }
            else
            {
                CompositeDiscount ds = new CompositeDiscount(comboBox1.Text);
                if (chkAE.Checked) ds.addDiscount(new AcademicExcellenceDiscount());
                if (chkAM.Checked) ds.addDiscount(new AboriginalMinorityDiscount());
                if (chkFF.Checked) ds.addDiscount(new FreedomFighterDiscount());
                disc_strat = ds;
            }
            rcc.getRegistration().setDiscountStrategy(disc_strat);
            GetChanged(rcc.getRegistration());
            Program.play = true;
        }

        public void GetChanged(Registration r)
        {
            totalLabel.Text = rcc.getRegistration().getExtraFeeAmount().ToString();
            discountLabel.Text = rcc.getRegistration().getDiscountedAmount().ToString();
            gTotalLabel.Text = rcc.getRegistration().getGrandTotal().ToString();
            SoundPlayer simpleSound = new SoundPlayer(Path.Combine(Directory.GetCurrentDirectory(), "beep.wav"));
            simpleSound.Play();
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            saved = true;
            Registration r = rcc.getRegistration();
            rcc.getRegistration().setID(studentID.Text);
            string result = "ID: " + r.getID() + Environment.NewLine + "Courses: " + r.getCourses()
                + Environment.NewLine + "Grand Total: " + r.getGrandTotal().ToString();
            MessageBox.Show(result, "Details", MessageBoxButtons.OK, MessageBoxIcon.Information);
            PersistenceFacade.getInstance().put(rcc.getRegistration());
        }

        private void fetchButton_Click(object sender, EventArgs e)
        {
            Registration temp = new Registration();
            Registration r = PersistenceFacade.getInstance().get(studentID.Text, temp) as Registration;
            if(r == null)
            {
                MessageBox.Show("No previous record found!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string result = "ID: " + r.getID() + Environment.NewLine + "Courses: " + r.ObtainedCourses()
                + Environment.NewLine + "Grand Total: " + r.getGT().ToString();
            MessageBox.Show(result, "Details", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            NewCourseForm ncf = new NewCourseForm();
            ncf.Show();
        }
    }
}

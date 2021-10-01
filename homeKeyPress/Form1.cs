using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace homeKeyPress
{


    public partial class Form1 : Form
    {


        private keyMonitorClass keyMonitor;
        private int iconNo;
        bool firstRun = true;
  



        public Form1()
        {
            InitializeComponent();
            //
            keyMonitor = new keyMonitorClass();
            keyMonitor.setHomeEndDetection(true);
            //
            iconNo = 0;
            //minimize
            //
            //this.WindowState = FormWindowState.Minimized;
            //this.ShowInTaskbar = false;
            //this.Hide();


        }

        /// <summary>
        /// ON / OFF button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            bool state = keyMonitor.homeEndDetection;
            keyMonitor.setHomeEndDetection(!state);
            updateIcon();
        }



        private void updateIcon()
        {
            bool onoff = keyMonitor.homeEndDetection;
            bool ctrldet = keyMonitor.ctrlDetectoinActive();
            if (onoff)
            {
                if (ctrldet)
                {
                    if (iconNo != 2)
                    {
                        notifyIcon1.Icon = Icon.FromHandle(((Bitmap)imageList1.Images[2]).GetHicon());
                        iconNo = 2;

                    }
                }
                else
                {
                    if (iconNo != 1)
                    {
                        notifyIcon1.Icon = Icon.FromHandle(((Bitmap)imageList1.Images[1]).GetHicon());
                        iconNo = 1;

                    }
                }
            }
            else
            {
                if (iconNo==0)
                {
                    notifyIcon1.Icon = Icon.FromHandle(((Bitmap)imageList1.Images[0]).GetHicon());
                    iconNo = 0;

                }
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Application HIDE
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            //hide!
            this.Hide();
            
        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {

        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            //
            this.Show();
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            updateIcon();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey  ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            
//            if (chkStartUp.Checked)
                rk.SetValue("homeKeyPressApp", Application.ExecutablePath);
//            else
//                rk.DeleteValue(AppName, false);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void Form1_Activated(object sender, EventArgs e)
        {
            if (firstRun)
            {
                firstRun = false;
                this.Hide();
            }
        }
    }
}

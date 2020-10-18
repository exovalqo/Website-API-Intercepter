using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Fiddler;
using Telerik.NetworkConnections;
using System.Collections;

namespace FiddlerHelper
{
    public partial class Form1 : Form
    {

        bool hasCert;
        int numLines = 0;
        ArrayList arrUrls;
        ArrayList responseBodies;
        public Form1()
        {
            InitializeComponent();
            hasCert = InstallCert();
            TextBox.CheckForIllegalCrossThreadCalls = false;
            ListView.CheckForIllegalCrossThreadCalls = false;
            FiddlerApplication.BeforeResponse += BeforeResponse;
            FiddlerApplication.Log.OnLogString += LogString;
            FiddlerApplication.ResponseHeadersAvailable += ResponseHeaderAvaliable;
            FiddlerApplication.BeforeRequest += BeforeRequest;
            listView1.Columns[0].Width = -2; //Information Column (I know, out of order) 
            listView1.Columns[1].Width = -2; // # Column 
        }

        private void ResponseHeaderAvaliable(Session oSession)
        {
            if (oSession.fullUrl.ToLower().Equals(textBox1.Text.ToLower()))
            {

                bool isText = oSession.oResponse.MIMEType.Contains("text/html");
                if (isText)
                {
                    oSession.bBufferResponse = true;
                    listView1.Items.Add("Added bBufferResponse!");
                }
            }
           

        }
        private void Label2_Click(object sender, EventArgs e)
        {

        }
        private void autoScroll()
        {
            int index = listView1.Items.Count - 1;
            try
            {
                listView1.Items[index].Focused = true;
            }catch(NullReferenceException ex)
            {


            }
            listView1.Items[index].Selected = true;
            listView1.EnsureVisible(index);

        }

        private void BeforeRequest(Session oSession)
        {
            for (int i = 0; i < arrUrls.Count; i++)
            {
                string temp = (string)arrUrls[i];
                if (oSession.uriContains(temp))
                {
                    if (checkBox1.Checked) // Checks If "Request Body Change Only" Checked
                    {
                        oSession.utilDecodeRequest();
                        oSession.utilSetRequestBody(textBox4.Text);
                    }
                    if(checkBox3.Checked)// Response Completely Change
                    {
                        // oSession.utilDecodeResponse();
                        oSession.utilCreateResponseAndBypassServer();
                    }
                }
            }
        }
        private void BeforeResponse(Session oSession)
        {
            for (int i = 0; i < arrUrls.Count; i++)
            {
                string temp = (string)arrUrls[i];
                if (oSession.uriContains(temp))
                {
                    if (checkBox2.Checked || checkBox3.Checked)
                    {
                        // oSession.utilDecodeResponse();

                        if (responseBodies.Count == 1)
                            oSession.utilSetResponseBody((string)responseBodies[0]);
                        else
                            oSession.utilSetResponseBody((string)responseBodies[i]);
                        oSession.utilDecodeResponse();
                        string oBody1 = oSession.GetResponseBodyAsString();
                        listView1.Items.Add(new ListViewItem(new[] { "Response Changed At " + oSession.fullUrl + " to \"" + oBody1 + "\"", (++numLines).ToString() }));
                        textBox3.AppendText(String.Format("\r\nURL: " + temp + " | Response Body Changed To: [{0}]", oBody1));

                        autoScroll();
                    }
                }
            }
            
           
        }
        private void LogString(object sender, LogEventArgs e)
         {

            listView1.Items.Add(new ListViewItem(new string[] { "Log " + e.LogString, "" + ++numLines }));
            autoScroll();
        }
        private void activateArrayLists()
        {
            string temp = textBox1.Text;
            if (temp.Contains(";"))
            {
                while (temp.Contains(";"))
                {
                    string substring = temp.Substring(0, temp.IndexOf(";"));
                    arrUrls.Add(substring);
                    temp = temp.Remove(0, temp.IndexOf(";") + 1);

                }
                if (!String.IsNullOrEmpty(temp))
                    arrUrls.Add(temp);
            }
            else
                arrUrls.Add(temp);

            string temp2 = textBox2.Text;
            if (temp2.Contains("\\;"))
            {
                while (temp2.Contains("\\;"))
                {
                    string substring = temp2.Substring(0, temp2.IndexOf("\\;"));
                    responseBodies.Add(substring);
                    temp2 = temp2.Remove(0, temp2.IndexOf("\\;") + 2);

                }
                if (!String.IsNullOrEmpty(temp2))
                    responseBodies.Add(temp2);
            }
            else
                responseBodies.Add(temp2);

        }
        private void Button1_Click(object sender, EventArgs e)
        {
            label7.Visible = false;
            
            arrUrls = new ArrayList();
            responseBodies = new ArrayList();
            activateArrayLists();


            if (button1.Text.Equals("Watch"))
            {
                button1.Text = "Stop";
                button1.ForeColor = Color.IndianRed;
                if (CertMaker.rootCertExists())
                {
                    if (!FiddlerApplication.IsStarted())
                    {


                        listView1.Items.Add(new ListViewItem(new string[] { "[!] Success: Found Certificate", ++numLines + "" }));
                        int port = 5555;
                      

                        FiddlerApplication.Startup(port, FiddlerCoreStartupFlags.Default);
                        listView1.Items.Add(new ListViewItem(new string[] { "Created endpoint listening on port " + port,++numLines + "" }));
                        listView1.Items.Add(new ListViewItem(new string[] { "Starting with settings: Default", ++numLines + "" }));
                        listView1.Items.Add(new ListViewItem(new string[] { "GateWay: " + CONFIG.UpstreamGateway, ++numLines + "" }));


                        textBox3.Text = "Starting... \r\nStarting Proxy On Port: " + port + ". With Proxy Type: " + CONFIG.UpstreamGateway.ToString();
                        textBox3.AppendText("\r\nUrls being watched: ");
                        for(int i = 0; i < arrUrls.Count; i++)
                        {
                            textBox3.AppendText("\r\n" + arrUrls[i]);
                        }

                    }
                    else
                        listView1.Items.Add("Fiddler Already Started");
                    
                }
                else
                {
                    listView1.Items.Add(++numLines + ":[?] Error: Couldn't Find Certificate");
                    // Add Create certificate button
                }
            }else
            {
                button1.Text = "Watch";
                button1.ForeColor = Color.DarkGreen;
                FiddlerApplication.oProxy.Detach();
                FiddlerApplication.Shutdown();
            }
        }
       
        private bool InstallCert()
        {
            if(!CertMaker.rootCertExists())
            {
                if (!CertMaker.createRootCert())
                    return false;

                if (!CertMaker.trustRootCert())
                    return false;

            }
            return true;
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
            numLines = 0;
        }

        private void ListView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void Button3_Click(object sender, EventArgs e)
        {
            textBox3.Text = "Cleared";
        }

        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBox1.Checked)
             checkBox4.Checked = false;
        }

        private void CheckBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox4.Checked)
                checkBox1.Checked = false;
        }

        private void CheckBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
                checkBox3.Checked = false;
        }

        private void CheckBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox3.Checked)
                checkBox2.Checked = false;
        }
    }
}

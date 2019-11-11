using System;
using System.Collections.Generic;
using System.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace Tool_Check_LOL
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private string LoLPath = "";
        private string PathSave = @"C:\\PathLOL.txt";
        private string[] lockfile;
        private string authorizationToken;
        private void BeGin()
        {
            Process[] processesByName = Process.GetProcessesByName("LeagueClient");
            if (processesByName.Length != 0)
            {
                string[] source = processesByName[0].MainModule.FileName.Split(new char[]
                {
            '\\'
                });
                this.LoLPath = string.Join("\\", source.Take(source.Count<string>() - 1).ToArray<string>());
            }
            string path = this.LoLPath + "\\lockfile";
            if (File.Exists(path))
            {
                Stream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                string text = new StreamReader(stream).ReadLine();
                this.lockfile = text.Split(new char[]
                {
            ':'
                });
                string s = "riot:" + this.lockfile[3];
                byte[] bytes = Encoding.UTF8.GetBytes(s);
                this.authorizationToken = "Basic " + Convert.ToBase64String(bytes);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(!File.Exists(PathSave))
            {
                MessageBox.Show("Chưa chọn Path LeagueClient kìa ba");
                return;
            }
            StreamReader sr = new StreamReader(PathSave);
            LoLPath = sr.ReadLine();
            BeGin();
            this.userData = JObject.Parse(this.lcuRequest("/lol-chat/v1/me", "GET", null));
            JObject jobject = JObject.Parse(this.lcuRequest("/lol-summoner/v1/summoners/" + this.userData["id"].ToString(), "GET", null));
            string Rich1 = "Name: " + this.userData["name"] + "\n";
            Rich1 += "LV: " + jobject["summonerLevel"] + "\n";
            Rich1 += "AFK: " + GetPenaltyTime() + "s\n";
            GetChampionsAndSkins();
            Rich1 += "Tướng: " + this.championsOwned.LongCount() + "\n";
            Rich1 += "Skins: " + this.skinsOwned.LongCount() + "\n";
            Dictionary<string, string> dictionary = this.GetRank();
            try
            {
                Rich1 += "Rank DD: " + dictionary["RANKED_SOLO_5x5"] + "\n"; 
            }
            catch
            {
                Rich1 += "Rank DD: Chưa RANK\n";
            }
            try
            {

                Rich1 += "5VS5: " + dictionary["RANKED_FLEX_SR"] + "\n";
            }
            catch
            {
                Rich1 += "5VS5: Chưa RANK\n";
            }
            richTextBox1.Text = Rich1.ToString();
            richTextBox2.Text = string.Join("|", this.championsOwned.ToArray());
            richTextBox3.Text = string.Join("|", this.skinsOwned.ToArray());

        }
        private JObject userData;
        private List<string> championsOwned = new List<string>();
        private List<string> skinsOwned = new List<string>();
        private string lcuRequest(string path, string type = "GET", string data = null)
        {
            ServicePointManager.ServerCertificateValidationCallback = ((object senderX, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => true);
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = (SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12);
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create("https://127.0.0.1:" + this.lockfile[2] + path);
            httpWebRequest.Method = type;
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.UserAgent = "Mozilla/5.0 (Windows NT 5.1; rv:28.0) Gecko/20100101 Firefox/28.0";
            httpWebRequest.Headers.Add("authorization", this.authorizationToken);
            if (data != null)
            {
                using (StreamWriter streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(data);
                }
            }
            string result;
            using (StreamReader streamReader = new StreamReader(((HttpWebResponse)httpWebRequest.GetResponse()).GetResponseStream()))
            {
                result = streamReader.ReadToEnd();
            }
            return result;
        }
        private string GetPenaltyTime()
        {
            JObject jobject = new JObject();
            jobject.Add("queueId", 430);
            try
            {
                this.lcuRequest("/lol-lobby/v2/lobby", "DELETE", null);
            }
            catch
            {
            }
            this.lcuRequest("/lol-lobby/v2/lobby", "POST", jobject.ToString());
            this.lcuRequest("/lol-lobby/v2/lobby/matchmaking/search", "POST", null);
            string text = this.lcuRequest("/lol-lobby/v2/lobby/matchmaking/search-state", "GET", null);
            if (text.Length == 0)
            {
                return "0.0";
            }
            string result;
            try
            {
                JObject jobject2 = JObject.Parse(text);
                result = jobject2["lowPriorityData"]["penaltyTime"].ToString();
            }
            catch
            {
                result = "0.0";
            }
            /*this.lcuRequest("/lol-lobby/v2/lobby/matchmaking/search", "DELETE", null);
            this.lcuRequest("/lol-lobby/v2/lobby", "DELETE", null);*/
            return result;
        }
        private void GetChampionsAndSkins()
        {
            this.championsOwned = new List<string>();
            this.skinsOwned = new List<string>();
            JArray jarray = JArray.Parse(this.lcuRequest("/lol-champions/v1/inventories/" + this.userData["id"].ToString() + "/champions", "GET", null));
            foreach (JToken jtoken in jarray)
            {
                JObject jobject = (JObject)jtoken;
                if (jobject["alias"].ToString() != "None" && jobject["ownership"]["owned"].ToString() == "True")
                {
                    this.championsOwned.Add(jobject["id"].ToString() + "-"+jobject["alias"].ToString());
                    foreach (JToken jtoken2 in ((JArray)jobject["skins"]))
                    {
                        JObject jobject2 = (JObject)jtoken2;
                        if (jobject2["isBase"].ToString() != "True" && jobject2["ownership"]["owned"].ToString() == "True")
                        {
                            this.skinsOwned.Add("championsskin_" + jobject2["id"].ToString() +"-"+jobject2["name"].ToString());
                        }
                    }
                }
            }
            this.championsOwned = this.championsOwned.Distinct<string>().ToList<string>();
            this.skinsOwned = this.skinsOwned.Distinct<string>().ToList<string>();
        }
        private Dictionary<string, string> GetRank()
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            Dictionary<string, string> result;
            try
            {
                JArray jarray = JArray.Parse(this.lcuRequest("/lol-ranked/v2/tiers?summonerIds=[" + this.userData["id"].ToString() + "]&queueTypes=[1,2]", "GET", null));
                if (jarray.Count == 0)
                {
                    throw new Exception();
                }
                foreach (JToken jtoken in ((JArray)jarray.First<JToken>()["achievedTiers"]))
                {
                    JObject jobject = (JObject)jtoken;
                    dictionary.Add(jobject["queueType"].ToString(), jobject["tier"].ToString() + " " + jobject["division"].ToString());
                }
                result = dictionary;
            }
            catch
            {
                result = new Dictionary<string, string>
        {
            {
                "RANKED_SOLO_5x5",
                "unrank"
            },
            {
                "RANKED_FLEX_SR",
                "unrank"
            }
        };
            }
            return result;

        }

        private void button2_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderDlg = new FolderBrowserDialog();
            folderDlg.ShowNewFolderButton = true;
            // Show the FolderBrowserDialog.  
            DialogResult result = folderDlg.ShowDialog();
            if (result == DialogResult.OK)
            {
                System.IO.File.WriteAllText(PathSave, folderDlg.SelectedPath);
                MessageBox.Show(folderDlg.SelectedPath);
            }
        }
    }
}

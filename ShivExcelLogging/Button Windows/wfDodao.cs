using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ActUtlTypeLib;

namespace ShivExcelLogging
{
    public partial class wfDodao : Form
    {
        public delegate void StringCap(string dd1, string dd2, string dd3);
        public event StringCap stringDoneDodao;
        int countProcess = 0;
        int countAllowEnd = 0;
        Timer timerProcess = new Timer();
        //Timer timerProcessClose = new Timer();
        SerialPort COMDoDao1, COMDoDao2, COMDoDao3;
        string bufferString_1, bufferString_2, bufferString_3;
        private static float valueMax1, valueMin1, valueMax2, valueMin2, valueMax3, valueMin3;
        ActUtlType plcRef;
        public wfDodao()
        {
            InitializeComponent();
            // Timer hiển thị trạng thái đợi
            timerProcess.Interval = 100;
            timerProcess.Tick += incProcess;
            timerProcess.Start();

            // Timer đóng cửa sổ
            //timerProcessClose.Interval = 200;
            //timerProcessClose.Tick += checkCloseForm;

            this.KeyPreview = true;
            this.KeyDown += CheckKeyDown;

            //giá trị mặc định

            valueMax1 = (float)0.0001;
            valueMin1 = 0;
            valueMax2 = (float)0.0001;
            valueMin2 = 0;
            valueMax3 = (float)0.0001;
            valueMin3 = 0;
        }

        private void CheckKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyData)
            {
                case Keys.Space:
                    if (stringDoneDodao != null) stringDoneDodao((valueMax1 - valueMin1).ToString("0.000"), (valueMax2 - valueMin2).ToString("0.000")
                        , (valueMax3 - valueMin3).ToString("0.000"));
                    this.Close();
                    break;
                default:
                    break;
            }
        }

        public wfDodao(ref ActUtlType PLC, ref SerialPort COM1, ref SerialPort COM2, ref SerialPort COM3) : this()
        {
            plcRef = PLC;

            COMDoDao1 = COM1;
            COMDoDao2 = COM2;
            COMDoDao3 = COM3;

            COMDoDao1.DataReceived -= ProcessCOMMessage1;
            COMDoDao1.DataReceived += ProcessCOMMessage1;

            COMDoDao2.DataReceived -= ProcessCOMMessage1;
            COMDoDao2.DataReceived += ProcessCOMMessage1;

            COMDoDao3.DataReceived -= ProcessCOMMessage1;
            COMDoDao3.DataReceived += ProcessCOMMessage1;

            Task.Delay(100);

            COMDoDao1.WriteLine("PRE+0\r\n");
            COMDoDao1.Write("OUT1\r\n");

            COMDoDao2.WriteLine("PRE+0\r\n");
            COMDoDao2.Write("OUT1\r\n");

            COMDoDao3.WriteLine("PRE+0\r\n");
            COMDoDao3.Write("OUT1\r\n");
        }

        private void ProcessCOMMessage1(object sender, SerialDataReceivedEventArgs e)
        {
            bufferString_1 += COMDoDao1.ReadExisting();
            bufferString_2 += COMDoDao2.ReadExisting();
            bufferString_3 += COMDoDao3.ReadExisting();

            if (bufferString_1.IndexOf("\r") > 0)
            {
                string tempStringRecevie1 = bufferString_1;
                bufferString_1 = "";
                try
                {
                    float temF = float.Parse(tempStringRecevie1);
                    if (valueMax1 < temF)
                        valueMax1 = temF;
                    if (valueMin1 > temF)
                        valueMin1 = temF;
                    Invoke(new MethodInvoker(delegate
                    {
                        lblDoDao1.Text = temF.ToString("0.00");
                    }));
                }
                catch
                {

                }
            }
            if (bufferString_2.IndexOf("\r") > 0)
            {
                string tempStringRecevie2 = bufferString_2;
                bufferString_2 = "";
                try
                {
                    float temF = float.Parse(tempStringRecevie2);
                    if (valueMax2 < temF)
                        valueMax2 = temF;
                    if (valueMin2 > temF)
                        valueMin2 = temF;
                    Invoke(new MethodInvoker(delegate
                    {
                        lblDoDao2.Text = temF.ToString("0.00");
                    }));
                }
                catch
                {

                }
            }
            if (bufferString_3.IndexOf("\r") > 0)
            {
                string tempStringRecevie3 = bufferString_3;
                bufferString_3 = "";
                try
                {
                    float temF = float.Parse(tempStringRecevie3);
                    if (valueMax3 < temF)
                        valueMax3 = temF;
                    if (valueMin3 > temF)
                        valueMin3 = temF;
                    Invoke(new MethodInvoker(delegate
                    {
                        lblDoDao3.Text = temF.ToString("0.00");
                    }));
                }
                catch
                {

                }
            }
        }
        //private void ProcessCOMMessage2(object sender, SerialDataReceivedEventArgs e)
        //{
        //    bufferString_2 += COMDoDao2.ReadExisting();
        //    if (bufferString_2.IndexOf("\r") > 0)
        //    {
        //        string tempStringRecevie2 = bufferString_2;
        //        bufferString_2 = "";
        //        try
        //        {
        //            float temF = float.Parse(tempStringRecevie2);
        //            if (valueMax2 < temF)
        //                valueMax2 = temF;
        //            if (valueMin2 > temF)
        //                valueMin2 = temF;
        //            Invoke(new MethodInvoker(delegate
        //            {
        //                lblDoDao2.Text = temF.ToString("0.00");
        //            }));
        //        }
        //        catch
        //        {

        //        }
        //    }
        //}

        //private void ProcessCOMMessage3(object sender, SerialDataReceivedEventArgs e)
        //{
        //    bufferString_3 += COMDoDao3.ReadExisting();
        //    if (bufferString_3.IndexOf("\r") > 0)
        //    {
        //        string tempStringRecevie3 = bufferString_3;
        //        bufferString_3 = "";
        //        try
        //        {
        //            float temF = float.Parse(tempStringRecevie3);
        //            if (valueMax3 < temF)
        //                valueMax3 = temF;
        //            if (valueMin3 > temF)
        //                valueMin3 = temF;
        //            Invoke(new MethodInvoker(delegate
        //            {
        //                lblDoDao3.Text = temF.ToString("0.00");
        //            }));
        //        }
        //        catch
        //        {

        //        }
        //    }
        //}
        /// <summary>
        /// Trả về Event độ đảo nếu có follow
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private void checkCloseForm(object sender, EventArgs e)
        //{
        //    if (textBox1.Text.IndexOf("DT100") >= 0)
        //    {
        //        if (stringDoneDodao != null) stringDoneDodao(textBox1.Text);
        //        Task.Delay(100);
        //        timerProcessClose.Stop();
        //        this.Close();
        //    }
        //    else
        //    {
        //        textBox1.Text = "DT10000-000004.0345M" + "DT10002-000004.1245M" + "DT10001-000004.4345M";
        //    }

        //}

        /// <summary>
        /// Hiển thị trạng thái đợi
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void incProcess(object sender, EventArgs e)
        {
            countProcess += 1;
            countAllowEnd += 1;
            if (countProcess > 8) countProcess = 0;
            lblProcess.Text = "";
            for (int i = 0; i < countProcess; i++)
            {
                lblProcess.Text += "_";
            }

            //Check PLC Reclick

            if (plcRef != null && countAllowEnd > 20)
            {
                int buttonRead;
                var iret = plcRef.GetDevice("X12", out buttonRead);
                if (buttonRead == 1)
                {
                    if (stringDoneDodao != null) stringDoneDodao((valueMax1 - valueMin1).ToString("0.000"), (valueMax2 - valueMin2).ToString("0.000")
                        , (valueMax3 - valueMin3).ToString("0.000"));
                    this.Close();
                }
            }
        }

        private void wfDodao_FormClosing(object sender, FormClosingEventArgs e)
        {
            COMDoDao1.Write("OUT0\r\n");
            COMDoDao2.Write("OUT0\r\n");
            COMDoDao3.Write("OUT0\r\n");

            COMDoDao1.DataReceived -= ProcessCOMMessage1;
            COMDoDao2.DataReceived -= ProcessCOMMessage1;
            COMDoDao3.DataReceived -= ProcessCOMMessage1;
            timerProcess.Stop();
        }

        private void lblCloseDodao_Click(object sender, EventArgs e)
        {
            if (stringDoneDodao != null) stringDoneDodao((valueMax1 - valueMin1).ToString("0.00"), (valueMax2 - valueMin2).ToString("0.00")
                       , (valueMax3 - valueMin3).ToString("0.00"));
            this.Close();
        }

        /// <summary>
        /// Nếu nhận được dữ liệu thì chuẩn bị đóng Form sau 0.2s
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            //if (textBox1.Text.Length > 15) timerProcessClose.Start();
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Threading;
using System.IO;
using ActUtlTypeLib;

namespace ShivExcelLogging
{
    public partial class Capture : Form
    {
        public delegate void saveImageCompleteDelegate(string link);
        public event saveImageCompleteDelegate saveImageComplete;
        ActUtlType plcFX3G;
        bool conditionRunCam = true;
        Mat m = new Mat(), mm = new Mat();
        VideoCapture captureV;
        string posSaveImage = "D:\\";
        string fileName;
        Thread newThread;
        private int buttonRead;
        public int _indexCamera = 0;

        public Capture(ref ActUtlType PLC) : this("Unknow", ref PLC)
        {

        }

        public Capture(string tempString, ref ActUtlType PLC)
        {
            InitializeComponent();
            _indexCamera = Setting.Default.indexCamera;
            plcFX3G = PLC;
            posSaveImage = tempString;
            newThread = new Thread(runCamera);
            newThread.IsBackground = true;
            newThread.Start();
        }

        private void runCamera()
        {
            while (true)
            {
                try
                {
                    if (conditionRunCam)
                    {
                        if (captureV == null) captureV = new VideoCapture(_indexCamera);
                        captureV.Read(m);
                        Mat tempImage = new Mat();
                        CvInvoke.Resize(m, tempImage, new Size(imageBox1.Width, imageBox1.Height), 0, 0, Emgu.CV.CvEnum.Inter.Linear);
                        if (!m.IsEmpty) imageBox1.Image = tempImage;
                        Thread.Sleep(50);
                    }
                    //if (!conditionRunCam)
                    //    if (captureV != null) captureV.Dispose();
                    plcFX3G.GetDevice("X13", out buttonRead);
                    if (buttonRead == 1)
                    {
                        Invoke(new MethodInvoker(delegate { btnCapture.PerformClick(); }));
                        break;
                    }
                }
                catch
                {
                    return;
                }

            }
        }

        private void btnCapture_Click(object sender, EventArgs e)
        {
            conditionRunCam = false;
            if (captureV == null) MessageBox.Show("Không có Camera kết nối");
            else
            {
                captureV.Read(mm);
                Mat tempImage = new Mat();
                CvInvoke.Resize(mm, tempImage, new Size(imageBox1.Width, imageBox1.Height), 0, 0, Emgu.CV.CvEnum.Inter.Linear);
                if (!mm.IsEmpty) imageBox1.Image = tempImage;

                // Tạo thư mục lưu
                string tempDirection = posSaveImage.Substring(0, posSaveImage.LastIndexOf("\\") + 1);
                if (!Directory.Exists(tempDirection)) Directory.CreateDirectory(tempDirection);
                //if (!Directory.Exists("E:\\Log\\" + DateTime.Now.ToString("yyyyMM")))
                //    Directory.CreateDirectory("E:\\Log\\" + DateTime.Now.ToString("yyyyMM"));
                // Tên file
                fileName = posSaveImage + DateTime.Now.ToString("_yyyyMMdd_hhmmss") + ".jpg";
                mm.Save(fileName);
                // Fire Event - Gửi đường dẫn ảnh theo Event
                saveImageComplete?.Invoke(fileName);
            }
            this.Close();
        }

        private void Capture_FormClosed(object sender, FormClosedEventArgs e)
        {
            captureV.Dispose();
        }

        private void btnCloseCapture_Click(object sender, EventArgs e)
        {
            newThread.Abort();
            Form.ActiveForm.Close();
        }
    }
}

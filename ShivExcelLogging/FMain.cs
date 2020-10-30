using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ActUtlTypeLib;
using Excel = Microsoft.Office.Interop.Excel;
using System.Diagnostics;
using System.IO.Ports;
using System.Runtime.InteropServices;

namespace ShivExcelLogging
{
    public partial class FMain : Form
    {
        #region // Khai báo
        ActUtlType plcFX3G;
        Excel.Application myExcel;

        short D100;
        int countPulse = 0, countNumberOfReadData = 0;
        Thread plcThread, dataThread;
        bool readLeft = false, readRight = false, dataLogging = false;
        string readDoneConfirm;
        int indexRow, indexCol, numberRead = 0;
        int EXCEL_CHART_START_ROW = 4;
        int EXCEL_CHART_START_COLUUM = 2;
        string productName;
        const int LIMIT_NUMBER = 25;
        private Capture formCap;
        private int tempDirection;

        short[,] maxVibrate = new short[40, 2];
        short[,,] collectVibrate = new short[40, 2, 20];
        short[] convertIndex = new short[50];
        short[] X0toX5 = new short[10];
        short tempCountPulse = 0;

        object misValue = System.Reflection.Missing.Value;
        private bool conditionRunCam;

        public MessageBoxVisual MessageOKForm = new MessageBoxVisual();
        private bool bitForward;
        private bool bitBackward;
        private bool bitManual = false;
        private short countPulsetemp;
        private bool bitCaptureOpen;
        private string firstString;
        private string secondString;
        private string thirdString;
        private string stringKhehoTinh;
        Dictionary<string, bool> currentPLCBit = new Dictionary<string, bool>();
        private SerialPort COMSylvac1, COMSylvac2, COMSylvac3, COMSylvac4;
        private bool excelUsing;
        private bool formLock;
        private string imageCaptureLink;
        #endregion

        #region LƯU ÂM THANH
        private string SoundPath = "D:\\Sound_File";
        [DllImport("winmm.dll", EntryPoint = "mciSendStringA", ExactSpelling = true, CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern int record(string lpstrCommand, string lpstrReturnString, int uReturnLength, int hwndCallback);
        private void RecordStart()
        {
            record("open new Type waveaudio Alias recsound", "", 0, 0);
            record("record recsound", "", 0, 0);
        }

        private void RecordStopAndSave(string fileName)
        {
            record("save recsound " + @fileName + ".wav", "", 0, 0);
            record("close recsound", "", 0, 0);
        }
        #endregion

        /// <summary>
        /// Hàm khởi tạo FMain
        /// </summary>
        public FMain()
        {
            InitializeComponent();

            InitializeValue();
            StartPLCThread();
        }

        /// <summary>
        /// Khởi tạo giá trị khi mở ứng dụng
        /// - Đóng App Excel cũ
        /// - Kết nối COM Sylvac
        /// - Tạo mới ứng dụng Excle
        /// - Khởi tạo giao diện
        /// - Khởi tạo mảng vị trí lưu trữ
        /// - Tạo thư mục lưu file Log
        /// </summary>


        #region CÁC PHẦN LIÊN QUAN PLC

        /// <summary>
        /// Tạo và chạy Thread lấy dữ liệu PLC
        /// </summary>
        private void StartPLCThread()
        {
            plcThread = new Thread(plcCycleReadAndWriteValue);
            plcThread.IsBackground = true;
            plcThread.Start();
        }

        /// <summary>
        /// Chu trình lấy dữ liệu PLC, update mỗi 10ms
        /// </summary>
        private void plcCycleReadAndWriteValue()
        {
            // Khai báo kết nối đến PLC, với cổng kết nối plcStationNumber (cài đặt qua Mitsubishi Communication Setup Utility)
            plcFX3G = new ActUtlType();
            plcFX3G.ActLogicalStationNumber = Setting.Default.StationPLC;
            currentPLCBit.Add("X10", false);
            currentPLCBit.Add("X11", false);
            currentPLCBit.Add("X12", false);
            currentPLCBit.Add("X13", false);
            while (true)
            {
                //CheckPlcOnlineOrNOt();              // Kiểm tra xem có đọc được dữ liệu từ PLC không
                ReadD100AndSaveToArray(); // Lấy dữ liệu D100 lưu vào mảng 20pt
                ReadX0ToX5AndProcess(); // Đọc các giá trị Input của PLC -> PC
                CountNumberOfPulseX0(); // Đếm xung số vòng quay
                ReadNewPLCButtonStatus(); // Đọc giá trị 3 nút nhấn - Chụp ảnh, Đo khe hở, Đo độ đảo
                Thread.Sleep(10);
            }
        }

        /// <summary>
        /// Đọc giá trị 3 nút nhấn - Chụp ảnh, Đo khe hở, Đo độ đảo
        /// </summary>
        private void ReadNewPLCButtonStatus()
        {
            int buttonRead;
            var iret = plcFX3G.GetDevice("X11", out buttonRead);
            // Kiểm tra nhấn nút đo Khe hở
            if (buttonRead == 1)
            {
                Console.WriteLine("Nhan nut X11!");
                if (!currentPLCBit["X11"])
                {
                    currentPLCBit["X11"] = true;
                    wfKheho wftemp = new wfKheho(ref plcFX3G, ref COMSylvac1);
                    wftemp.stringDoneKheho += InputKhehoToTestlerOrExcel;
                    wftemp.ShowDialog();
                }
            }
            else
            {
                currentPLCBit["X11"] = false;
            }

            // Kiểm tra nhấn nút đo độ đảo
            plcFX3G.GetDevice("X12", out buttonRead);
            if (buttonRead == 1)
            {
                Console.WriteLine("Nhan nut X6!");
                if (!currentPLCBit["X12"])
                {
                    currentPLCBit["X12"] = true;
                    wfDodao wftemp1 = new wfDodao(ref plcFX3G, ref COMSylvac2, ref COMSylvac3, ref COMSylvac4);
                    wftemp1.stringDoneDodao += InputDoDaoToTesler;
                    wftemp1.ShowDialog();
                    formLock = false;
                }
            }
            else
            {
                currentPLCBit["X12"] = false;
            }

            // Kiểm tra nhấn nút chụp ảnh
            plcFX3G.GetDevice("X13", out buttonRead);
            if (buttonRead == 1)
            {
                if (!bitCaptureOpen)
                {
                    // Cập nhật tên Order
                    GetNameInExcelToProductName();
                    // Mở form chụp ảnh, gửi kèm địa chỉ lưu ảnh
                    string tempDic = @txtLoggingFolder.Text + "\\" + DateTime.Now.ToString("ddMMyyyy") + @"Image\" + productName + "_" + (numberRead + 1).ToString();
                    Capture wftemp2 = new Capture(tempDic, ref plcFX3G);
                    wftemp2.saveImageComplete += ProcessImageSavedLink;
                    wftemp2.ShowDialog();
                    Console.WriteLine("Nhan nut X13!");
                }
            }

        }

        /// <summary>
        /// Đọc các giá trị trạng thái nút nhấn từ PLC =>> thay đổi trạng thái các bit điều khiển của chương trình
        /// </summary>
        private void ReadX0ToX5AndProcess()
        {
            plcFX3G.ReadDeviceRandom2("M10\nM11\nM12\nM13\nM14\nM15", 6, out X0toX5[0]);
            //MessageBox.Show(X0toX5[0].ToString());
            if (X0toX5[1] == 1)
            {
                bitForward = true;
                btnLampF.BackColor = Color.MidnightBlue;
            }
            else
            {
                bitForward = false;
                if (!readLeft) btnLampF.BackColor = Color.Transparent;
            }
            if (X0toX5[3] == 1)
            {
                bitBackward = true;
                btnLampB.BackColor = Color.MidnightBlue;
            }
            else
            {
                bitBackward = false;
                if (!readRight) btnLampB.BackColor = Color.Transparent;
            }
            if (X0toX5[4] == 1)
            {
                btnLampG.BackColor = Color.MidnightBlue;
            }
            else
            {
                btnLampG.BackColor = Color.Transparent;
            }
        }

        /// <summary>
        /// Kiểm tra PLC có được kết nối hay không bằng cách lấy dữ liệu M8000 từ PLC, nếu mất kết nối =>> gửi message thông báo
        /// </summary>
        private void CheckPlcOnlineOrNOt()
        {
            short temp04;
            if (plcFX3G.ReadDeviceRandom2("M8000", 1, out temp04) != 0 && dataLogging)
            {
                MessageBox.Show("PLC Connect Error!");
                Invoke(new MethodInvoker(delegate { btnStop.PerformClick(); lblNumber.Text = "    Error!!"; }));
            }
            if (plcFX3G.ReadDeviceRandom2("M8000", 1, out temp04) == 0)
            {
                if (!dataLogging)
                {
                    Invoke(new MethodInvoker(delegate
                    {
                        lblNumber.Text = "    Ready!";
                        txtNumberRun.Text = "";
                    }));
                }
                else
                {
                    Invoke(new MethodInvoker(delegate
                    {
                        lblNumber.Text = "Number: ";
                    }));
                }
            }
        }

        /// <summary>
        /// Kiểm tra điều kiện đang lấy dữ liệu không?
        /// Lấy giá trị độ rung (D100) từ PLC và lưu vào mảng dữ liệu (theo chiều chạy của động cơ được chọn - thuận/ nghịch)
        /// </summary>
        private void ReadD100AndSaveToArray()
        {
            // Có thể thêm Count vào để giảm thời gian lấy mẫu D100
            string zDevice = "D100";
            plcFX3G.ReadDeviceRandom2(zDevice, 1, out D100);
            // Kiểm tra có đang lấy dữ liệu hay không? Nếu chưa và chưa lấy xong 20 dữ liệu thì lấy dữ liệu
            if (bitBackward && countNumberOfReadData < 20)
            {
                collectVibrate[numberRead, 1, countNumberOfReadData] = D100;
                countNumberOfReadData += 1;
            }
            if (bitForward && countNumberOfReadData < 20)
            {
                collectVibrate[numberRead, 0, countNumberOfReadData] = D100;
                countNumberOfReadData += 1;
            }
        }

        /// <summary>
        /// Đọc số xung từ PLC (D10)
        /// Mỗi xung là 1 vòng quay của động cơ =>> Kiểm soát điều kiện vòng quay của chương trình
        /// Ví dụ: Quay 5 vòng bắt đầu lấy dữ liệu, ...
        /// </summary>
        private void CountNumberOfPulseX0()
        {
            // D10 là số xung vòng quay, được đếm trong PLC
            string zDevice = "D10";
            plcFX3G.ReadDeviceRandom2(zDevice, 1, out countPulsetemp);
            if (tempCountPulse != countPulsetemp)
            {
                tempCountPulse = countPulsetemp;
                Invoke(new MethodInvoker(delegate { btnLampP.BackColor = Color.MidnightBlue; }));
            }
            countPulse = (int)countPulsetemp;
            if ((!bitBackward) && (!bitForward)) Invoke(new MethodInvoker(delegate { txtNumberPulse.Text = ""; }));
        }

        /// <summary>
        /// Kiểm tra kết nối PLC bằng cách đọc giá trị M8000
        /// Nếu mất kết nối thì thông báo PLC Error
        /// </summary>
        private void RecheckPlcConnection()
        {
            short tempData;
            if (plcFX3G.ReadDeviceRandom2("SM400", 1, out tempData) != 0)
            {
                var iRet = plcFX3G.Open();
                if (iRet != 0)
                {
                    MessageBox.Show("PLC Error!");
                    txtNumberRun.Text = "";
                }
                else ChageStatusLogging();
            }
            else ChageStatusLogging();
        }
        #endregion

        #region EVENT CÁC CONTROL WINFORM
        private void btnCloseApp_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void lblTitle_Click(object sender, EventArgs e)
        {

        }

        private void btnHome_Click(object sender, EventArgs e)
        {
            pnlMainMenu.BringToFront();
            conditionRunCam = false;
        }

        private void btnSetting_Click(object sender, EventArgs e)
        {
            pnlSetting.BringToFront();
            conditionRunCam = false;
            //threadRunCamera.Abort();
        }

        private void btnCapture_Click(object sender, EventArgs e)
        {
            //pnlCapture.BringToFront();
            formCap = new Capture(ref plcFX3G);
            formCap.Show();
        }

        /// <summary>
        /// Khi nhấn nút Start, chu trình lấy dữ liệu bắt đầu
        /// 1. Mở lại kết nối PLC
        /// 2. Mở ứng dụng Excel
        /// 3. Chạy Thread tổng hợp dữ liệu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStart_Click(object sender, EventArgs e)
        {
            if (!bitManual)
            {
                RecheckPlcConnection();
                if (!chkNoExcel.Checked)
                    OpenExcelAplication();
                StartThreadToCollectionData();
            }
            else
            {
                btnStart.BackColor = Color.MidnightBlue;
                btnStart.ForeColor = Color.White;
                bitManual = false;
            }
            // Khởi tạo lại hiển thị nút nhấn Manual
            btnManual.BackColor = Color.White;
            btnManual.ForeColor = Color.Black;
            // Tắt trạng thái Manual
            bitManual = false;
        }

        /// <summary>
        /// Nút nhấn dừng lấy dữ liệu
        /// Dừng quá trình lấy dữ liệu bằng cách đặt giá trji dataLogging sang OFF
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStop_Click(object sender, EventArgs e)
        {
            StopDataLoggingIfRunning();
            plcFX3G.Close();
        }

        /// <summary>
        /// Khi nhấn nút Brower thì mở giao điện để tìm đến file Excel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnBrowser_Click(object sender, EventArgs e)
        {
            // Browser file and save fileIndex to Textbox
            if (openFileDialogExcel.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                txtFileIndex.Text = openFileDialogExcel.FileName.ToString();
        }

        /// <summary>
        /// Khi đường dẫn file Excel thay đổi thì Update vào trong Setting
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtFileIndex_TextChanged(object sender, EventArgs e)
        {
            Setting.Default.FileIndex = txtFileIndex.Text;
            Setting.Default.Save();
        }

        /// <summary>
        /// Nếu giá trị nhập trong StartPos thayd dổi, thì tính toán lại hàng và cột trong file Excel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtStartPos_TextChanged(object sender, EventArgs e)
        {
            CaculateRowAndColuumOfNamePostion(); // Tính toán hàng cột
            if (txtStartPos.Text.Length > 1)
            {
                Setting.Default.StartPos = txtStartPos.Text;
                Setting.Default.Save();
            }
        }

        /// <summary>
        /// Nếu giá trị nhập trong NamePos thay đổi, thì Update lại tên sản phẩm
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtNamePos_TextChanged(object sender, EventArgs e)
        {
            Setting.Default.NamePos = txtNamePos.Text;
            Setting.Default.Save();
            GetNameInExcelToProductName();
        }
        /// <summary>
        /// Chương trình test lưu ảnh Excel - chưa sử dụng
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnTestSend_Click(object sender, EventArgs e) // Test Copy Image to Excel Cell and Keep Image In Range
        {
            try
            {
                Excel.Worksheet tempWorkSheet = myExcel.ActiveWorkbook.Worksheets[5];
                Excel.Range tempRange2 = (Excel.Range)tempWorkSheet.Cells[1, 5];
                Excel.Range oRange = (Excel.Range)tempWorkSheet.Cells[1, 1];
                Image tempImage = Image.FromFile("D:\\latus.jpg");
                Clipboard.SetDataObject(tempImage, true);
                Excel.Shape shape1 = tempWorkSheet.Shapes.AddPicture("D:\\latus.jpg", Microsoft.Office.Core.MsoTriState.msoFalse, Microsoft.Office.Core.MsoTriState.msoCTrue, oRange.Left, oRange.Top, 200, 150);
                shape1.Placement = Excel.XlPlacement.xlMoveAndSize;
            }
            catch { }
        }

        /// <summary>
        /// Cập nhật trạng thái Auto/Manual khi nhấn nút Manual trên giao diện
        /// Chế độ Manual cho phép lựa chọn ô điền dữ liệu bằng cách chỉ chuột vào ô bất kỳ trong file Excel đang mở
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnManual_Click(object sender, EventArgs e)
        {
            if (dataLogging)
            {
                btnStart.BackColor = Color.White;
                btnStart.ForeColor = Color.Black;
            }
            btnManual.BackColor = Color.MidnightBlue;
            btnManual.ForeColor = Color.White;
            bitManual = true;
        }

        /// <summary>
        /// Đường đến thư mục lưu file Log
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtBrowser2_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                txtLoggingFolder.Text = folderBrowserDialog.SelectedPath.ToString();
        }

        /// <summary>
        /// Update vào Setting nếu thay đổi
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtLoggingFolder_TextChanged(object sender, EventArgs e)
        {
            Setting.Default.LogFolderIndex = txtLoggingFolder.Text;
            Setting.Default.Save();
        }

        private void FMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                myExcel.ActiveWorkbook.Close(false, misValue, misValue);
                plcFX3G.Close();
            }
            catch { }
        }

        /// <summary>
        /// Khi nhấn nút Reset => hiển thị thông báo có cho phép reset không
        /// Nếu có, Reset tất cả về mặc định
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnReset_Click(object sender, EventArgs e)
        {
            MessageOKForm = new MessageBoxVisual();
            IfConfirmResetDataThenResetAllToNull();
            txtNumberRun.Text = "1";
            GenerateNewFolderLogFile();
        }

        /// <summary>
        /// Đóng các Thread khi tắt chương trình
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            plcThread.Abort();
            try
            {
                dataThread.Abort();
            }
            catch
            {

            }
            plcFX3G.Close();
        }
        #endregion

        #region CÁC HÀM XỬ LÝ DỮ LIỆU

        /// <summary>
        /// Hàm khởi tạo những giá trị mặc định
        /// </summary>
        private void InitializeValue()
        {
            CloseAllExcelApps();
            LoadSylvacCOM();
            myExcel = new Excel.Application();
            LoadDefaultDisplay();
            CaculateArrayToConvertIndexOfExcel();
            CaculateRowAndColuumOfNamePostion();
            CreatLogFolder();
        }

        /// <summary>
        /// Tạo kết nối COM đến thiết bị Sylvac
        /// </summary>
        private void LoadSylvacCOM()
        {
            //COM đồng ho đo khe hở
            COMSylvac1 = new SerialPort(Setting.Default.COMSylvac1, 4800, Parity.Even, 7, StopBits.Two);
            COMSylvac1.DtrEnable = true;
            //COMSylvac1.Open();

            //COM đồng hồ đo độ đảo 1
            COMSylvac2 = new SerialPort(Setting.Default.COMSylvac2, 4800, Parity.Even, 7, StopBits.Two);
            COMSylvac2.DtrEnable = true;
            //COMSylvac2.Open();

            //COM đồng hồ đo độ đảo 2
            COMSylvac3 = new SerialPort(Setting.Default.COMSylvac3, 4800, Parity.Even, 7, StopBits.Two);
            COMSylvac3.DtrEnable = true;
            //COMSylvac3.Open();

            //COM đồng hồ đo độ đảo 3
            COMSylvac4 = new SerialPort(Setting.Default.COMSylvac4, 4800, Parity.Even, 7, StopBits.Two);
            COMSylvac4.DtrEnable = true;
            //COMSylvac4.Open();

        }

        /// <summary>
        /// Khởi tạo các giá trị trong giao diện hiển thị về mặc định -Load những giá trị sẵn có trong Setting
        /// </summary>
        private void LoadDefaultDisplay()
        {
            txtNamePos.Text = Setting.Default.NamePos;
            txtStartPos.Text = Setting.Default.StartPos;
            txtFileIndex.Text = Setting.Default.FileIndex;
            txtLoggingFolder.Text = Setting.Default.LogFolderIndex;
            pnlMainMenu.BringToFront();
            lblNumber.Text = "    Ready!";
            txtNumberRun.Text = "";
        }

        /// <summary>
        /// Tạo mảng giá trị 0,1,2,3,4,5, 10, 20, 30, 40, ... 200
        /// </summary>
        private void CaculateArrayToConvertIndexOfExcel()
        {
            for (short i = 0; i < 50; i++)
            {
                if (i <= 5)
                    convertIndex[i] = i;
                else
                    convertIndex[i] = (short)((i - 5) * 10);
            }
        }

        /// <summary>
        /// Tính toán giá trị hàng, cột của ô Excel, dựa trên giá trị cài đặt
        /// Ví dụ: Giá trị cài đặt là "A1" =>> hàng 1, cột 1
        /// </summary>
        private void CaculateRowAndColuumOfNamePostion()
        {
            if (txtStartPos.Text.Length > 1)
            {
                char tempChar = txtStartPos.Text[0];
                tempChar = char.ToUpper(tempChar);
                indexCol = ((int)tempChar) - 64;
                indexRow = Int32.Parse(txtStartPos.Text.Substring(1, (txtStartPos.Text.Length - 1)));
            }
        }

        /// <summary>
        /// Khởi tạo thư mục chứa file Log của chương trình dựa theo ngày tháng năm
        /// </summary>
        private void CreatLogFolder()
        {
            if (!Directory.Exists(Setting.Default.LogFolderIndex + "\\" + DateTime.Now.ToString("yyyyMM")))
                Directory.CreateDirectory(Setting.Default.LogFolderIndex + "\\" + DateTime.Now.ToString("yyyyMM"));
        }
        void CreateFolderSaveSound()
        {
            if (!Directory.Exists(SoundPath + "\\" + DateTime.Now.ToString("dd/MM/yyyy")))
                Directory.CreateDirectory(SoundPath + "\\" + DateTime.Now.ToString("dd/MM/yyyy"));
        }
        /// <summary>
        /// Đóng tất các các ứng dụng đang chạy có chứa từ khóa EXCEL
        /// </summary>
        private void CloseAllExcelApps()
        {
            foreach (var process in Process.GetProcessesByName("EXCEL"))
            {
                process.Kill();
            }
        }

        /// <summary>
        /// Điền link ảnh đã lưu vào ô Excel
        /// </summary>
        /// <param name="link"></param>
        private void ProcessImageSavedLink(string link)
        {
            imageCaptureLink = link;
            // Lưu vào Excel tùy theo chế độ Auto/Manual
            if (!bitManual)
            {
                if (chkNoExcel.Checked)
                    UpdateDataToTestler("Capture");
                else
                    UpdateDataToAutoCellExcel("Capture");
            }
            else
            {
                UpdateDataToActiveCell("Capture");
            }
        }

        /// <summary>
        /// Đổi màu nút nhấn Start khi quá trình lấy dữ liệu bắt đầu
        /// </summary>
        private void ChageStatusLogging()
        {
            btnStart.BackColor = Color.MidnightBlue;
            btnStart.ForeColor = Color.White;
            dataLogging = true;
        }

        /// <summary>
        /// Mở file Excel để tiến hành điền dữ liệu
        /// Đường dẫn file lưu trong txtFileIndex
        /// </summary>
        private void OpenExcelAplication()
        {
            if (myExcel.Workbooks.Count == 0)
                myExcel.Workbooks.Open((txtFileIndex.Text.ToString())); // Mở file Excel
            myExcel.DisplayFullScreen = true; // Hiển thị full màn hình
            myExcel.Visible = true;
            GetNameInExcelToProductName(); // Lấy tên mã sản phẩm từ file Excel
        }

        /// <summary>
        /// Khai báo và chạy Thread tổng hợp dữ liệu
        /// </summary>
        private void StartThreadToCollectionData()
        {
            dataThread = new Thread(DataCollection); // chương trình tổng hợp dữ liệu "DataCollection"
            dataThread.Name = "ThData";
            dataThread.IsBackground = true;
            dataThread.Start();
            txtNumberRun.Text = "1";
        }

        /// <summary>
        /// Đặt dataLoging sang OFF và reset Trạng thái hiển thị nút Start
        /// </summary>
        private void StopDataLoggingIfRunning()
        {
            if (dataLogging)
            {
                btnStart.BackColor = Color.White;
                btnStart.ForeColor = Color.Black;
                dataLogging = false;
            }
        }

        /// <summary>
        /// Lấy tên mã sản phẩm từ file Excel =>> lưu vào productName
        /// </summary>
        private void GetNameInExcelToProductName()
        {
            try
            {
                Excel.Worksheet tempWorkSheet = myExcel.ActiveWorkbook.Worksheets[1];
                if (txtNamePos.Text.Length > 1)
                    productName = (tempWorkSheet.Range[txtNamePos.Text].Value2);
                //MessageBox.Show(productName);
            }
            catch (Exception e) { }
        }

        /// <summary>
        /// Khởi tạo về mặc định nếu comfirms
        /// </summary>
        private void IfConfirmResetDataThenResetAllToNull()
        {
            if (MessageBox.Show("Confirm Reset?", "Caption", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                //ResetNumberReadAndReadStatus
                numberRead = 0;
                readLeft = false;
                readRight = false;
                //
                if (myExcel.Workbooks.Count != 0)
                {
                    ResetAllChangeInExcelToNull();
                    MessageOKForm.Show();
                }
            }
        }

        /// <summary>
        /// Tạo lại thư mục lưu file Log theo ngày tháng năm
        /// </summary>
        private void GenerateNewFolderLogFile()
        {
            if (!Directory.Exists(Setting.Default.LogFolderIndex + "\\" + DateTime.Now.ToString("yyyyMMdd")))
                Directory.CreateDirectory(Setting.Default.LogFolderIndex + "\\" + DateTime.Now.ToString("yyyyMMdd"));
        }

        /// <summary>
        /// Xóa hết các dữ liệu đã điền trong file Excel
        /// </summary>
        private void ResetAllChangeInExcelToNull()
        {
            Excel.Range resetRange;
            Excel.Worksheet tempWorkSheet = myExcel.ActiveWorkbook.Worksheets[1];
            // Xóa dữ liệu đã điền
            for (int i = 0; i < 25; i++)
            {
                resetRange = (Excel.Range)tempWorkSheet.Range[tempWorkSheet.Cells[indexRow + convertIndex[i + 1] - 1, indexCol - 2], tempWorkSheet.Cells[indexRow + convertIndex[i + 1] - 1, indexCol + 1]];
                resetRange.Value2 = "";
                resetRange = (Excel.Range)tempWorkSheet.Range[tempWorkSheet.Cells[indexRow + convertIndex[i + 1] - 1, indexCol + 7], tempWorkSheet.Cells[indexRow + convertIndex[i + 1] - 1, indexCol + 10]];
                resetRange.Value2 = "";
                //resetRange = (Excel.Range)tempWorkSheet.Range[tempWorkSheet.Cells[indexRow + convertIndex[i + 1] - 1, indexCol - 2], tempWorkSheet.Cells[indexRow + convertIndex[i + 1] - 1, indexCol - 1]];
                //resetRange.Value2 = "";
            }

            // Xóa dữ liệu điền tay OK
            var tempDelOK = 19;
            for (int i = 0; i < 200; i++)
            {
                try
                {
                    resetRange = tempWorkSheet.Cells[(tempDelOK + i), indexCol + 2];
                    resetRange.Value2 = "";
                }
                catch { }
            }

            // Xóa dữ liệu Check bằng tay
            resetRange = (Excel.Range)tempWorkSheet.Range["T19", "V218"]; // T218
            resetRange.Value2 = "";
            // Xóa dữ liệu độ đảo - Update file Excel mới
            resetRange = (Excel.Range)tempWorkSheet.Range["K19", "M218"];
            resetRange.Value2 = "";
            resetRange = (Excel.Range)tempWorkSheet.Range["R19", "R218"];
            resetRange.Value2 = "";
            // Xóa dữ liệu biểu đồ
            tempWorkSheet = myExcel.ActiveWorkbook.Worksheets[5];
            for (int i = 0; i < 25; i++)
            {
                resetRange = (Excel.Range)tempWorkSheet.Range[tempWorkSheet.Cells[4 + i * 15, 2], tempWorkSheet.Cells[15 + i * 15, 3]];
                resetRange.Value2 = "";
            }
        }

        /// <summary>
        /// Tổng hợp dữ liệu D100. Số đếm numberRead. Xử lý hiển thị Forward, Backward.
        /// </summary>
        private void DataCollection()
        {
            while (true)
            {
                // dataLogging = true sau khi nhấn nút Start, và PLC đang được kết nối
                if (dataLogging)
                {
                    IfCompleteOneReadCyleProcessData(); // Xử lý khi xong 1 chu kỳ kiểm tra
                    IfReadingDataBackward(); // Xử lý nếu bắt đầu kiểm tra chiều thuận
                    IfReadingDataForward(); // Xử lý nếu bắt đầu kiểm tra chiều nghịch
                }
                Thread.Sleep(50);
            }
        }

        /// <summary>
        /// Kiểm tra trạng thái đang lấy dữ liệu chiều Nghịch (bitBackward)
        /// Chạy chu trình lấy dữ liệu, khi hoàn thành thì cập nhật readRight = true
        /// </summary>
        private void IfReadingDataBackward()
        {
            if (bitBackward && !readRight)
            {
                RecordStart();
                if (readDoneConfirmCheckRight() == "True")
                {
                    readRight = true;
                    RecordStopAndSave("Backwward_" + DateTime.Now.ToString("ss"));

                    Invoke(new MethodInvoker(delegate () { MessageOKForm.Show(); }));
                }
            }
        }

        /// <summary>
        /// Kiểm tra trạng thái đang lấy dữ liệu chiều Thuận (bitForward)
        /// Chạy chu trình lấy dữ liệu, khi hoàn thành thì cập nhật readLeft = true
        /// </summary>
        private void IfReadingDataForward()
        {
            if (bitForward && !readLeft)
            {
                RecordStart();
                if (readDoneConfirmCheckLeft() == "True")
                {
                    readLeft = true;
                    RecordStopAndSave("Forward_" + DateTime.Now.ToString("ss"));
                    Invoke(new MethodInvoker(delegate () { MessageOKForm.Show(); }));
                }
            }
        }

        /// <summary>
        /// Thực hiện lấy dữ liệu chiều Nghịch
        /// </summary>
        /// <returns></returns>
        private string readDoneConfirmCheckLeft()
        {
            readDoneConfirm = "False";
            while (bitForward && readDoneConfirm == "False")
            {
                WaitTo4PulseComplete(); // Đợi động cơ quay 4 vòng
                WaitRead20DataToArray(20); // Đợi lấy đủ 20 giá trị
                if (bitForward)
                {
                    UpdateMaxVibration(); // Cập nhật giá trị max theo chiều
                    readDoneConfirm = "True";
                }
                else
                    readDoneConfirm = "False";
            }

            if (readDoneConfirm == "True")
                return ("True");
            else
                return ("False");
        }

        /// <summary>
        /// Thực hiện lấy dữ liệu chiều Thuận
        /// </summary>
        /// <returns></returns>
        private string readDoneConfirmCheckRight()
        {
            readDoneConfirm = "False";
            while (bitBackward && readDoneConfirm == "False")
            {
                WaitTo4PulseComplete(); // Đợi động cơ quay 4 vòng
                WaitRead20DataToArray(20); // Đợi lấy đủ 20 giá trị
                if (bitBackward)
                {
                    UpdateMaxVibration(); // Cập nhật giá trị max theo chiều
                    readDoneConfirm = "True";
                }
                else readDoneConfirm = "False";
            }

            if (readDoneConfirm == "True")
                return ("True");
            else
                return ("False");
        }



        /// <summary>
        /// Chờ đến khi lấy đủ 20 giá trị vào mảng dữ liệu
        /// </summary>
        /// <param name="number"></param>
        private void WaitRead20DataToArray(int number)
        {
            countNumberOfReadData = 0;
            while ((countNumberOfReadData < number)) { }
        }

        /// <summary>
        /// Khi nhấn nút lưu, thì ngay lập tức lưu file Excel theo ngày tháng năm, giờ phút giây
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSaveFile_Click(object sender, EventArgs e)
        {
            try
            {
                myExcel.ActiveWorkbook.SaveCopyAs(@txtLoggingFolder.Text + productName + DateTime.Now.ToString("_yy_MM_dd_HH_mm_ss") + ".xlsm");
                Invoke(new MethodInvoker(delegate () { btnReset.PerformClick(); }));
            }
            catch
            {
                MessageBox.Show("Save Excel File Error!");
            }
        }

        /// <summary>
        /// Hàm xử lí độ đảo đưa vào Excel
        /// </summary>
        /// <param name="x"></param>
        private void InputDodaoToExcel(string x)
        {
            // Nhập giá trị độ đảo vào Excel

            // Xử lý chuỗi độ đảo nhận về
            // Lay gia tri dau tien
            if (x.IndexOf("DT10000") >= 0)
            {
                string temp = x.Substring(x.IndexOf("DT10000"));
                temp = temp.Substring(7, temp.IndexOf("M") - 7);
                float tempF = float.Parse(temp);
                firstString = tempF.ToString("0.00");
            }
            else firstString = "";

            if (x.IndexOf("DT10001") >= 0)
            {
                string temp = x.Substring(x.IndexOf("DT10001"));
                temp = temp.Substring(7, temp.IndexOf("M") - 7);
                float tempF = float.Parse(temp);
                secondString = tempF.ToString("0.00");
            }
            else secondString = "";

            if (x.IndexOf("DT10002") >= 0)
            {
                string temp = x.Substring(x.IndexOf("DT10002"));
                temp = temp.Substring(7, temp.IndexOf("M") - 7);
                float tempF = float.Parse(temp);
                thirdString = tempF.ToString("0.00");
            }
            else thirdString = "";

            UpdateDataToActiveCell("doDao");
        }

        /// <summary>
        /// Cập nhật giá trị lớn nhất theo chiều quay hiện tại
        /// </summary>
        private void UpdateMaxVibration()
        {
            tempDirection = bitBackward ? 1 : 0;
            maxVibrate[numberRead, tempDirection] = 0;
            for (int i = 0; i < 20; i++)
            {
                if (maxVibrate[numberRead, tempDirection] < collectVibrate[numberRead, tempDirection, i])
                    maxVibrate[numberRead, tempDirection] = collectVibrate[numberRead, tempDirection, i];
            }
        }

        /// <summary>
        /// Chờ đến cho đến khi động cơ quay 4 vòng (countPulse - oldCountPulse > 4)
        /// </summary>
        private void WaitTo4PulseComplete()
        {
            int tempPulse = countPulse;
            while ((tempPulse + 4) > countPulse)
            {
                if (bitBackward | bitForward) Invoke(new MethodInvoker(delegate { txtNumberPulse.Text = (countPulse - tempPulse).ToString(); }));
                else tempPulse = countPulse;
                Thread.Sleep(20);
            }
            Invoke(new MethodInvoker(delegate { txtNumberPulse.Text = (countPulse - tempPulse).ToString(); }));
        }

        /// <summary>
        /// Xử lý dữ liệu khi kết thúc 1 chu trình lấy dữ cả 2 chiều thuận và nghịch
        /// </summary>
        private void IfCompleteOneReadCyleProcessData()
        {
            // giá trị readLeft ON sau khi đọc xong chiều Thuận, readRight ON sau khi đọc chiều Nghịch
            // giá trị bitBackward và bitForward ON khi đang lấy dữ liệu theo chiều tương ứng
            // phải đạt đủ điều kiện thì mới coi như kết thúc 1 chu trình lấy dữ liệu
            if (readLeft && readRight && !bitBackward && !bitForward)
            {
                UpdateStatusOfRead(); // Khởi tạo lại các giá trị hiển thị về mặc định, chuẩn bị cho chu trình đọc tiếp theo
                if (!bitManual) // Nếu đang ở chế độ Auto thì cập nhật vào ô tương ứng theo giá trị currentNumberRead
                {
                    ChangeDisplayOfCurrentNumberRead(); // Tăng giá trị numberRead lên 1, cập nhật hiển thị
                    if (chkNoExcel.Checked)
                        UpdateDataToTestler("doRung");
                    else
                        UpdateDataToAutoCellExcel("doRung"); // Update các giá trị vào file Excel

                    SaveFileAndResetIfRaise200(); // Lưu file Excel sau khi đã điền xong hết bảng
                }
                else
                    UpdateDataToActiveCell("doRung"); // Nếu đang ở chế độ Manual thì cập nhật dữ liệu vào ô đang được chọn trên Excel
            }
        }

        /// <summary>
        /// Điền dữ liệu ở chế độ Manual
        /// </summary>
        private void UpdateDataToActiveCell(string options)
        {
            switch (options)
            {
                case "doRung":
                    excelUsing = true;
                    Excel.Worksheet tempWorkSheet = myExcel.ActiveWorkbook.Worksheets[1];
                    try
                    {
                        // Tính toán vị trí ô Excel cần điền dữ liệu - chiều thuận, chiều nghịch
                        Excel.Range tempRangeFW = (Excel.Range)tempWorkSheet.Cells[myExcel.ActiveCell.Row, myExcel.ActiveCell.Column];
                        Excel.Range tempRangeBW = (Excel.Range)tempWorkSheet.Cells[myExcel.ActiveCell.Row, myExcel.ActiveCell.Column + 1];

                        // Nhân giá trị theo tỉ lệ, điền vào ô Excel tương ứng
                        tempRangeFW.Value2 = maxVibrate[numberRead, 0] * 0.0026;
                        tempRangeBW.Value2 = maxVibrate[numberRead, 1] * 0.0026;

                        // Tự động chuyển sang ô nhập dữ liệu độ chụp ảnh
                        (tempWorkSheet.Cells[myExcel.ActiveCell.Row, "R"] as Excel.Range).Select();

                    }
                    catch
                    {
                        MessageBox.Show($"Error Save to Excel. Plese Select Correct Cell!!! Not {myExcel.ActiveCell.Row} {myExcel.ActiveCell.Column}");
                    }
                    excelUsing = false;
                    break;
                case "kheHo":
                    excelUsing = true;
                    tempWorkSheet = myExcel.ActiveWorkbook.Worksheets[1];
                    try
                    {
                        // Tự động chuyển sang ô nhập dữ liệu khe hở
                        (tempWorkSheet.Cells[myExcel.ActiveCell.Row, "O"] as Excel.Range).Select();

                        Excel.Range kheHoRange = (Excel.Range)tempWorkSheet.Cells[myExcel.ActiveCell.Row, myExcel.ActiveCell.Column];
                        kheHoRange.Value2 = stringKhehoTinh;

                        // Tự động chuyển sang ô nhập dữ liệu độ rung
                        (tempWorkSheet.Cells[myExcel.ActiveCell.Row, "P"] as Excel.Range).Select();

                    }
                    catch
                    {
                        MessageBox.Show($"Error Save to Excel. Plese Select Correct Cell!!! Not {myExcel.ActiveCell.Row} {myExcel.ActiveCell.Column}");
                    }
                    excelUsing = false;
                    break;
                case "Capture":
                    excelUsing = true;
                    tempWorkSheet = myExcel.ActiveWorkbook.Worksheets[1];

                    // Tự động chuyển sang ô nhập dữ liệu độ chụp ảnh
                    (tempWorkSheet.Cells[myExcel.ActiveCell.Row, "R"] as Excel.Range).Select();
                    try
                    {
                        Excel.Range captureRange = (Excel.Range)tempWorkSheet.Cells[myExcel.ActiveCell.Row, myExcel.ActiveCell.Column];
                        captureRange.Value2 = imageCaptureLink;

                        // Tự động chuyển sang ô nhập dữ liệu độ đảo
                        (tempWorkSheet.Cells[myExcel.ActiveCell.Row, "T"] as Excel.Range).Select();
                    }
                    catch
                    {
                        MessageBox.Show($"Error Save to Excel. Plese Select Correct Cell!!! Not {myExcel.ActiveCell.Row} {myExcel.ActiveCell.Column}");
                    }
                    excelUsing = false;
                    break;
                case "doDao":
                    excelUsing = true;
                    tempWorkSheet = myExcel.ActiveWorkbook.Worksheets[1];
                    try
                    {
                        // Tính toán vị trí ô Excel cần điền dữ liệu - Vị trí 1, 2, 3
                        Excel.Range tempRangeFirst = (Excel.Range)tempWorkSheet.Cells[myExcel.ActiveCell.Row, myExcel.ActiveCell.Column];
                        Excel.Range tempRangeSecond = (Excel.Range)tempWorkSheet.Cells[myExcel.ActiveCell.Row, myExcel.ActiveCell.Column + 1];
                        Excel.Range tempRangeThird = (Excel.Range)tempWorkSheet.Cells[myExcel.ActiveCell.Row, myExcel.ActiveCell.Column + 2];

                        // Điền vào ô Excel tương ứng
                        tempRangeFirst.Value2 = firstString;
                        tempRangeSecond.Value2 = secondString;
                        tempRangeThird.Value2 = thirdString;
                        //
                        myExcel.ActiveCell.Offset[1, 0].Select();
                    }
                    catch
                    {
                        MessageBox.Show($"Error Save to Excel. Plese Select Correct Cell!!! Not {myExcel.ActiveCell.Row} {myExcel.ActiveCell.Column}");
                    }
                    excelUsing = false;
                    break;
                default:
                    break;
            }

        }

        /// <summary>
        /// Lưu file Excel theo ngày, tháng, năm - Khi đã điền hết dữ liệu vào bảng Excel - tương ứng với numberRead > 24
        /// Sau khi Update, tự nhấn Nút Reset để các giá trị được đặt về mặc định
        /// </summary>
        private void SaveFileAndResetIfRaise200()
        {
            if (numberRead > 24)
            {
                try
                {
                    myExcel.ActiveWorkbook.SaveCopyAs(@txtLoggingFolder.Text + productName + DateTime.Now.ToString("_yy_MM_dd_HH_mm_ss") + ".xlsm");

                    // Điều khiển nút Reset tự động nhấn
                    Invoke(new MethodInvoker(delegate () { btnReset.PerformClick(); }));
                }
                catch
                {
                    MessageBox.Show("Save Excel File Error!");
                }
            }
        }

        /// <summary>
        /// Điền dữ liệu ở chế độ Auto
        /// Điền dữ liệu vào ô tương ứng tiếp theo trong bảng
        /// Điền dữ liệu vào ô giá trị đề hiển thị đồ thị - trong Sheet 5
        /// </summary>
        private void UpdateDataToAutoCellExcel(string options)
        {
            switch (options)
            {
                case "doRung":
                    excelUsing = true;

                    // Điền vào ô Excel theo chiều thuân, nghịch
                    Excel.Worksheet tempWorkSheet = myExcel.ActiveWorkbook.Worksheets[1];
                    Excel.Range forwardRange = (Excel.Range)tempWorkSheet.Cells[indexRow + convertIndex[numberRead] - 1, indexCol];
                    Excel.Range backwardRange = (Excel.Range)tempWorkSheet.Cells[indexRow + convertIndex[numberRead] - 1, indexCol + 1];
                    forwardRange.Value2 = maxVibrate[numberRead - 1, 0] * 0.0026;
                    backwardRange.Value2 = maxVibrate[numberRead - 1, 1] * 0.0026;

                    // Điền vào bảng giá trị hiển thị đồ thị
                    tempWorkSheet = myExcel.ActiveWorkbook.Worksheets[5];
                    for (int i = 0; i < 10; i++)
                    {
                        forwardRange = (Excel.Range)tempWorkSheet.Cells[EXCEL_CHART_START_ROW + (numberRead - 1) * 15 + i, EXCEL_CHART_START_COLUUM];
                        backwardRange = (Excel.Range)tempWorkSheet.Cells[EXCEL_CHART_START_ROW + (numberRead - 1) * 15 + i, EXCEL_CHART_START_COLUUM + 1];
                        forwardRange.Value2 = collectVibrate[numberRead - 1, 0, i] * 0.0026;
                        backwardRange.Value2 = collectVibrate[numberRead - 1, 1, i] * 0.0026;
                    }

                    // Chuyển ActiveCell về vị trí đo độ đảo
                    tempWorkSheet = myExcel.ActiveWorkbook.Worksheets[1];
                    tempWorkSheet.Select();
                    (tempWorkSheet.Cells[indexRow + convertIndex[numberRead] - 1, "K"] as Excel.Range).Select();

                    excelUsing = false;
                    break;
                case "Capture": // Điền link ảnh vào vị trí sau vị trí lưu độ rung 2 ô
                    tempWorkSheet = myExcel.ActiveWorkbook.Worksheets[1];
                    Excel.Range captureRange = (Excel.Range)tempWorkSheet.Cells[indexRow + convertIndex[numberRead + 1] - 1, indexCol + 2];
                    captureRange.Value2 = imageCaptureLink;

                    // Chuyển ActiveCell về vị trí đo độ đảo
                    tempWorkSheet = myExcel.ActiveWorkbook.Worksheets[1];
                    tempWorkSheet.Select();
                    (tempWorkSheet.Cells[indexRow + convertIndex[numberRead], "K"] as Excel.Range).Select();
                    break;
                case "kheHo":
                    tempWorkSheet = myExcel.ActiveWorkbook.Worksheets[1];
                    Excel.Range kheHoRange = (Excel.Range)tempWorkSheet.Cells[indexRow + convertIndex[numberRead + 1] - 1, indexCol - 1];
                    kheHoRange.Value2 = stringKhehoTinh;
                    break;
                case "doDao":
                    excelUsing = true;
                    tempWorkSheet = myExcel.ActiveWorkbook.Worksheets[1];
                    try
                    {
                        // Tính toán vị trí ô Excel cần điền dữ liệu - Vị trí 1, 2, 3
                        Excel.Range tempRangeFirst = (Excel.Range)tempWorkSheet.Cells[myExcel.ActiveCell.Row, myExcel.ActiveCell.Column];
                        Excel.Range tempRangeSecond = (Excel.Range)tempWorkSheet.Cells[myExcel.ActiveCell.Row, myExcel.ActiveCell.Column + 1];
                        Excel.Range tempRangeThird = (Excel.Range)tempWorkSheet.Cells[myExcel.ActiveCell.Row, myExcel.ActiveCell.Column + 2];

                        // Điền vào ô Excel tương ứng
                        tempRangeFirst.Value2 = firstString;
                        tempRangeSecond.Value2 = secondString;
                        tempRangeThird.Value2 = thirdString;

                        myExcel.ActiveCell.Offset[1, 0].Select();
                        //(tempWorkSheet.Cells[indexRow + convertIndex[numberRead] + 1, "E"] as Excel.Range).Select();
                        numberRead += 1;
                    }
                    catch
                    {
                        MessageBox.Show($"Error Save to Excel. Plese Select Correct Cell!!! Not {myExcel.ActiveCell.Row} {myExcel.ActiveCell.Column}");
                    }
                    excelUsing = false;
                    //try
                    //{
                    //    Invoke(new MethodInvoker(delegate ()
                    //    {
                    //        System.Windows.Forms.Clipboard.Clear();  // Always clear the clipboard first
                    //        System.Windows.Forms.Clipboard.SetText(firstString);
                    //        System.Windows.Forms.SendKeys.SendWait(" ");
                    //        System.Windows.Forms.SendKeys.SendWait("^v");// Paste
                    //        Thread.Sleep(20);
                    //        System.Windows.Forms.SendKeys.SendWait("{TAB}");

                    //        System.Windows.Forms.Clipboard.SetText(secondString);
                    //        System.Windows.Forms.SendKeys.SendWait(" ");
                    //        System.Windows.Forms.SendKeys.SendWait("^v");// Paste
                    //        Thread.Sleep(20);
                    //        System.Windows.Forms.SendKeys.SendWait("{TAB}");

                    //        System.Windows.Forms.Clipboard.SetText(thirdString);
                    //        System.Windows.Forms.SendKeys.SendWait(" ");
                    //        System.Windows.Forms.SendKeys.SendWait("^v");// Paste
                    //        Thread.Sleep(20);
                    //        System.Windows.Forms.SendKeys.SendWait("{TAB}");

                    //    }));

                    //}
                    //catch (Exception ex)
                    //{
                    //    MessageBox.Show(ex.Message);
                    //}
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Điền dữ liệu chế độ Auto vào phần mềm Testler
        /// </summary>
        /// <param name="options"></param>
        void UpdateDataToTestler(string options)
        {
            switch (options)
            {
                case "doRung":
                    try
                    {
                        Invoke(new MethodInvoker(delegate ()
                        {
                            System.Windows.Forms.Clipboard.Clear();  // Always clear the clipboard first
                            string stringWrite_1 = Convert.ToString(maxVibrate[numberRead - 1, 0] * 0.0026);
                            string stringWrite_2 = Convert.ToString(maxVibrate[numberRead - 1, 1] * 0.0026);
                            System.Windows.Forms.Clipboard.SetText(stringWrite_1);
                            System.Windows.Forms.SendKeys.SendWait(" ");
                            System.Windows.Forms.SendKeys.SendWait("^v");// Paste
                            Thread.Sleep(30);
                            System.Windows.Forms.SendKeys.SendWait("{ENTER}");

                            System.Windows.Forms.Clipboard.SetText(stringWrite_2);
                            System.Windows.Forms.SendKeys.SendWait(" ");
                            System.Windows.Forms.SendKeys.SendWait("^v");  // Paste
                            Thread.Sleep(30);
                            System.Windows.Forms.SendKeys.SendWait("{ENTER}");
                        }));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                    break;
                case "Capture":
                    try
                    {
                        Invoke(new MethodInvoker(delegate ()
                        {
                            System.Windows.Forms.Clipboard.Clear();  // Always clear the clipboard first
                            string stringCapture = imageCaptureLink;
                            System.Windows.Forms.Clipboard.SetText(stringCapture);
                            System.Windows.Forms.SendKeys.SendWait(" ");
                            System.Windows.Forms.SendKeys.SendWait("^v");// Paste
                            Thread.Sleep(20);
                            System.Windows.Forms.SendKeys.SendWait("{ENTER}");
                        }));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                    break;
                case "kheHo":
                    try
                    {
                        Invoke(new MethodInvoker(delegate ()
                        {
                            System.Windows.Forms.Clipboard.Clear();  // Always clear the clipboard first
                            string stringKheHo = stringKhehoTinh;
                            System.Windows.Forms.Clipboard.SetText(stringKheHo);
                            System.Windows.Forms.SendKeys.SendWait(" ");
                            System.Windows.Forms.SendKeys.SendWait("^v");// Paste
                            Thread.Sleep(30);
                            System.Windows.Forms.SendKeys.SendWait("{ENTER}");
                        }));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                    break;
                case "doDao":
                    try
                    {
                        Invoke(new MethodInvoker(delegate ()
                        {
                            System.Windows.Forms.Clipboard.Clear();  // Always clear the clipboard first
                            System.Windows.Forms.Clipboard.SetText(firstString);
                            System.Windows.Forms.SendKeys.SendWait(" ");
                            System.Windows.Forms.SendKeys.SendWait("^v");// Paste
                            Thread.Sleep(20);
                            System.Windows.Forms.SendKeys.SendWait("{ENTER}");

                            System.Windows.Forms.Clipboard.SetText(secondString);
                            System.Windows.Forms.SendKeys.SendWait(" ");
                            System.Windows.Forms.SendKeys.SendWait("^v");// Paste
                            Thread.Sleep(20);
                            System.Windows.Forms.SendKeys.SendWait("{ENTER}");

                            System.Windows.Forms.Clipboard.SetText(thirdString);
                            System.Windows.Forms.SendKeys.SendWait(" ");
                            System.Windows.Forms.SendKeys.SendWait("^v");// Paste
                            Thread.Sleep(20);
                            System.Windows.Forms.SendKeys.SendWait("{ENTER}");

                        }));

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Cập nhật giá trị kiểm tra hiện tại lên giao diện
        /// </summary>
        private void ChangeDisplayOfCurrentNumberRead()
        {
            if (InvokeRequired) Invoke(new MethodInvoker(delegate { txtNumberRun.Text = (convertIndex[numberRead + 1]).ToString(); }));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!currentPLCBit["X11"]) plcFX3G.SetDevice("X11", 1);
            else plcFX3G.SetDevice("X11", 0);
        }

        private void chkNoExcel_CheckedChanged(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Tăng numberRead lên 1 để nhảy đến ô lấy dữ liệu tiếp theo/ trừ khi đang ở chế độ Manual
        /// Khởi tạo lại các giá trị (đã đọc) về OFF
        /// </summary>
        private void UpdateStatusOfRead()
        {
            if (!bitManual)
                numberRead += 1;
            readRight = false;
            readLeft = false;
            btnLampF.BackColor = Color.Transparent;
            btnLampB.BackColor = Color.Transparent;
        }

        /// <summary>
        /// Hàm xử lí đưa giá trị khe hở vào Testler hoặc excel
        /// </summary>
        /// <param name="x"></param>
        private void InputKhehoToTestlerOrExcel(string x)
        {
            // Nhập giá trị khe hở vào ô Excel tương ứng
            stringKhehoTinh = x;
            if (!bitManual)
            {

                if (chkNoExcel.Checked)
                    UpdateDataToTestler("kheHo");
                else
                    UpdateDataToAutoCellExcel("kheHo");

            }
            else
            {
                //UpdateDataToActiveCell("kheHo");
            }
        }

        /// <summary>
        /// Hàm xử lí đưa giá trị độ đảo vào Testler hoặc Excel xử dụng đồng hồ Sylvac
        /// </summary>
        /// <param name="dd"></param>
        void InputDoDaoToTesler(string dd1, string dd2, string dd3)
        {
            firstString = dd1;
            secondString = dd2;
            thirdString = dd3;

            if (!bitManual)
            {
                if (chkNoExcel.Checked)
                    UpdateDataToTestler("doDao");
                else
                    UpdateDataToAutoCellExcel("doDao");
            }
            else
            {

            }
        }

        #endregion


        #region<region> nút nhấn Test chương trình, đã làm ẩn đi
        private void chkTest02_CheckedChanged(object sender, EventArgs e)
        {
            // Change Color of Button Forward Lamp if ReadDone Or Not
            if (chkTestForward.Checked) btnLampF.BackColor = Color.MidnightBlue;
            else if (!readLeft)
                btnLampF.BackColor = Color.Transparent;
        }

        private void chkTest01_CheckedChanged(object sender, EventArgs e)
        {
            // Change Color of Button Backward Lamp if ReadDone Or Not
            if (chkTestBackward.Checked) btnLampB.BackColor = Color.MidnightBlue;
            else if (!readRight)
                btnLampB.BackColor = Color.Transparent;
        }

        #endregion


        private void btnTest001_Click(object sender, EventArgs e)
        {
            //Task.Delay(100);
            //Console.WriteLine("Nhan nut X5!");
            //wfKheho wftemp = new wfKheho(ref plcFX3G, ref COMSylvac);
            //wftemp.stringDoneKheho += InputKhehoToExcel;
            //wftemp.ShowDialog();
            //wftemp.Dispose();

            // Test
            if (!currentPLCBit["X11"]) plcFX3G.SetDevice("X11", 1);
            else plcFX3G.SetDevice("X11", 0);
        }



        private void btnTest002_Click(object sender, EventArgs e)
        {
            //await Task.Delay(100);
            //Console.WriteLine("Nhan nut X6!");
            //wfDodao wftemp1 = new wfDodao();
            //wftemp1.stringDoneDodao += InputDodaoToExcel;
            //wftemp1.ShowDialog();

            // Test
            if (!currentPLCBit["X12"]) plcFX3G.SetDevice("X12", 1);
            else plcFX3G.SetDevice("X12", 0);
        }

        private async void btnTest003_Click(object sender, EventArgs e)
        {
            await Task.Delay(100);
            if (!bitCaptureOpen)
            {
                Capture wftemp2 = new Capture(ref plcFX3G);
                wftemp2.Show();
                Console.WriteLine("Nhan nut X7!");
            }
        }

        private void btnTest004_Click(object sender, EventArgs e)
        {

        }


    }
}
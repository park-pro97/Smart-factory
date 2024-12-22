// 최종
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Data.SqlClient;
using static MesWinForm.ReportStructs;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Security.Cryptography;
using static System.ComponentModel.Design.ObjectSelectorEditor;
using Microsoft.VisualBasic;
using System.CodeDom;
using static System.Net.Mime.MediaTypeNames;
using System.Data;
using MesWinForm;

namespace MesWinForm
{
    public partial class Form1 : Form
    {
        private SqlConnection dbConnection;
        private string connectionString = "Server=DESKTOP-SBKK1SS\\SQLEXPRESS;Database=mes;Integrated Security=True;";

        private Socket clientSocket;
        private Thread receiveThread;
        private Thread MainThread;
        private bool isReceiving = false;
        private bool bMainThreadChk = false;
        private bool isQueryMode = false;
        private System.Windows.Forms.Timer updateTimer;
        private string logFilePath;

        private Queue<string> recvReportIDequeue = new Queue<string>();
        private string textBox2Text = string.Empty;
        private string logBuffer = string.Empty;


        public Form1()
        {
            InitializeComponent();
            InitializeSocket();
            InitializeTimer();
        }

        // 1. 초기화 메서드
        private void InitializeSocket()
        {
            try
            {
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("192.168.0.67"), 13000);
                clientSocket.Connect(endPoint);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 소켓 연결 실패");
            }
        }

        private void InitializeTimer()
        {
            updateTimer = new System.Windows.Forms.Timer();
            updateTimer.Interval = 1000; // 1초마다 갱신
            //updateTimer.Tick += (s, e) => RefreshUnifiedReport();
            updateTimer.Start();


        }
        //private void RefreshUnifiedReport()
        //{
        //    if (isQueryMode) return; // 조회 모드일 경우 실시간 갱신 중단

        //    try
        //    {
        //        string query = "SELECT * FROM CompleteReport";
        //        SqlDataAdapter dataAdapter = new SqlDataAdapter(query, dbConnection);
        //        System.Data.DataTable dataTable = new System.Data.DataTable();
        //        dataAdapter.Fill(dataTable);

        //        // DataGridView 업데이트
        //        dataGridView1.DataSource = dataTable;
        //    }
        //    catch (Exception ex)
        //    {
        //        //updateLog("실시간 갱신 실패\r\n");
        //        //textBoxText = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 실시간 갱신 실패\r\n";
        //    }
        //}

        //private void timer2_Tick(object sender, EventArgs e)
        //{
        //    if (!isQueryMode)
        //    {
        //        try
        //        {
        //            string query = "SELECT * FROM CompleteReport";
        //            SqlDataAdapter dataAdapter = new SqlDataAdapter(query, dbConnection);
        //            System.Data.DataTable dataTable = new System.Data.DataTable();
        //            dataAdapter.Fill(dataTable);
        //            dataGridView1.DataSource = dataTable;
        //        }
        //        catch (Exception ex)
        //        {

        //        }
        //    }
        //}

        // 2. 폼 로드 및 종료
        private void Form1_Load_1(object sender, EventArgs e)
        {
            // 로그 파일을 저장할 디렉토리 경로
            string logDirectory = @"C:\Users\Admin\source\repos\MesWinForm\MesWinForm\Log";

            // 디렉토리가 없으면 생성
            if (!System.IO.Directory.Exists(logDirectory))
            {
                System.IO.Directory.CreateDirectory(logDirectory);
            }

            // 새로운 로그 파일 경로 생성 (현재 날짜 및 시간을 기반으로 파일명 생성)
            logFilePath = System.IO.Path.Combine(logDirectory, $"Log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

            // 로그 파일에 초기 내용 기록
            System.IO.File.AppendAllText(logFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 로그 시작\r\n", Encoding.UTF8);

            try
            {
                dbConnection = new SqlConnection(connectionString);
                dbConnection.Open();

                bMainThreadChk = true;
                MainThread = new Thread(MainSequenceThread);
                MainThread.Start();

                isReceiving = true;
                receiveThread = new Thread(ReceiveData);
                receiveThread.Start();

                updateTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] DB 연결 실패: {ex.Message}");
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveLogToFile();
            if (dbConnection != null && dbConnection.State == System.Data.ConnectionState.Open)
            {
                dbConnection.Close();
            }

            if (clientSocket != null && clientSocket.Connected)
            {
                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();
            }

            if (receiveThread != null && receiveThread.IsAlive)
            {
                isReceiving = false;
                receiveThread.Join();
                receiveThread = null;
            }

            if (MainThread != null && MainThread.IsAlive)
            {
                bMainThreadChk = false;
                MainThread.Join();
                MainThread = null;
            }

        }
        private void ReconnectSocket()
        {
            if (clientSocket != null)
            {
                try
                {
                    if (clientSocket.Connected)
                    {
                        clientSocket.Shutdown(SocketShutdown.Both);
                    }
                }
                catch (SocketException) { }
                finally
                {
                    clientSocket.Close();
                    clientSocket = null;
                }
            }

            Thread.Sleep(1000);
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.Connect(new IPEndPoint(IPAddress.Parse("192.168.46.103"), 13000));
        }

        // 3. 데이터 수신 및 처리
        private void ReceiveData()
        {
            while (isReceiving)
            {
                try
                {
                    if (clientSocket != null && clientSocket.Connected && IsSocketConnected(clientSocket))
                    {
                        byte[] buffer = new byte[1024]; // 메시지 버퍼
                        int bytesRead = clientSocket.Receive(buffer); // 데이터 수신
                        string data = Encoding.Default.GetString(buffer, 0, bytesRead).Trim(); // 문자열 변환

                        if (!string.IsNullOrEmpty(data))
                        {
                            // 기존 데이터 처리
                            ProcessReceivedData(data);
                        }
                    }
                    else
                    {
                        ReconnectSocket(); // 연결이 끊어진 경우 재연결 시도
                    }
                }
                catch (Exception ex)
                {
                    updateLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 소켓 오류 발생\r\n");
                    textBox2Text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 소켓 오류 발생\r\n";
                }
                Thread.Sleep(100);
            }
        }
        private bool IsSocketConnected(Socket socket)
        {
            try
            {
                // Poll 메서드로 소켓 상태를 확인
                return !(socket.Poll(1000, SelectMode.SelectRead) && socket.Available == 0);
            }
            catch (SocketException)
            {
                return false; // Poll 중 오류 발생 시 연결이 유효하지 않다고 판단
            }
        }

        private static readonly object logLock = new object(); // 동기화 객체

        private void updateLog(string msg)
        {
            lock (logLock) // 쓰레드 간 동기화
            {
                try
                {
                    // 디렉토리 및 파일 확인 후 없으면 생성
                    string directoryPath = System.IO.Path.GetDirectoryName(logFilePath);
                    if (!string.IsNullOrEmpty(directoryPath) && !System.IO.Directory.Exists(directoryPath))
                    {
                        System.IO.Directory.CreateDirectory(directoryPath);
                    }

                    // FileStream과 StreamWriter를 사용하여 로그 작성
                    using (var fileStream = new System.IO.FileStream(logFilePath, System.IO.FileMode.Append, System.IO.FileAccess.Write, System.IO.FileShare.Read))
                    using (var writer = new System.IO.StreamWriter(fileStream, Encoding.UTF8))
                    {
                        writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {msg}");
                    }
                }
                catch (Exception ex)
                {
                    // 로그 기록 실패 시 예외 처리
                    System.Windows.Forms.MessageBox.Show($"로그 기록 실패: {ex.Message}");
                }
            }
        }


        private void ProcessReceivedData(string data)
        {
            // 텍스트박스에 수신 데이터 출력 (중복 방지)
            if (textBox2.InvokeRequired)
            {
                // UI 스레드에서 안전하게 실행
                textBox2.Invoke(new Action(() =>
                {
                    textBox2.AppendText($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {data}\r\n");
                }));
            }
            else
            {
                // 직접 출력
                textBox2.AppendText($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {data}\r\n");
            }

            // 데이터를 파싱하여 처리 (출력 이후 처리)
            ParsingRecvData(data);
        }



        // 받은 데이터를 구조체로 변환하는 메서드
        //private object TransformData(string[] parts)
        //{
        //    int reportID = Convert.ToInt32(parts[0].Substring(1, parts[0].Length - 1)); // 첫 번째 부분에서 ReportID를 추출
        //    int index = 1; // 배열의 나머지 부분을 처리하기 위해 인덱스 초기화

        //    switch (reportID)
        //    {
        //        case ReportIDList.IDReport:
        //            return new IDReport
        //            {
        //                Datetime = DateTime.Now,
        //                ReportID = parts[index++],
        //                ReportName = parts[index++],
        //                ModelID = parts[index++],
        //                OPID = parts[index++],
        //                ProcID = parts[index++],
        //                MaterialID = parts[index++]
        //            };

        //        case ReportIDList.StartedReport:
        //            return new StartedReport
        //            {
        //                Datetime = DateTime.Now,
        //                ReportID = parts[index++],
        //                ReportName = parts[index++],
        //                ModelID = parts[index++],
        //                OPID = parts[index++],
        //                ProcID = parts[index++],
        //                MaterialID = parts[index++],
        //                LotID = parts[index++]
        //            };

        //        case ReportIDList.CanceledReport:
        //            return new CanceledReport
        //            {
        //                Datetime = DateTime.Now,
        //                ReportID = parts[index++],
        //                ReportName = parts[index++],
        //                ModelID = parts[index++],
        //                OPID = parts[index++],
        //                ProcID = parts[index++],
        //                MaterialID = parts[index++],
        //                LotID = parts[index++]
        //            };

        //        case ReportIDList.CompleteReport:
        //            return new CompletedReport
        //            {
        //                Datetime = DateTime.Now,
        //                reportID = parts[index++],
        //                reportName = parts[index++],
        //                ModelID = parts[index++],
        //                OPID = parts[index++],
        //                ProcID = parts[index++],
        //                MaterialID = parts[index++],
        //                LotID = parts[index++],
        //                Sen1 = Convert.ToDouble(parts[index++]),
        //                Sen2 = Convert.ToDouble(parts[index++]),
        //                Sen3 = Convert.ToDouble(parts[index++]),
        //                Jud1 = parts[index++],
        //                Jud2 = parts[index++],
        //                Jud3 = parts[index++],
        //                TotalJud = parts[index++]
        //            };

        //        default:
        //            updateLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 알 수 없는 ReportID: {reportID}\r\n");
        //            return null; // 알 수 없는 ReportID는 null 반환
        //    }
        //}

        private object ParsingRecvData(string p_data)
        {
            int recvReportID = new int();
            int index = 0;
            string[] parts = new string[] { };

            try
            {
                // 데이터가 예상한 포맷인지 확인 (예: "_10701/Wafer/...") 
                if (p_data != "")
                {
                    parts = p_data.Split('/');

                    recvReportID = Convert.ToInt32(parts[index].Substring(1, parts[index++].Length - 1));
                }

                if (recvReportID == ReportIDList.onlineRemote)
                {
                    Config.onlineStatus = true;
                }

                if (!Config.onlineStatus)
                {
                    return false;
                }

                switch (recvReportID)
                {
                    case ReportIDList.offlineChange:
                        //textBox2.AppendText($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {p_data}\r\n");
                        break;

                    case ReportIDList.onlineRemote:
                        
                        //textBox2.AppendText($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {p_data}\r\n");

                        break;

                    case ReportIDList.IDReport:
                        IDReport tempIDReport = new IDReport();
                        tempIDReport.Datetime = DateTime.Now;
                        tempIDReport.ReportID = recvReportID.ToString();
                        tempIDReport.ReportName = "IDReport";
                        tempIDReport.ModelID = parts[index++];
                        tempIDReport.OPID = parts[index++];
                        tempIDReport.ProcID = parts[index++];
                        tempIDReport.MaterialID = parts[index++];

                        InsertToDatabase(tempIDReport);

                        Report.IDReport.Enqueue(tempIDReport);
                        recvReportIDequeue.Enqueue(tempIDReport.ReportID);

                        return tempIDReport;

                    case ReportIDList.StartedReport:
                        StartedReport tempStartedReport = new StartedReport();
                        tempStartedReport.Datetime = DateTime.Now;
                        tempStartedReport.ReportID = recvReportID.ToString();
                        tempStartedReport.ReportName = "StartedReport";
                        tempStartedReport.ModelID = parts[index++];
                        tempStartedReport.OPID = parts[index++];
                        tempStartedReport.ProcID = parts[index++];
                        tempStartedReport.MaterialID = parts[index++];
                        tempStartedReport.LotID = parts[index++];

                        InsertToDatabase(tempStartedReport);

                        Report.startedReport.Enqueue(tempStartedReport);
                        recvReportIDequeue.Enqueue(tempStartedReport.ReportID);

                        return tempStartedReport;

                    case ReportIDList.CanceledReport:
                        CanceledReport tempCanceledReport = new CanceledReport();
                        tempCanceledReport.Datetime = DateTime.Now;
                        tempCanceledReport.ReportID = recvReportID.ToString();
                        tempCanceledReport.ReportName = "CanceledReport";
                        tempCanceledReport.ModelID = parts[index++];
                        tempCanceledReport.OPID = parts[index++];
                        tempCanceledReport.ProcID = parts[index++];
                        tempCanceledReport.MaterialID = parts[index++];
                        tempCanceledReport.LotID = parts[index++];

                        InsertToDatabase(tempCanceledReport);

                        Report.canceledReport.Enqueue(tempCanceledReport);
                        recvReportIDequeue.Enqueue(tempCanceledReport.ReportID);

                        return tempCanceledReport;

                    case ReportIDList.CompleteReport:
                        CompletedReport completedReport = new CompletedReport();
                        completedReport.Datetime = DateTime.Now;
                        completedReport.reportID = recvReportID.ToString();
                        completedReport.reportName = "CompleteReport";
                        completedReport.ModelID = parts[index++];
                        completedReport.OPID = parts[index++];
                        completedReport.ProcID = parts[index++];
                        completedReport.MaterialID = parts[index++];
                        completedReport.LotID = parts[index++];
                        completedReport.Sen1 = Convert.ToDouble(parts[index++]);
                        completedReport.Sen2 = Convert.ToDouble(parts[index++]);
                        completedReport.Sen3 = Convert.ToDouble(parts[index++]);
                        completedReport.Jud1 = parts[index++];
                        completedReport.Jud2 = parts[index++];
                        completedReport.Jud3 = parts[index++];
                        completedReport.TotalJud = parts[index++];

                        InsertToDatabase(completedReport);

                        Report.completedReport.Enqueue(completedReport);
                        recvReportIDequeue.Enqueue(completedReport.reportID);

                        return completedReport;

                    case ReportIDList.ModelChangeStateChangedReport:
                        ModelChangeStateChangedReport modelChangeStateChangedReport = new ModelChangeStateChangedReport();
                        modelChangeStateChangedReport.reportID = recvReportID.ToString();
                        modelChangeStateChangedReport.reportName = "ModelChangeStateChangedReport";
                        modelChangeStateChangedReport.ModelID = parts[index++];
                        modelChangeStateChangedReport.OPID = parts[index++];
                        modelChangeStateChangedReport.ProcID = parts[index++];

                        Report.modelChangeStateChangedReports.Enqueue(modelChangeStateChangedReport);
                        recvReportIDequeue.Enqueue(modelChangeStateChangedReport.reportID);

                        break;

                    case ReportIDList.ModelChangeCanceledReport:
                        ModelChangeCanceledReport modelChangeCanceledReport = new ModelChangeCanceledReport();
                        modelChangeCanceledReport.reportID = recvReportID.ToString();
                        modelChangeCanceledReport.reportName = "ModelChangeCanceledReport";
                        modelChangeCanceledReport.ModelID = parts[index++];
                        modelChangeCanceledReport.OPID = parts[index++];
                        modelChangeCanceledReport.ProcID = parts[index++];

                        Report.modelChangeCanceledReports.Enqueue(modelChangeCanceledReport);
                        recvReportIDequeue.Enqueue(modelChangeCanceledReport.reportID);

                        break;

                    case ReportIDList.ModelChangeCompletedReport:
                        ModelChangeCompletedReport modelChangeCompletedReport = new ModelChangeCompletedReport();
                        modelChangeCompletedReport.reportID = recvReportID.ToString();
                        modelChangeCompletedReport.reportName = "ModelChangeCompletedReport";
                        modelChangeCompletedReport.ModelID = parts[index++];
                        modelChangeCompletedReport.OPID = parts[index++];
                        modelChangeCompletedReport.ProcID = parts[index++];

                        Report.modelChangeCompletedReports.Enqueue(modelChangeCompletedReport);
                        recvReportIDequeue.Enqueue(modelChangeCompletedReport.reportID);

                        break;

                    case ReportIDList.ModelListReport:
                        ModelListReport modelListReport = new ModelListReport();
                        modelListReport.reportID = recvReportID.ToString();
                        modelListReport.reportName = "ModelListReport";

                        Config.modelList.Clear();
                        index = 1;
                        for (int i = 1; i < parts.Length; i++)
                        {
                            if (index < parts.Length)
                            {
                                Config.modelList.Add(parts[index++]);
                            }
                        }

                        break;

                    default:
                        updateLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {recvReportID}\r\n");
                        //textBoxText = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {recvReportID}\r\n");
                        break;
                }

                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        // DB에 데이터를 삽입하는 메서드
        private void InsertToDatabase(object reportData)
        {
            try
            {
                // 1. 구조체 또는 클래스 이름에 따라 테이블 이름 매핑
                string tableName = reportData switch
                {
                    IDReport => "IDReport",
                    StartedReport => "StartedReport",
                    CanceledReport => "CanceledReport",
                    CompletedReport => "CompleteReport",
                    _ => throw new Exception($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 알 수 없는 데이터 타입")
                };

                // 2. SQL 컬럼 및 파라미터 생성
                var columns = new List<string>();
                var parameters = new List<string>();

                // 구조체 또는 클래스 필드 가져오기
                var fields = reportData.GetType().GetFields(); // 구조체에서 필드 추출
                foreach (var field in fields)
                {
                    columns.Add(field.Name);
                    parameters.Add($"@{field.Name}");
                }

                // 3. SQL 쿼리 문자열 생성
                string query = $"INSERT INTO {tableName} ({string.Join(", ", columns)}) " +
                               $"VALUES ({string.Join(", ", parameters)})";

                // 4. SQL 커맨드 실행
                using (SqlCommand command = new SqlCommand(query, dbConnection))
                {
                    foreach (var field in fields)
                    {
                        var value = field.GetValue(reportData) ?? DBNull.Value; // NULL 값 처리
                        command.Parameters.AddWithValue($"@{field.Name}", value);
                    }

                    command.ExecuteNonQuery();
                    updateLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {tableName} DB 삽입 성공: {reportData.GetType().Name}\r\n");
                    textBox2Text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] DB 삽입 성공\r\n";

                }
            }
            catch (Exception ex)
            {
                updateLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] DB 삽입 실패\r\n");
                textBox2Text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] DB 삽입 실패\r\n";
            }
        }

        // 4. 버튼 이벤트
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                string baseQuery = "SELECT * FROM CompleteReport";
                List<string> conditions = new List<string>();

                // 1. 날짜 범위 조건 추가
                // 시간까지 포함된 날짜가 아니라 날짜만 비교
                DateTime startDate = dateTimePicker1.Value.Date; // 시작 날짜의 시간은 00:00:00
                DateTime endDate = dateTimePicker2.Value.Date;   // 종료 날짜의 시간은 00:00:00

                // 사용자가 시작 날짜를 선택하지 않은 경우, 기본적으로 오늘 날짜로 설정
                if (dateTimePicker1.Value == dateTimePicker1.MinDate)
                {
                    startDate = DateTime.Today;
                    dateTimePicker1.Value = startDate;
                }

                // 사용자가 종료 날짜를 선택하지 않은 경우, 기본적으로 오늘 날짜로 설정
                if (dateTimePicker2.Value == dateTimePicker2.MinDate)
                {
                    endDate = DateTime.Today;
                    dateTimePicker2.Value = endDate;
                }

                // 날짜 범위 조건을 시간까지 고려하여 수정
                string startDateTime = startDate.ToString("yyyy-MM-dd 00:00:00");
                string endDateTime = endDate.ToString("yyyy-MM-dd 23:59:59");

                // 조건 추가
                conditions.Add($"DateTime BETWEEN '{startDateTime}' AND '{endDateTime}'");

                // 2. MaterialID 조건 추가
                if (!string.IsNullOrEmpty(textBox1.Text))
                {
                    string materialID = textBox1.Text.Trim();
                    conditions.Add($"MaterialID = '{materialID}'");
                }

                // 3. 불량 조건 추가 (checkBox1이 체크되면 NG인 데이터 조회)
                if (checkBox1.Checked)
                {
                    conditions.Add("TotalJud = 'NG'");  // 'NG'는 불량
                }

                // 4. 정상 조건 추가 (checkBox2가 체크되면 OK인 데이터 조회)
                if (checkBox2.Checked)
                {
                    conditions.Add("TotalJud = 'OK'");  // 'OK'는 정상
                }

                // 5. 불량과 정상 모두 조회하고자 할 경우 OR로 처리
                if (checkBox1.Checked && checkBox2.Checked)
                {
                    // 'NG'와 'OK'를 OR 조건으로 조회
                    string conditionForBoth = "TotalJud = 'NG' OR TotalJud = 'OK'";
                    conditions.Add(conditionForBoth);
                }

                // 조건 조합
                string query = baseQuery;
                if (conditions.Count > 0)
                {
                    query += " WHERE " + string.Join(" AND ", conditions);
                }

                // 쿼리 문자열을 출력하여 확인
                //MessageBox.Show($"실행되는 쿼리: {query}");

                // 쿼리 실행
                SqlDataAdapter dataAdapter = new SqlDataAdapter(query, dbConnection);
                System.Data.DataTable dataTable = new System.Data.DataTable();
                dataAdapter.Fill(dataTable);

                // 데이터가 없으면 메시지 출력
                if (dataTable.Rows.Count == 0)
                {
                    MessageBox.Show("조회된 데이터가 없습니다.");
                }

                // 결과를 DataGridView에 바인딩
                dataGridView1.DataSource = null;
                dataGridView1.DataSource = dataTable;

                // 로그 출력
                updateLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 조회 성공\r\n");
                textBox2Text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 조회 성공\r\n";

            }
            catch (Exception ex)
            {
                updateLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 조회 실패\r\n");
                textBox2Text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 조회 실패\r\n";
            }
        }



        private void button2_Click(object sender, EventArgs e)
        {
            // 그리드뷰를 초기화하여 빈 화면으로 바꿔줍니다.
            dataGridView1.DataSource = null; // 데이터 소스를 null로 설정하여 빈 화면으로 변경

            // DateTimePicker와 다른 컨트롤들을 초기 상태로 되돌립니다.
            dateTimePicker1.Value = DateTime.Now; // 기본값으로 현재 날짜와 시간 설정 (원하는 초기값으로 설정 가능)
            dateTimePicker2.Value = DateTime.Now; // 기본값으로 현재 날짜와 시간 설정 (원하는 초기값으로 설정 가능)
            checkBox1.Checked = false; // 체크박스를 초기 상태로 되돌리기 (체크 안됨)
            checkBox2.Checked = false; // 체크박스를 초기 상태로 되돌리기 (체크 안됨)
            textBox1.Text = string.Empty; // 텍스트박스를 빈 문자열로 초기화

            // 추가적으로 필요할 경우 다른 UI 컨트롤들도 초기화해줄 수 있습니다.

            // 상태 텍스트 출력 (로그 등)
            updateLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 초기화 완료\r\n");
            textBox2Text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 초기화 완료\r\n";
        }


        private void MainSequenceThread()
        {
            int reportID = new int(); // 수신된 ReportID
            string lastModelID = string.Empty; // 이전에 처리된 ModelID 저장
            string lastMaterialType = string.Empty; // MaterialID의 마지막 문자 저장 (A 또는 B)
            bool judge = false;

            try
            {
                while (bMainThreadChk)
                {
                    if (recvReportIDequeue.Count > 0)
                    {
                        string strReport = recvReportIDequeue.Dequeue(); // 큐에서 ReportID 가져오기
                        reportID = Convert.ToInt32(strReport);

                        switch (reportID)
                        {
                            case ReportIDList.IDReport:
                                judge = true;
                                IDReport idReport = Report.IDReport.Dequeue(); // 큐에서 IDReport 가져오기

                                // Model ID Check
                                if (Config.currModelID == idReport.ModelID)
                                {
                                    string materialType = idReport.MaterialID.Substring(idReport.MaterialID.Length - 1, 1);

                                    if (materialType != Config.currModelID.Substring(Config.currModelID.Length - 1, 1))
                                    {
                                        judge = false;
                                    }
                                }
                                else
                                {
                                    judge = false;
                                }

                                if (judge)
                                {
                                    // START
                                    updateLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ModelID 동일, Material 타입 다름, START 전송\r\n");
                                    textBox2Text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ModelID 동일, Material 타입 다름, START 전송\r\n";

                                    // START 패킷 생성 및 전송
                                    string lotID = DateTime.Now.ToString("yyyyMM"); // LotID 생성 (현재 년월)
                                    var startMessage = $"{"_START"}/{idReport.ModelID}/{idReport.ProcID}/{idReport.MaterialID}/{lotID}/{idReport.code}/{idReport.text}";
                                    SendPacket(startMessage);

                                    updateLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] START 신호 전송 성공: {startMessage}\r\n");
                                    textBox2Text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] START 신호 전송 성공: {startMessage}\r\n";
                                }
                                else
                                {
                                    // CANCEL
                                    // ModelID가 다르거나 Material 타입이 동일할 경우 CANCEL
                                    updateLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ModelID 불일치 또는 Material 타입 동일, CANCEL 전송\r\n");
                                    textBox2Text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ModelID 불일치 또는 Material 타입 동일, CANCEL 전송\r\n";

                                    // CANCEL 패킷 생성 및 전송
                                    string lotID = DateTime.Now.ToString("yyyyMM"); // LotID 생성 (현재 년월)
                                    var cancelMessage = $"{"_CANCEL"}/{idReport.ModelID}/{idReport.ProcID}/{idReport.MaterialID}/{lotID}/{idReport.code}/{idReport.text}";
                                    SendPacket(cancelMessage);

                                    updateLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] CANCEL 신호 전송 성공: {cancelMessage}\r\n");
                                    textBox2Text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] CANCEL 신호 전송 성공: {cancelMessage}\r\n";
                                }

                                break;

                            case ReportIDList.StartedReport:

                                break;

                            case ReportIDList.CanceledReport:

                                break;

                            case ReportIDList.CompleteReport:

                                break;

                            case ReportIDList.ModelChangeStateChangedReport:
                                judge = false;
                                ModelChangeStateChangedReport modelChangeStateChangedReport = Report.modelChangeStateChangedReports.Dequeue();

                                for (int i = 0; i < Config.modelList.Count; i++)
                                {
                                    if (modelChangeStateChangedReport.ModelID == Config.modelList[i])
                                    {
                                        judge = true;
                                        break;
                                    }
                                }

                                if (judge)
                                {
                                    updateLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 모델 변경 일치, ACCEPT 전송\r\n");
                                    textBox2Text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]  모델 변경 일치, ACCEPT 전송\r\n";

                                    var acceptMessage = $"{"_MODEL_CHANGE_ACCEPT"}/{modelChangeStateChangedReport.ModelID}/{modelChangeStateChangedReport.ProcID}";
                                    SendPacket(acceptMessage);
                                    updateLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ACCEPT 신호 전송 성공: {acceptMessage}\r\n");
                                    textBox2Text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ACCEPT 신호 전송 성공: {acceptMessage}\r\n";
                                }
                                else
                                {
                                    updateLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 잘못된 ModelID, CANCEL 전송\r\n");
                                    textBox2Text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 잘못된 ModelID, CANCEL 전송\r\n";

                                    var cancelMessage = $"{"_MODEL_CHANGE_CANCEL"}/{modelChangeStateChangedReport.ModelID}/{modelChangeStateChangedReport.ProcID}";
                                    SendPacket(cancelMessage);
                                    updateLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] CANCEL 신호 전송 성공: {cancelMessage}\r\n");
                                    textBox2Text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] CANCEL 신호 전송 성공: {cancelMessage}\r\n";
                                }

                                break;

                            case ReportIDList.ModelChangeCanceledReport:
                                // 처리할 게 없음
                                // 로그만 출력

                                break;

                            case ReportIDList.ModelChangeCompletedReport:
                                ModelChangeCompletedReport modelChangeCompletedReport = Report.modelChangeCompletedReports.Dequeue();

                                Config.currModelID = modelChangeCompletedReport.ModelID;

                                break;

                            case ReportIDList.ModelListReport:

                                break;


                            default:
                                updateLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 처리되지 않은 ReportID: {reportID}\r\n");
                                textBox2Text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 처리되지 않은 ReportID: {reportID}\r\n";
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                updateLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] IDReport 처리 중 오류 발생\r\n");
                textBox2Text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] IDReport 처리 중 오류 발생\r\n";
            }
        }




        // 패킷 문자열 생성 메서드
        private string CreatePacket(string commandType, string modelID, string procID, string materialID, string code, string text)
        {
            string lotID = DateTime.Now.ToString("yyyyMM"); // LotID 생성 (현재 년월)
            return $"{commandType}/{modelID}/{procID}/{materialID}/{lotID}/{code}/{text}"; // 패킷 형식으로 문자열 반환
        }

        // 패킷 전송 메서드
        private void SendPacket(string message)
        {
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(message); // 메시지를 바이트 배열로 변환
                clientSocket.Send(buffer); // 소켓을 통해 메시지 전송
            }
            catch (Exception ex)
            {
                updateLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 패킷 전송 실패\r\n"); // 전송 실패 로그
                textBox2Text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 패킷 전송 실패\r\n";
            }
        }

        // 구조체 정의
        public struct StartPacket
        {
            public string StartType;  // 패킷 시작 유형
            public string RCMD;       // 명령 유형
            public string ModelID;    // Code01에서 가져온 ModelID
            public string ProcID;     // 사용자 입력 PROC ID
            public string MaterialID; // Code01에서 가져온 MaterialID
            public string LotID;      // 사용자 입력 LOT ID
            public string EndType;    // 패킷 종료 유형
        }
        public struct CancelPacket
        {
            public string StartType;  // 패킷 시작 유형
            public string RCMD;       // 명령 유형
            public string ModelID;    // Code01에서 가져온 ModelID
            public string ProcID;     // 사용자 입력 PROC ID
            public string MaterialID; // Code01에서 가져온 MaterialID
            public string LotID;      // 사용자 입력 LOT ID
            public string EndType;    // 패킷 종료 유형
        }

        private void timer1_Tick_1(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBox2Text))
            {
                textBox2.AppendText(textBox2Text);
                textBox2Text = string.Empty;
            }
        }

        //LOG 저장
        private void SaveLogToFile()
        {
            try
            {
                // 로그 파일에 기록
                using (StreamWriter writer = new StreamWriter(logFilePath, true, Encoding.UTF8))
                {
                    writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 로그 저장 시작");
                    writer.WriteLine(textBox2.Text); // textbox2에 출력된 내용을 파일에 저장
                    writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 로그 저장 종료");
                }
            }
            catch (Exception ex)
            {
                // 로그 저장 실패 시 메시지 표시
                MessageBox.Show($"로그 저장 실패: {ex.Message}");
            }
        }



        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox6_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void pictureBox7_Click(object sender, EventArgs e)
        {

        }


        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }
         
        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }


        private void pictureBox11_Click(object sender, EventArgs e)
        {

        }
    }
}

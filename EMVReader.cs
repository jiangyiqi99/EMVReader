/*=========================================================================================
'  Copyright(C):    Advanced Card Systems Ltd 
'  
'  Author :         Eternal TUTU
'
'  Module :         EMVReader.cs
'   
'  Date   :         June 23, 2008
'==========================================================================================*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace EMVCard
{
    public partial class MainEMVReaderBin : Form
    {
        public Int64 hContext, hCard;
        public int retCode, Protocol;
        public bool connActive, validATS;
        public bool autoDet;
        public byte[] SendBuff = new byte[263];
        public byte[] RecvBuff = new byte[263];
        public int reqType, Aprotocol, dwProtocol, cbPciLength;
        public Int64 SendLen, RecvLen, nBytesRet;
        public ModWinsCard64.SCARD_IO_REQUEST pioSendRequest;
        private Dictionary<string, string> labelToAID = new Dictionary<string, string>();


        public MainEMVReaderBin() {
            InitializeComponent();
        }

        private void ClearBuffers() {
            long indx;

            for (indx = 0; indx <= 262; indx++) {
                RecvBuff[indx] = 0;
                SendBuff[indx] = 0;
            }
        }

        private void displayOut(int errType, int retVal, string PrintText) {
            switch (errType) {
                case 0:
                    break;
                case 1:
                    PrintText = ModWinsCard64.GetScardErrMsg(retVal);
                    break;
                case 2:
                    PrintText = "<" + PrintText;
                    break;
                case 3:
                    PrintText = "> " + PrintText;
                    break;
            }

            richTextBoxLogs.Select(richTextBoxLogs.Text.Length, 0);
            richTextBoxLogs.SelectedText = PrintText + "\r\n";
            richTextBoxLogs.ScrollToCaret();
        }

        private void EnableButtons() {
            bInit.Enabled = false;
            bConnect.Enabled = true;
            bReset.Enabled = true;
            bClear.Enabled = true;
        }

        private void bInit_Click(object sender, EventArgs e) {
            string ReaderList = "" + Convert.ToChar(0);
            int indx;
            int pcchReaders = 0;
            string rName = "";

            // 1. Establish Context
            retCode = ModWinsCard64.SCardEstablishContext(ModWinsCard64.SCARD_SCOPE_USER, 0, 0, ref hContext);
            if (retCode != ModWinsCard64.SCARD_S_SUCCESS) {
                displayOut(1, retCode, "");
                return;
            }

            // 2. List PC/SC card readers installed in the system
            retCode = ModWinsCard64.SCardListReaders(this.hContext, null, null, ref pcchReaders);
            if (retCode != ModWinsCard64.SCARD_S_SUCCESS) {
                displayOut(1, retCode, "");
                return;
            }

            EnableButtons();

            byte[] ReadersList = new byte[pcchReaders];

            // Fill reader list
            retCode = ModWinsCard64.SCardListReaders(this.hContext, null, ReadersList, ref pcchReaders);
            if (retCode != ModWinsCard64.SCARD_S_SUCCESS) {
                displayOut(1, retCode, "");
                return;
            }

            rName = "";
            indx = 0;

            // Convert reader buffer to string
            while (ReadersList[indx] != 0) {
                while (ReadersList[indx] != 0) {
                    rName = rName + (char)ReadersList[indx];
                    indx = indx + 1;
                }

                // Add reader name to list
                cbReader.Items.Add(rName);
                rName = "";
                indx = indx + 1;
            }

            if (cbReader.Items.Count > 0)
                cbReader.SelectedIndex = 0;
        }

        private int SendAPDUandDisplay() {
            int indx;
            string tmpStr;

            pioSendRequest.dwProtocol = Aprotocol;
            pioSendRequest.cbPciLength = 8;

            // Display Apdu In
            tmpStr = "";
            for (indx = 0; indx <= SendLen - 1; indx++) {
                tmpStr = tmpStr + " " + string.Format("{0:X2}", SendBuff[indx]);
            }
            displayOut(2, 0, tmpStr);

            retCode = ModWinsCard64.SCardTransmit(hCard, ref pioSendRequest, SendBuff, SendLen, ref pioSendRequest, RecvBuff, ref RecvLen);
            if (retCode != ModWinsCard64.SCARD_S_SUCCESS) {
                displayOut(1, retCode, "");
                return retCode;
            }
            else {
                tmpStr = "";
                for (indx = 0; indx <= (RecvLen - 1); indx++) {
                    tmpStr = tmpStr + " " + string.Format("{0:X2}", RecvBuff[indx]);
                }
                displayOut(3, 0, tmpStr.Trim());
            }

            return retCode;
        }

        private void bReadApp_Click(object sender, EventArgs e) {
            textCardNum.Text = "";
            textEXP.Text = "";
            textHolder.Text = "";
            textTrack.Text = "";

            string selectedLabel = cbPSE.Text.Trim();
            if (!labelToAID.ContainsKey(selectedLabel)) {
                displayOut(0, 0, "��ѡ��һ��Ӧ�� AID");
                return;  // ����ķ����Ǻ���ģ���Ϊû��ѡ��AID
            }

            string aidHex = labelToAID[selectedLabel];
            string[] aidBytes = aidHex.Split(' ');
            string selectAID = $"00 A4 04 00 {aidBytes.Length:X2} {aidHex}";
            SendLen = FillBufferFromHexString(selectAID, SendBuff, 0);
            RecvLen = 0xFF;

            int result = TransmitWithAutoFix();
            if (result != 0 || !(RecvBuff[RecvLen - 2] == 0x90 && RecvBuff[RecvLen - 1] == 0x00)) {
                displayOut(0, 0, "ѡ�� AID ʧ��");
                return;  // ����ķ����Ǻ���ģ���ΪAIDѡ��ʧ��
            }

            byte[] fciData = new byte[RecvLen];
            Array.Copy(RecvBuff, fciData, RecvLen);

            // === ���� GPO ���Զ����� PDOL ===
            bool gpoSuccess = SendGPOWithAutoPDOL(fciData, RecvLen);
            if (!gpoSuccess) {
                displayOut(0, 0, "���� GPO ʧ��");
                // ��Ҫ�����ﷵ�أ��������Զ�ȡ����
            }

            // ���GPO�ɹ������Խ���GPO��Ӧ�е�����
            if (gpoSuccess) {
                // === �ȳ���ֱ�ӽ��� GPO ��Ӧ���Ƿ���� Track2��PAN �� TLV �ֶ� ===
                ParseTLV(RecvBuff, 0, (int)RecvLen - 2, 0, true);

                // === �ٽ��� AFL ����ȡ��¼ ===
                var aflList = ParseAFL(RecvBuff, RecvLen);
                if (aflList.Count > 0) {
                    // ���� AFL ��ȡ���м�¼
                    foreach (var (sfi, start, end) in aflList) {
                        for (int rec = start; rec <= end; rec++) {
                            string cmd = $"00 B2 {rec:X2} {((sfi << 3) | 4):X2} 00";
                            SendLen = FillBufferFromHexString(cmd, SendBuff, 0);
                            RecvLen = 0xFF;
                            result = TransmitWithAutoFix();
                            if (result != 0 || RecvLen < 2 || !(RecvBuff[RecvLen - 2] == 0x90 && RecvBuff[RecvLen - 1] == 0x00)) {
                                displayOut(0, 0, $"SFI {sfi} Record {rec} δ���� 90 00����������");
                                continue;
                            }
                            ParseRecordContent(RecvBuff, RecvLen - 2);
                        }
                    }
                }
                else {
                    displayOut(0, 0, "δ�ܽ����� AFL�����Զ�ȡ SFI 1 Record 1");
                    // ���Զ�ȡһЩ������SFI�ͼ�¼
                    TryReadCommonRecords();
                }
            }
            else {
                // ���GPOʧ�ܣ�Ҳ���Զ�ȡһЩ�����ļ�¼
                displayOut(0, 0, "����GPOʧ�ܣ�����ֱ�Ӷ�ȡ������¼");
                TryReadCommonRecords();
            }

            // �������ǰ��Ĳ����Ƿ�ɹ��������Դ�Track2������Ϣ
            FillMissingInfoFromTrack2();
        }

        // �������������Զ�ȡ������SFI�ͼ�¼
        private void TryReadCommonRecords() {
            // ������SFI�ͼ�¼���
            int[][] commonRecords = new int[][] {
        new int[] { 1, 1 },  // SFI 1, Record 1
        new int[] { 2, 1 },  // SFI 2, Record 1
        new int[] { 3, 1 },  // SFI 3, Record 1
        new int[] { 4, 1 },  // SFI 4, Record 1
        new int[] { 1, 2 },  // SFI 1, Record 2
        new int[] { 2, 2 }   // SFI 2, Record 2
    };

            foreach (var record in commonRecords) {
                int sfi = record[0];
                int rec = record[1];
                string cmd = $"00 B2 {rec:X2} {((sfi << 3) | 4):X2} 00";
                SendLen = FillBufferFromHexString(cmd, SendBuff, 0);
                RecvLen = 0xFF;
                int result = TransmitWithAutoFix();
                if (result == 0 && RecvLen >= 2 && RecvBuff[RecvLen - 2] == 0x90 && RecvBuff[RecvLen - 1] == 0x00) {
                    displayOut(0, 0, $"�ɹ���ȡ SFI {sfi} Record {rec}");
                    ParseRecordContent(RecvBuff, RecvLen - 2);
                }
            }
        }


        private void FillMissingInfoFromTrack2() {
            // ֻ�е����Ż���Ч��Ϊ��ʱ���Ŵ�Track2����ȡ
            if (!string.IsNullOrEmpty(textTrack.Text)) {
                string track2 = textTrack.Text;

                // ���ҷָ���"D"
                int dIndex = track2.IndexOf("D");

                if (dIndex > 0 && track2.Length >= dIndex + 5) {
                    // �������Ϊ�գ���Track2����ȡ
                    if (string.IsNullOrEmpty(textCardNum.Text)) {
                        string pan = track2.Substring(0, dIndex);
                        pan = pan.TrimEnd('F'); // ȥ��β����F���
                        textCardNum.Text = pan;
                        displayOut(0, 0, $"��Track2��ȡ����: {pan}");
                    }

                    // �����Ч��Ϊ�գ���Track2����ȡ
                    if (string.IsNullOrEmpty(textEXP.Text) && track2.Length >= dIndex + 5) {
                        string expiryYYMM = track2.Substring(dIndex + 1, 4);
                        if (System.Text.RegularExpressions.Regex.IsMatch(expiryYYMM, @"^\d{4}$")) {
                            string expiry = $"20{expiryYYMM.Substring(0, 2)}-{expiryYYMM.Substring(2)}";
                            textEXP.Text = expiry;
                            displayOut(0, 0, $"��Track2��ȡ��Ч��: {expiry}");
                        }
                    }
                }
                else {
                    // ����û����ȷ�ָ��������
                    // һЩ��Ƭ����ʹ��"="��Ϊ�ָ�����������������ʽ
                    dIndex = track2.IndexOf("=");
                    if (dIndex > 0 && track2.Length >= dIndex + 5) {
                        // ����ʹ��"="��Ϊ�ָ��������
                        if (string.IsNullOrEmpty(textCardNum.Text)) {
                            string pan = track2.Substring(0, dIndex);
                            pan = pan.TrimEnd('F');
                            textCardNum.Text = pan;
                            displayOut(0, 0, $"��Track2(=�ָ�)��ȡ����: {pan}");
                        }

                        if (string.IsNullOrEmpty(textEXP.Text) && track2.Length >= dIndex + 5) {
                            string expiryYYMM = track2.Substring(dIndex + 1, 4);
                            if (System.Text.RegularExpressions.Regex.IsMatch(expiryYYMM, @"^\d{4}$")) {
                                string expiry = $"20{expiryYYMM.Substring(0, 2)}-{expiryYYMM.Substring(2)}";
                                textEXP.Text = expiry;
                                displayOut(0, 0, $"��Track2(=�ָ�)��ȡ��Ч��: {expiry}");
                            }
                        }
                    }
                    else {
                        // ����ʹ�ù̶�λ�ý���
                        // ĳЩ��Ƭ����û����ȷ�ķָ���������ѭ�̶���ʽ
                        // ���磺ǰ16-19λ��PAN��������4λ����Ч��

                        // ������ȡPAN (����PAN����Ϊ16-19λ)
                        if (string.IsNullOrEmpty(textCardNum.Text)) {
                            // ���Բ�ͬ��PAN����
                            for (int panLength = 16; panLength <= 19; panLength++) {
                                if (track2.Length >= panLength) {
                                    string possiblePan = track2.Substring(0, panLength);
                                    // ����Ƿ�ȫ������
                                    if (System.Text.RegularExpressions.Regex.IsMatch(possiblePan, @"^\d+$")) {
                                        textCardNum.Text = possiblePan;
                                        displayOut(0, 0, $"��Track2(�̶���ʽ)��ȡ����: {possiblePan}");

                                        // ������ȡ��Ч��
                                        if (string.IsNullOrEmpty(textEXP.Text) && track2.Length >= panLength + 4) {
                                            string expiryYYMM = track2.Substring(panLength, 4);
                                            if (System.Text.RegularExpressions.Regex.IsMatch(expiryYYMM, @"^\d{4}$")) {
                                                string expiry = $"20{expiryYYMM.Substring(0, 2)}-{expiryYYMM.Substring(2)}";
                                                textEXP.Text = expiry;
                                                displayOut(0, 0, $"��Track2(�̶���ʽ)��ȡ��Ч��: {expiry}");
                                            }
                                        }
                                        break;
                                    }
                                }
                            }
                        }

                        // �����Ȼ�޷����������������ʽ
                        // ���磺ĳЩ�����������������ʽ
                        if (string.IsNullOrEmpty(textCardNum.Text) && track2.Length >= 30) {
                            // ����Ƿ��������������ʽ
                            // ���磺6231871800000762306D33122203870000000F
                            // ����6231871800000762��PAN��306D�����Ƿָ�������Ч��

                            // ������ȡǰ16λ��ΪPAN
                            string possiblePan = track2.Substring(0, 16);
                            if (possiblePan.StartsWith("62") || possiblePan.StartsWith("60")) { // ������BINͨ����62��60��ͷ
                                textCardNum.Text = possiblePan;
                                displayOut(0, 0, $"��Track2(���������ʽ)��ȡ����: {possiblePan}");

                                // ���Դ�λ��16����ȡ��Ч��
                                // ע�⣺����ĸ�ʽ������Ҫ����ʵ���������
                                if (string.IsNullOrEmpty(textEXP.Text) && track2.Length >= 20) {
                                    // ������Ч����PAN���λ�ã���ʽΪYYMM
                                    string expiryYYMM = "";

                                    // ���Բ�ͬ��λ��
                                    for (int i = 16; i <= 20 && i + 4 <= track2.Length; i++) {
                                        string possibleExpiry = track2.Substring(i, 4);
                                        // ����Ƿ��������Ч��(���ֻ����D/=)
                                        if (System.Text.RegularExpressions.Regex.IsMatch(possibleExpiry, @"^[\dD=]{4}$")) {
                                            expiryYYMM = possibleExpiry.Replace("D", "").Replace("=", "");
                                            if (expiryYYMM.Length == 4 && System.Text.RegularExpressions.Regex.IsMatch(expiryYYMM, @"^\d{4}$")) {
                                                string expiry = $"20{expiryYYMM.Substring(0, 2)}-{expiryYYMM.Substring(2)}";
                                                textEXP.Text = expiry;
                                                displayOut(0, 0, $"��Track2(���������ʽ)��ȡ��Ч��: {expiry}");
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // �����Ȼ�޷���������¼����
                if (string.IsNullOrEmpty(textCardNum.Text)) {
                    displayOut(0, 0, $"���棺�޷���Track2���ݽ���������: {track2}");
                }
            }
        }


        private bool SendGPOWithAutoPDOL(byte[] fciBuffer, long fciLen) {
            int index = 0;
            while (index < fciLen - 2) {
                if (fciBuffer[index] == 0x9F && fciBuffer[index + 1] == 0x38) {
                    index += 2;
                    int len = fciBuffer[index++];
                    byte[] pdolRaw = new byte[len];
                    Array.Copy(fciBuffer, index, pdolRaw, 0, len);

                    int pdolIndex = 0;
                    List<byte> pdolData = new List<byte>();
                    while (pdolIndex < pdolRaw.Length) {
                        int tag = pdolRaw[pdolIndex++];
                        if ((tag & 0x1F) == 0x1F) {
                            tag = (tag << 8) | pdolRaw[pdolIndex++];
                        }

                        int tagLen = pdolRaw[pdolIndex++];

                        // ������ PDOL ����
                        switch (tag) {
                            case 0x9F66: // TTQ
                                pdolData.AddRange(new byte[] { 0x37, 0x00, 0x00, 0x00 });
                                break;
                            case 0x9F02: // Amount Authorized
                                pdolData.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 });
                                break;
                            case 0x9F03: // Amount Other (Cashback)
                                pdolData.AddRange(new byte[tagLen]);
                                break;
                            case 0x9F1A: // Terminal Country Code (China: 0156)
                            case 0x5F2A: // Transaction Currency Code (RMB: 0156)
                                pdolData.AddRange(new byte[] { 0x01, 0x56 });
                                break;
                            case 0x9A: // Transaction Date (YYMMDD)
                                var date = DateTime.Now;
                                pdolData.AddRange(new byte[] {
                            (byte)(date.Year % 100),
                            (byte)(date.Month),
                            (byte)(date.Day)
                        });
                                break;
                            case 0x9C: // Transaction Type (default: Purchase)
                                pdolData.Add(0x00);
                                break;
                            case 0x9F37: // Unpredictable Number
                                var rnd = new Random();
                                for (int i = 0; i < tagLen; i++) {
                                    pdolData.Add((byte)rnd.Next(0, 256));
                                }
                                break;
                            default:
                                pdolData.AddRange(new byte[tagLen]); // ��0
                                break;
                        }
                    }

                    int pdolDataLen = pdolData.Count;
                    List<byte> gpo = new List<byte> {
                0x80, 0xA8, 0x00, 0x00,
                (byte)(pdolDataLen + 2), 0x83, (byte)pdolDataLen
            };
                    gpo.AddRange(pdolData);
                    gpo.Add(0x00); // Le

                    for (int i = 0; i < gpo.Count; i++)
                        SendBuff[i] = gpo[i];
                    SendLen = gpo.Count;
                    RecvLen = 0xFF;
                    int result = TransmitWithAutoFix();
                    if (result != 0)
                        return false;

                    if (RecvBuff[0] == 0x80 || RecvBuff[0] == 0x77) {
                        displayOut(0, 0, "GPO �ɹ����أ��� PDOL��");
                        return true;
                    }
                    else {
                        displayOut(0, 0, "GPO ���ظ�ʽ�쳣");
                        return false;
                    }
                }
                else {
                    index++;
                }
            }

            // === �� PDOL�����ͼ� GPO ===
            string gpoEmpty = "80 A8 00 00 02 83 00 00";
            SendLen = FillBufferFromHexString(gpoEmpty, SendBuff, 0);
            RecvLen = 0xFF;
            int res = TransmitWithAutoFix();
            if (res != 0)
                return false;

            if (RecvBuff[0] == 0x80 || RecvBuff[0] == 0x77) {
                displayOut(0, 0, "GPO �ɹ����أ���ģʽ��");
                return true;
            }
            else {
                displayOut(0, 0, "�� GPO ���ظ�ʽ�쳣");
                return false;
            }
        }

        private int TransmitWithAutoFix() {
            int result = SendAPDUandDisplay();

            // === ��� 1��SW = 6C XX����ʾLe��ƥ�䣬��Ҫ���·�һ�� ===
            if (RecvLen == 2 && RecvBuff[0] == 0x6C) {
                SendBuff[SendLen - 1] = RecvBuff[1]; // ���Ƽ������滻 Le
                RecvLen = RecvBuff[1] + 2;
                result = SendAPDUandDisplay();
                return result;
            }

            // === ��� 2��SW = 67 00����ʾȱʧ Le��Ҳ���Բ�Ϊ 0xFF �ٷ�һ�� ===
            if (RecvLen == 2 && RecvBuff[0] == 0x67 && RecvBuff[1] == 0x00) {
                    SendBuff[SendLen - 1] = 0xFF;
                    RecvLen = 0xFF;
                    result = SendAPDUandDisplay();
                    return result;
            }

            // === ��� 3��SW = 61 XX����Ҫ GET RESPONSE ===
            if (RecvLen == 2 && RecvBuff[0] == 0x61) {
                byte le = RecvBuff[1];
                SendLen = FillBufferFromHexString($"00 C0 00 00 {le:X2}", SendBuff, 0);
                RecvLen = le + 2;
                result = SendAPDUandDisplay();
                return result;
            }

            return result;
        }


        private List<(int sfi, int startRecord, int endRecord)> ParseAFL(byte[] buffer, long length) {
            var aflList = new List<(int, int, int)>();

            if (buffer[0] == 0x77)  // GPO ����ģ�� 77
            {
                int i = 0;
                while (i < length - 2) {
                    if (buffer[i] == 0x94) {
                        int len = buffer[i + 1];
                        int pos = i + 2;
                        while (pos + 3 < i + 2 + len) {
                            int sfi = buffer[pos] >> 3;
                            int start = buffer[pos + 1];
                            int end = buffer[pos + 2];
                            aflList.Add((sfi, start, end));
                            pos += 4;
                        }
                        break;
                    }
                    i++;
                }
            }
            else if (buffer[0] == 0x80)  // GPO ����ģ�� 80��Visa��
            {
                int totalLen = buffer[1];
                if (totalLen + 2 > buffer.Length)
                    return aflList;

                int pos = 2;
                pos += 2; // ���� AIP��2�ֽڣ�

                while (pos + 3 < 2 + totalLen) {
                    int sfi = buffer[pos] >> 3;
                    int start = buffer[pos + 1];
                    int end = buffer[pos + 2];
                    if (sfi >= 1 && sfi <= 31 && start >= 1 && end >= start) {
                        aflList.Add((sfi, start, end));
                    }
                    pos += 4;
                }
            }

            return aflList;
        }

        private void bLoadPSE_Click(object sender, EventArgs e) {
            cbPSE.Items.Clear();
            cbPSE.Text = "";
            textCardNum.Text = "";
            textEXP.Text = "";
            textHolder.Text = "";
            textTrack.Text = "";
            labelToAID.Clear();

            // === 1. ѡ�� PSE Ӧ�ã�1PAY.SYS.DDF01�� ===
            string selectPSE = "00 A4 04 00 0E 31 50 41 59 2E 53 59 53 2E 44 44 46 30 31";
            int cmdLen = FillBufferFromHexString(selectPSE, SendBuff, 0);
            SendLen = cmdLen;
            RecvLen = 0xFF;
            int result = TransmitWithAutoFix(); // �Զ����� 61
            if (result != 0) {
                displayOut(0, 0, "ѡ�� PSE Ӧ��ʧ��");
                return;
            }

            // === 2. �� SFI 1 ��ʼ������ȡ Record��ֱ������ 6A 83 ===
            for (int record = 1; ; record++) {
                string readSFI = $"00 B2 {record:X2} 0C 00"; // SFI=1, P2=0C, Le=00
                SendLen = FillBufferFromHexString(readSFI, SendBuff, 0);
                RecvLen = 0xFF;

                result = TransmitWithAutoFix();

                // ����Ƿ�Ϊ����¼�����ڡ�
                if (RecvLen == 2 && RecvBuff[0] == 0x6A && RecvBuff[1] == 0x83) {
                    displayOut(0, 0, $"Record {record} �����ڣ�������ȡ AID");
                    break;
                }

                // ����Ƿ�Ϊ�ɹ�
                if (result != 0 || RecvLen < 2 || !(RecvBuff[RecvLen - 2] == 0x90 && RecvBuff[RecvLen - 1] == 0x00)) {
                    displayOut(0, 0, $"Record {record} ��ȡʧ�ܣ�ֹͣ");
                    break;
                }

                displayOut(0, 0, $"���� Record {record} �е� AID ��Ϣ");
                ParseSFIRecord(RecvBuff, RecvLen - 2); // ����β�� SW1 SW2
            }

            // === �Զ�ѡ�е�һ��Ӧ�ã����У�===
            if (cbPSE.Items.Count > 0 && cbPSE.SelectedIndex == -1) {
                cbPSE.SelectedIndex = 0;
            }
        }


        private string InsertSpaces(string hex) {
            StringBuilder spaced = new StringBuilder();
            for (int i = 0; i < hex.Length; i += 2) {
                if (i > 0)
                    spaced.Append(" ");
                spaced.Append(hex.Substring(i, 2));
            }
            return spaced.ToString();
        }


        private void bLoadPPSE_Click(object sender, EventArgs e) {
            cbPSE.Items.Clear();
            cbPSE.Text = "";
            textCardNum.Text = "";
            textEXP.Text = "";
            textHolder.Text = "";
            textTrack.Text = "";
            labelToAID.Clear();

            // === 1. ѡ�� PPSE Ӧ�� ===
            string selectPPSE = "00 A4 04 00 0E 32 50 41 59 2E 53 59 53 2E 44 44 46 30 31";
            int cmdLen = FillBufferFromHexString(selectPPSE, SendBuff, 0);
            SendLen = cmdLen;
            RecvLen = 0xFF;
            int result = TransmitWithAutoFix();
            if (result != 0 || RecvLen < 2 || !(RecvBuff[RecvLen - 2] == 0x90 && RecvBuff[RecvLen - 1] == 0x00)) {
                displayOut(0, 0, "ѡ�� PPSE Ӧ��ʧ��");
                return;
            }

            // === 2. �ӷ��ص� FCI Template �в������� Application Template (61) ===
            int index = 0;
            while (index < RecvLen - 2) {
                if (RecvBuff[index] == 0x61) {
                    int len = RecvBuff[index + 1];
                    int start = index + 2;
                    int end = start + len;
                    if (end > RecvLen - 2)
                        break;

                    string currentAID = "";
                    string label = "";

                    int subIndex = start;
                    while (subIndex < end) {
                        byte tag = RecvBuff[subIndex++];
                        if (subIndex >= end)
                            break;
                        int tagLen = RecvBuff[subIndex++];

                        if (subIndex + tagLen > end)
                            break;
                        byte[] value = new byte[tagLen];
                        Array.Copy(RecvBuff, subIndex, value, 0, tagLen);
                        subIndex += tagLen;

                        if (tag == 0x4F) {
                            currentAID = string.Join(" ", value.Select(b => b.ToString("X2")));
                            displayOut(0, 0, "AID: " + currentAID);
                        }


                        else if (tag == 0x50) {
                            label = Encoding.ASCII.GetString(value).Trim();
                            displayOut(0, 0, "Application Label: " + label);
                        }
                    }

                    if (!string.IsNullOrEmpty(currentAID)) {
                        if (string.IsNullOrEmpty(label)) {
                            label = "App_" + currentAID.Substring(currentAID.Length - 4);
                        }
                        if (!cbPSE.Items.Contains(label)) {
                            // ����Ҫ���cbPSE��labelToAIDʱ������ParseSFIRecord��bLoadPPSE_Click�
                            int aidIndex = cbPSE.Items.Count + 1; // 1��ʼ���
                            string itemName = $"{aidIndex}. {label}";
                            // ��֤cbPSE.Items���ݲ����ظ�
                            cbPSE.Items.Add(itemName);
                            // �� itemName ��Ϊkey��AID
                            labelToAID[itemName] = currentAID;

                        }
                    }

                    index = end;
                }
                else {
                    index++;
                }
            }

            // === �Զ�ѡ�е�һ�� ===
            if (cbPSE.Items.Count > 0 && cbPSE.SelectedIndex == -1) {
                cbPSE.SelectedIndex = 0;
            }
        }


        private string ParseTLV(byte[] buffer, int startIndex, int endIndex, int priority = 0, bool storeTrack2 = true) {
            string track2Data = null;
            int index = startIndex;

            while (index < endIndex) {
                if (index >= buffer.Length)
                    break;

                // ����Tag
                byte tag = buffer[index++];
                byte? tag2 = null;

                if ((tag & 0x1F) == 0x1F) {
                    if (index >= buffer.Length)
                        break;
                    tag2 = buffer[index++];
                }

                int tagValue = tag2.HasValue ? (tag << 8 | tag2.Value) : tag;

                // ����Length
                if (index >= buffer.Length)
                    break;

                int len = buffer[index++];
                if (len >= 0x80) {
                    int lenLen = (len & 0x7F);

                    if (lenLen <= 0 || lenLen > 3 || index + lenLen > buffer.Length) {
                        displayOut(0, 0, $"TLV �����ֶ��쳣��lenLen={lenLen}��index={index}");
                        break;
                    }

                    len = 0;
                    for (int i = 0; i < lenLen; i++) {
                        len = (len << 8) + buffer[index++];
                    }
                }

                // ��ȫ���
                if (len < 0 || len > 4096 || index + len > buffer.Length) {
                    displayOut(0, 0, $"TLV ���ȷǷ���len={len}��index={index}");
                    break;
                }

                // ��ȡValue
                byte[] value = new byte[len];
                Array.Copy(buffer, index, value, 0, len);
                index += len;

                // ����Tag��������
                switch (tagValue) {
                    case 0x5A: // PAN (����)
                        if (priority > 0 || string.IsNullOrEmpty(textCardNum.Text)) {
                            string pan = BitConverter.ToString(value).Replace("-", "");
                            // ȥ��β����F���
                            pan = pan.TrimEnd('F');
                            textCardNum.Text = pan;
                            displayOut(0, 0, $"����(PAN): {pan}");
                        }
                        break;

                    case 0x5F24: // ��Ч��
                        if (priority > 0 || string.IsNullOrEmpty(textEXP.Text)) {
                            string rawDate = BitConverter.ToString(value).Replace("-", "");
                            string expiry = "";

                            if (rawDate.Length >= 6) {
                                expiry = $"20{rawDate.Substring(0, 2)}-{rawDate.Substring(2, 2)}-{rawDate.Substring(4, 2)}";
                            }
                            else if (rawDate.Length >= 4) {
                                expiry = $"20{rawDate.Substring(0, 2)}-{rawDate.Substring(2, 2)}";
                            }

                            if (!string.IsNullOrEmpty(expiry)) {
                                textEXP.Text = expiry;
                                displayOut(0, 0, $"��Ч��: {expiry}");
                            }
                        }
                        break;

                    case 0x5F20: // �ֿ�������
                        if (priority > 0 || string.IsNullOrEmpty(textHolder.Text)) {
                            string name = Encoding.ASCII.GetString(value).Trim();
                            textHolder.Text = name;
                            displayOut(0, 0, $"�ֿ�������: {name}");
                        }
                        break;

                    case 0x57: // Track2����
                        string track2 = BitConverter.ToString(value).Replace("-", "");
                        if (storeTrack2) {
                            textTrack.Text = track2;
                            track2Data = track2;
                            displayOut(0, 0, $"Track2 ����: {track2}");
                        }
                        break;

                    case 0x9F6B: // Track2��Ч����
                        if (string.IsNullOrEmpty(textTrack.Text)) {
                            string track2Equiv = BitConverter.ToString(value).Replace("-", "");
                            if (storeTrack2) {
                                textTrack.Text = track2Equiv;
                                track2Data = track2Equiv;
                                displayOut(0, 0, $"Track2��Ч����: {track2Equiv}");
                            }
                        }
                        break;

                    case 0x70: // Record Template
                    case 0x77: // Response Message Template Format 2
                        ParseTLV(value, 0, value.Length, priority, storeTrack2);
                        break;

                    case 0x80: // Response Message Template Format 1
                        if (len > 2) { // ȷ�����㹻������
                                       // ����AIP(2�ֽ�)
                            ParseTLV(value, 2, value.Length, priority, storeTrack2);
                        }
                        break;
                }
            }

            return track2Data;
        }



        private void bConnect_Click(object sender, EventArgs e) {
            cbPSE.Items.Clear();
            cbPSE.Text = "";
            textCardNum.Text = "";
            textEXP.Text = "";
            textHolder.Text = "";
            textTrack.Text = "";

            // Connect to selected reader using hContext handle and obtain hCard handle
            if (connActive)
                retCode = ModWinsCard64.SCardDisconnect(hCard, ModWinsCard64.SCARD_UNPOWER_CARD);

            // Shared Connection
            retCode = ModWinsCard64.SCardConnect(hContext, cbReader.Text, ModWinsCard64.SCARD_SHARE_SHARED, ModWinsCard64.SCARD_PROTOCOL_T0 | ModWinsCard64.SCARD_PROTOCOL_T1, ref hCard, ref Protocol);
            if (retCode == ModWinsCard64.SCARD_S_SUCCESS)
                displayOut(0, 0, "Successful connection to " + cbReader.Text);
            else {
                displayOut(1, retCode, "");
                return;
            }
            byte[] atr = new byte[33];
            long atrLen = atr.Length;
            int readerLen = 0;
            int state = 0;
            retCode = ModWinsCard64.SCardStatus(
                hCard,
                null,
                ref readerLen,
                ref state,
                ref Protocol,           // ���� ref proto
                atr,
                ref atrLen
            );

            if (retCode == ModWinsCard64.SCARD_S_SUCCESS) {
                string atrStr = BitConverter.ToString(atr, 0, (int)atrLen);
                displayOut(0, 0, "ATR: " + atrStr);

                // ���ж��Ƿ�Ӵ�ʽ��
                if (atrLen > 0 && (atr[0] == 0x3B || atr[0] == 0x3F)) {
                    displayOut(0, 0, "��ƬĬ���ڽӴ�ʽģʽ����");
                }
                else {
                    displayOut(0, 0, "��ƬĬ���ڷǽӴ�ʽģʽ����");
                }
            }
            else {
                displayOut(1, retCode, "�޷���ȡ ATR");
            }
            connActive = true;
        }

        private void bClear_Click(object sender, EventArgs e) {
            richTextBoxLogs.Clear();
        }

        private void clearInterface() {
            cbPSE.Items.Clear();
            cbPSE.Text = "";
            richTextBoxLogs.Clear();
            textCardNum.Text = "";
            textEXP.Text = "";
            textHolder.Text = "";
            textTrack.Text = "";

        }

        private void bReset_Click(object sender, EventArgs e) {
            if (connActive)
                retCode = ModWinsCard64.SCardDisconnect(hCard, ModWinsCard64.SCARD_UNPOWER_CARD);
            cbReader.Items.Clear();
            cbReader.Text = "";
            bInit.Enabled = true;
            cbPSE.Items.Clear();
            cbPSE.Text = "";
            richTextBoxLogs.Clear();
            textCardNum.Text = "";
            textEXP.Text = "";
            textHolder.Text = "";
            textTrack.Text = "";
            retCode = ModWinsCard64.SCardReleaseContext(hContext);
        }

        private void bQuit_Click(object sender, EventArgs e) {
            // terminate the application
            retCode = ModWinsCard64.SCardDisconnect(hCard, ModWinsCard64.SCARD_UNPOWER_CARD);
            retCode = ModWinsCard64.SCardReleaseContext(hContext);
            System.Environment.Exit(0);
        }

        public void ParseSFIRecord(byte[] buffer, long length) {
            string currentAID = "";
            int index = 0;
            
            while (index < length) {
                byte tag = buffer[index++];
                byte? tag2 = null;

                // �������ֽ� Tag���� 9F 12��
                if ((tag & 0x1F) == 0x1F) {
                    tag2 = buffer[index++];
                }

                int tagValue = tag2.HasValue ? (tag << 8 | tag2.Value) : tag;

                // ��ȡ����
                int len = buffer[index++];
                if (len > 0x80) {
                    int lenLen = len & 0x7F;
                    len = 0;
                    for (int i = 0; i < lenLen; i++)
                        len = (len << 8) + buffer[index++];
                }

                // ��ȡ Value
                byte[] value = new byte[len];
                Array.Copy(buffer, index, value, 0, len);
                index += len;

                // ��������ӡ�����ֶ�
                switch (tagValue) {
                    case 0x4F: // AID
                        currentAID = string.Join(" ", value.Select(b => b.ToString("X2")));
                        displayOut(0, 0, "AID: " + currentAID);
                        break;
                    case 0x50: // Application Label
                        string label = Encoding.ASCII.GetString(value);
                        displayOut(0, 0, "Application Label: " + label);
                        if (!cbPSE.Items.Contains(label)) {
                            cbPSE.Items.Add(label);
                            if (!labelToAID.ContainsKey(label) && !string.IsNullOrEmpty(currentAID)) {
                                labelToAID[label] = currentAID;
                            }
                        }
                        break;
                    case 0x9F12: // Preferred Name
                        displayOut(0, 0, "Preferred Name: " + Encoding.ASCII.GetString(value));
                        break;
                    case 0x87: // Priority
                        displayOut(0, 0, "Application Priority: " + value[0]);
                        break;
                    case 0x61: // Application Template, recursively parse
                    case 0x70: // FCI template
                        displayOut(0, 0, $"Template tag {tagValue:X}, parsing inner TLVs...");
                        ParseSFIRecord(value, len);
                        break;
                    default:
                        // �����ֶοɸ�����Ҫ���
                        break;
                }
            }
        }

        public int FillBufferFromHexString(string hexString, byte[] buffer, int startIndex) {
            if (string.IsNullOrWhiteSpace(hexString))
                throw new ArgumentException("�����ַ�������Ϊ��", nameof(hexString));
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (startIndex < 0 || startIndex >= buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex), "��ʼλ�ó�����������Χ");

            string[] hexValues = hexString.Split(' ', (char)StringSplitOptions.RemoveEmptyEntries);
            int byteCount = hexValues.Length;

            if (startIndex + byteCount > buffer.Length)
                throw new ArgumentException("buffer �������޷�������������");

            for (int i = 0; i < byteCount; i++) {
                if (!byte.TryParse(hexValues[i], System.Globalization.NumberStyles.HexNumber, null, out byte result))
                    throw new FormatException($"�޷����� '{hexValues[i]}' Ϊʮ�������ֽ�");
                buffer[startIndex + i] = result;
            }

            return byteCount;
        }

        private void ParseRecordContent(byte[] buffer, long len) {
            // ����Ƿ���ģ���ʽ(70��ͷ)
            if (buffer[0] == 0x70) {
                int templateLen = buffer[1];
                int startPos = 2;

                // ������ʽ����
                if (buffer[1] > 0x80) {
                    int lenBytes = buffer[1] & 0x7F;
                    templateLen = 0;
                    for (int i = 0; i < lenBytes; i++) {
                        templateLen = (templateLen << 8) | buffer[2 + i];
                    }
                    startPos = 2 + lenBytes;
                }

                // ʹ�ø����ȼ�(1)������¼�ļ�
                ParseTLV(buffer, 0, (int)len, 1, true);
            }
            else {
                // ֱ��ʹ�ø����ȼ�����TLV����
                ParseTLV(buffer, 0, (int)len, 1, true);
            }
        }

    }

}
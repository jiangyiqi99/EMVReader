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
                return;
            }

            string aidHex = labelToAID[selectedLabel];
            string[] aidBytes = aidHex.Split(' ');
            string selectAID = $"00 A4 04 00 {aidBytes.Length:X2} {aidHex}";
            SendLen = FillBufferFromHexString(selectAID, SendBuff, 0);
            RecvLen = 0xFF;

            int result = TransmitWithAutoFix();
            if (result != 0 || !(RecvBuff[RecvLen - 2] == 0x90 && RecvBuff[RecvLen - 1] == 0x00)) {
                displayOut(0, 0, "ѡ�� AID ʧ��");
                return;
            }

            byte[] fciData = new byte[RecvLen];
            Array.Copy(RecvBuff, fciData, RecvLen);

            // === ���Է��� GPO���Զ����� PDOL��===
            if (!SendGPOWithAutoPDOL(fciData, RecvLen)) {
                displayOut(0, 0, "���� GPO ʧ�ܣ�����Ĭ�϶�ȡ SFI 3 Record 1-10");
            }

            // === ���ȳ��Դ� GPO ��Ӧ����ȡ AFL (Tag 94 in 77 format) ===
            bool aflFound = false;

            // Tag 77 ģ��
            for (int i = 0; i < RecvLen - 2; i++) {
                if (RecvBuff[i] == 0x94) {
                    aflFound = true;
                    int len = RecvBuff[i + 1];
                    for (int j = 0; j + 4 <= len; j += 4) {
                        int sfi = RecvBuff[i + 2 + j] >> 3;
                        int recordStart = RecvBuff[i + 3 + j];
                        int recordEnd = RecvBuff[i + 4 + j];

                        for (int rec = recordStart; rec <= recordEnd; rec++) {
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
                    break;
                }
            }

            // Tag 80 ģ�壺�򻯸�ʽ GPO��AFL ���� AIP��2 �ֽڣ�֮��
            if (!aflFound && RecvBuff[0] == 0x80 && RecvLen >= 6) {
                aflFound = true;
                int aflStart = 4; // ���� Tag(1) + Len(1) + AIP(2)
                int aflLen = RecvBuff[1] - 2; // AFL ���� = �ܳ� - AIP(2)
                for (int i = 0; i + 4 <= aflLen; i += 4) {
                    int sfi = RecvBuff[aflStart + i] >> 3;
                    int recordStart = RecvBuff[aflStart + i + 1];
                    int recordEnd = RecvBuff[aflStart + i + 2];

                    for (int rec = recordStart; rec <= recordEnd; rec++) {
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

            // === ���û�� AFL�����Խ��� Track2 ���� (Tag 57) ===
            if (!aflFound) {
                for (int i = 0; i < RecvLen - 2; i++) {
                    if (RecvBuff[i] == 0x57) {
                        int len = RecvBuff[i + 1];
                        byte[] track2 = new byte[len];
                        Array.Copy(RecvBuff, i + 2, track2, 0, len);
                        string track2str = BitConverter.ToString(track2).Replace("-", "");
                        textTrack.Text = track2str;
                        displayOut(0, 0, $"Track2 ����: {track2str}");

                        int dIndex = track2str.IndexOf("D");
                        if (dIndex > 0) {
                            string pan = track2str.Substring(0, dIndex);
                            string expiry = track2str.Substring(dIndex + 1, 4);
                            textCardNum.Text = pan;
                            textEXP.Text = $"20{expiry.Substring(0, 2)}-{expiry.Substring(2)}";
                            displayOut(0, 0, $"����: {pan}");
                            displayOut(0, 0, $"��Ч��: 20{expiry.Substring(0, 2)}-{expiry.Substring(2)}");
                        }
                        return;
                    }
                }
                displayOut(0, 0, "δ�ܴ� GPO ��Ӧ�л�ȡ��Ч AFL �� Track2 ����");
            }
        }


        private void ParseRecordContent(byte[] buffer, long len) {
            for (int i = 0; i < len - 1; i++) {
                // === Track 2 ���� (57) ===
                if (buffer[i] == 0x57) {
                    int tlen = buffer[i + 1];
                    if (i + 2 + tlen > len)
                        continue;
                    byte[] track2 = new byte[tlen];
                    Array.Copy(buffer, i + 2, track2, 0, tlen);
                    string track2str = BitConverter.ToString(track2).Replace("-", "");
                    textTrack.Text = track2str;
                    displayOut(0, 0, $"Track2 ����: {track2str}");

                    int dIndex = track2str.IndexOf("D");
                    if (dIndex > 0) {
                        string pan = track2str.Substring(0, dIndex);
                        string expiry = track2str.Substring(dIndex + 1, 4);
                        textCardNum.Text = pan;
                        textEXP.Text = $"20{expiry.Substring(0, 2)}-{expiry.Substring(2)}";
                        displayOut(0, 0, $"����: {pan}");
                        displayOut(0, 0, $"��Ч��: 20{expiry.Substring(0, 2)}-{expiry.Substring(2)}");
                    }
                }

                // === �ֿ������� (5F20) ===
                else if (buffer[i] == 0x5F && buffer[i + 1] == 0x20) {
                    int tlen = buffer[i + 2];
                    if (i + 3 + tlen > len)
                        continue;
                    byte[] nameBytes = new byte[tlen];
                    Array.Copy(buffer, i + 3, nameBytes, 0, tlen);
                    string name = Encoding.ASCII.GetString(nameBytes).Trim();
                    textHolder.Text = name;
                    displayOut(0, 0, $"�ֿ�������: {name}");
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
                            cbPSE.Items.Add(label);
                            labelToAID[label] = currentAID;
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


        private void ParseTLV(byte[] buffer, int startIndex, int endIndex) {
            long index = startIndex;

            while (index < endIndex) {
                if (index >= buffer.Length)
                    break;

                byte tag = buffer[index++];
                byte? tag2 = null;

                if ((tag & 0x1F) == 0x1F) {
                    if (index >= buffer.Length)
                        break;
                    tag2 = buffer[index++];
                }

                int tagValue = tag2.HasValue ? (tag << 8 | tag2.Value) : tag;

                if (index >= buffer.Length)
                    break;

                long len = buffer[index++];
                if (len >= 0x80) {
                    long lenLen = (len & 0x7F);

                    // ��ֹ�쳣����������EMV һ�㲻�ᳬ�� 3 �ֽڣ�
                    if (lenLen <= 0 || lenLen > 3) {
                        displayOut(0, 0, $"TLV �����ֶ��쳣��lenLen={lenLen}��index={index}");
                        break;
                    }

                    len = 0;
                    for (int i = 0; i < lenLen; i++) {
                        if (index >= buffer.Length) {
                            displayOut(0, 0, "TLV �����ֶζ�ȡԽ��");
                            break;
                        }
                        len = (len << 8) + buffer[index++];
                    }
                }

                // ��ȫ�Լ��
                if (len < 0 || len > 4096 || index + len > buffer.Length) {
                    displayOut(0, 0, $"TLV ���ȷǷ���len={len}��index={index}");
                    // ��ӡ��ǰƬ�����ڵ���
                    long printStart = Math.Max(index - 5, 0);
                    long printLen = Math.Min(10, buffer.Length - printStart);
                    displayOut(0, 0, "���� TLV Ƭ��: " + BitConverter.ToString(buffer, (int)printStart, (int)printLen));
                    break;
                }

                byte[] value = new byte[len];
                Array.Copy(buffer, index, value, 0, len);
                index += len;

                switch (tagValue) {
                    case 0x5A: // PAN
                        string pan = BitConverter.ToString(value).Replace("-", "");
                        textCardNum.Text = pan;
                        displayOut(0, 0, "����(PAN): " + pan);
                        break;

                    case 0x5F24: // Expiry
                        string rawDate = BitConverter.ToString(value).Replace("-", "");
                        if (rawDate.Length >= 4) {
                            string expiry = "20" + rawDate.Substring(0, 2) + "-" + rawDate.Substring(2, 2);
                            textEXP.Text = expiry;
                            displayOut(0, 0, "��Ч��: " + expiry);
                        }
                        break;

                    case 0x5F20: // Name
                        string name = Encoding.ASCII.GetString(value);
                        textHolder.Text = name;
                        displayOut(0, 0, "�ֿ�������: " + name);
                        break;

                    case 0x57: // Track2
                        string track2 = BitConverter.ToString(value).Replace("-", "");
                        textTrack.Text = track2;
                        displayOut(0, 0, "Track2 ����: " + track2);
                        break;

                    case 0x70: // FCI or EMV Template
                        ParseTLV(value, 0, value.Length); // �ݹ����
                        break;

                    default:
                        // �������������Ը�����Ҫ��Ӹ��� tag ֧��
                        break;
                }
            }
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

    }

}
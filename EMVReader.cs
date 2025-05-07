/*=========================================================================================
'  Copyright(C):    Advanced Card Systems Ltd 
' 
'  Description:     This sample program outlines the steps on how to
'                   implement the binary file support in ACOS3-24K
'  
'  Author :         Daryl M. Rojas
'
'  Module :         EMVReader.cs
'   
'  Date   :         June 23, 2008
'
' Revision Trail:   (Date/Author/Description) 
'                    April 20, 2010/ Gil Bagaporo/ Converted to VS.NET 2008
'==========================================================================================*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Forms;

namespace ACOSBinary
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

            // Begin select Application
            if (cbPSE.SelectedItem == null) {
                displayOut(0, 0, "��ѡ��һ�� Application Label");
                return;
            }
            string label = cbPSE.SelectedItem.ToString();
            if (!labelToAID.ContainsKey(label)) {
                displayOut(0, 0, "δ�ҵ��� Label ��Ӧ�� AID");
                return;
            }

            // === 1. ѡ�� AID ===
            string aidHex = labelToAID[label];
            string selectApdu = "00 A4 04 00 " + (aidHex.Length / 2).ToString("X2") + " " + InsertSpaces(aidHex);
            int cmdLen = FillBufferFromHexString(selectApdu, SendBuff, 0);
            SendLen = cmdLen;
            RecvLen = 0xFF;
            int result = TransmitWithAutoFix(); // �Զ����� 61
            if (result != 0)
                return;

            // === 2. ���� GPO ���� ===
            if (!SendGPOWithAutoPDOL(RecvBuff, (int)RecvLen)) {
                displayOut(0, 0, "���� GPO ʧ�ܣ�����Ĭ�� SFI 3 Record 1-10");

                for (int record = 1; record <= 10; record++) {
                    int sfiByte = (3 << 3) | 0x04;
                    string readApdu = $"00 B2 {record:X2} {sfiByte:X2} 00";
                    SendLen = FillBufferFromHexString(readApdu, SendBuff, 0);
                    RecvLen = 0xFF;

                    result = TransmitWithAutoFix();
                    if (result == 0) {
                        displayOut(0, 0, $"��ȡĬ�� SFI 3, Record {record} �ɹ�");
                        ParseTLV(RecvBuff, 0, (int)RecvLen);
                    }
                }
                return;
            }

            // === 3. ���� GPO �����е� AFL ===
            var aflList = ParseAFL(RecvBuff, RecvLen);
            if (aflList.Count == 0) {
                displayOut(0, 0, "δ�ܴ� GPO ��Ӧ�л�ȡ��Ч AFL");
                return;
            }

            // === 4. ���� AFL ָ���ļ�¼���ж�ȡ ===
            foreach (var (sfi, start, end) in aflList) {
                for (int record = start; record <= end; record++) {
                    int sfiByte = (sfi << 3) | 0x04;
                    string readApdu = $"00 B2 {record:X2} {sfiByte:X2} 00";
                    SendLen = FillBufferFromHexString(readApdu, SendBuff, 0);
                    RecvLen = 0xFF;

                    result = TransmitWithAutoFix();
                    if (result != 0) {
                        displayOut(0, 0, $"��ȡ SFI {sfi}, Record {record} ʧ��");
                        continue;
                    }

                    displayOut(0, 0, $"��ȡ SFI {sfi}, Record {record} �ɹ�");
                    ParseTLV(RecvBuff, 0, (int)RecvLen);
                }
            }
        }

        private bool SendGPOWithAutoPDOL(byte[] fciBuffer, int fciLen) {
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
                        int tag1 = pdolRaw[pdolIndex++];
                        int tag = tag1;
                        if ((tag1 & 0x1F) == 0x1F)
                            tag = (tag << 8) | pdolRaw[pdolIndex++];

                        int tagLen = pdolRaw[pdolIndex++];

                        switch (tag) {
                            case 0x9F02:
                                pdolData.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 });
                                break;
                            case 0x9F1A:
                                pdolData.AddRange(new byte[] { 0x01, 0x56 });
                                break;
                            case 0x5F2A:
                                pdolData.AddRange(new byte[] { 0x01, 0x56 });
                                break;
                            default:
                                pdolData.AddRange(new byte[tagLen]);
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

            // û�� PDOL����ʹ�ü� GPO
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

            // === 1. ѡ�� PSE Ӧ�ã�1PAY.SYS.DDF01��===
            string selectPSE = "00 A4 04 00 0E 31 50 41 59 2E 53 59 53 2E 44 44 46 30 31";
            int cmdLen = FillBufferFromHexString(selectPSE, SendBuff, 0);
            SendLen = cmdLen;
            RecvLen = 0xFF;
            int result = TransmitWithAutoFix(); // �Զ����� 61
            if (result != 0) {
                displayOut(0, 0, "ѡ�� PSE Ӧ��ʧ��");
                return;
            }

            // === 2. ��ȡ SFI 1 �� Record 1��ͨ���������� AID ��Ϣ��===
            string readSFI1 = "00 B2 01 0C 00"; // SFI=1, Record=1, Le=00
            cmdLen = FillBufferFromHexString(readSFI1, SendBuff, 0);
            SendLen = cmdLen;
            RecvLen = 0xFF;
            result = TransmitWithAutoFix();
            if (result != 0) {
                displayOut(0, 0, "��ȡ SFI 1 Record 1 ʧ��");
                return;
            }

            // === 3. ���� Record����ȡ Application Label ������������ ===
            ParseSFIRecord(RecvBuff, RecvLen);

            // === �� cbPSE ���������Զ�ѡ�е�һ�� ===
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

        private void ParseTLV(byte[] buffer, int startIndex, int endIndex) {
            int index = startIndex;

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
                    int lenLen = (int)(len & 0x7F);

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
                    int printStart = Math.Max(index - 5, 0);
                    int printLen = Math.Min(10, buffer.Length - printStart);
                    displayOut(0, 0, "���� TLV Ƭ��: " + BitConverter.ToString(buffer, printStart, printLen));
                    break;
                }

                byte[] value = new byte[len];
                Array.Copy(buffer, index, value, 0, (int)len);
                index += (int)len;

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
                displayOut(0, 0, "ATR: " + BitConverter.ToString(atr, 0, (int)atrLen));
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
                        currentAID = BitConverter.ToString(value).Replace("-", "");
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
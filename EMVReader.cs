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
                displayOut(0, 0, "请选择一个应用 AID");
                return;
            }

            string aidHex = labelToAID[selectedLabel];
            string[] aidBytes = aidHex.Split(' ');
            string selectAID = $"00 A4 04 00 {aidBytes.Length:X2} {aidHex}";
            SendLen = FillBufferFromHexString(selectAID, SendBuff, 0);
            RecvLen = 0xFF;

            int result = TransmitWithAutoFix();
            if (result != 0 || !(RecvBuff[RecvLen - 2] == 0x90 && RecvBuff[RecvLen - 1] == 0x00)) {
                displayOut(0, 0, "选择 AID 失败");
                return;
            }

            byte[] fciData = new byte[RecvLen];
            Array.Copy(RecvBuff, fciData, RecvLen);

            // === 尝试发送 GPO（自动解析 PDOL）===
            if (!SendGPOWithAutoPDOL(fciData, RecvLen)) {
                displayOut(0, 0, "发送 GPO 失败，尝试默认读取 SFI 3 Record 1-10");
            }

            // === 优先尝试从 GPO 响应中提取 AFL (Tag 94 in 77 format) ===
            bool aflFound = false;

            // Tag 77 模板
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
                                displayOut(0, 0, $"SFI {sfi} Record {rec} 未返回 90 00，跳过解析");
                                continue;
                            }
                            ParseRecordContent(RecvBuff, RecvLen - 2);
                        }
                    }
                    break;
                }
            }

            // Tag 80 模板：简化格式 GPO，AFL 紧跟 AIP（2 字节）之后
            if (!aflFound && RecvBuff[0] == 0x80 && RecvLen >= 6) {
                aflFound = true;
                int aflStart = 4; // 跳过 Tag(1) + Len(1) + AIP(2)
                int aflLen = RecvBuff[1] - 2; // AFL 长度 = 总长 - AIP(2)
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
                            displayOut(0, 0, $"SFI {sfi} Record {rec} 未返回 90 00，跳过解析");
                            continue;
                        }
                        ParseRecordContent(RecvBuff, RecvLen - 2);
                    }
                }
            }

            // === 如果没有 AFL，尝试解析 Track2 数据 (Tag 57) ===
            if (!aflFound) {
                for (int i = 0; i < RecvLen - 2; i++) {
                    if (RecvBuff[i] == 0x57) {
                        int len = RecvBuff[i + 1];
                        byte[] track2 = new byte[len];
                        Array.Copy(RecvBuff, i + 2, track2, 0, len);
                        string track2str = BitConverter.ToString(track2).Replace("-", "");
                        textTrack.Text = track2str;
                        displayOut(0, 0, $"Track2 数据: {track2str}");

                        int dIndex = track2str.IndexOf("D");
                        if (dIndex > 0) {
                            string pan = track2str.Substring(0, dIndex);
                            string expiry = track2str.Substring(dIndex + 1, 4);
                            textCardNum.Text = pan;
                            textEXP.Text = $"20{expiry.Substring(0, 2)}-{expiry.Substring(2)}";
                            displayOut(0, 0, $"卡号: {pan}");
                            displayOut(0, 0, $"有效期: 20{expiry.Substring(0, 2)}-{expiry.Substring(2)}");
                        }
                        return;
                    }
                }
                displayOut(0, 0, "未能从 GPO 响应中获取有效 AFL 或 Track2 数据");
            }
        }


        private void ParseRecordContent(byte[] buffer, long len) {
            for (int i = 0; i < len - 1; i++) {
                // === Track 2 数据 (57) ===
                if (buffer[i] == 0x57) {
                    int tlen = buffer[i + 1];
                    if (i + 2 + tlen > len)
                        continue;
                    byte[] track2 = new byte[tlen];
                    Array.Copy(buffer, i + 2, track2, 0, tlen);
                    string track2str = BitConverter.ToString(track2).Replace("-", "");
                    textTrack.Text = track2str;
                    displayOut(0, 0, $"Track2 数据: {track2str}");

                    int dIndex = track2str.IndexOf("D");
                    if (dIndex > 0) {
                        string pan = track2str.Substring(0, dIndex);
                        string expiry = track2str.Substring(dIndex + 1, 4);
                        textCardNum.Text = pan;
                        textEXP.Text = $"20{expiry.Substring(0, 2)}-{expiry.Substring(2)}";
                        displayOut(0, 0, $"卡号: {pan}");
                        displayOut(0, 0, $"有效期: 20{expiry.Substring(0, 2)}-{expiry.Substring(2)}");
                    }
                }

                // === 持卡人姓名 (5F20) ===
                else if (buffer[i] == 0x5F && buffer[i + 1] == 0x20) {
                    int tlen = buffer[i + 2];
                    if (i + 3 + tlen > len)
                        continue;
                    byte[] nameBytes = new byte[tlen];
                    Array.Copy(buffer, i + 3, nameBytes, 0, tlen);
                    string name = Encoding.ASCII.GetString(nameBytes).Trim();
                    textHolder.Text = name;
                    displayOut(0, 0, $"持卡人姓名: {name}");
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

                        // 填充各类 PDOL 数据
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
                                pdolData.AddRange(new byte[tagLen]); // 填0
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
                        displayOut(0, 0, "GPO 成功返回（带 PDOL）");
                        return true;
                    }
                    else {
                        displayOut(0, 0, "GPO 返回格式异常");
                        return false;
                    }
                }
                else {
                    index++;
                }
            }

            // === 无 PDOL，发送简化 GPO ===
            string gpoEmpty = "80 A8 00 00 02 83 00 00";
            SendLen = FillBufferFromHexString(gpoEmpty, SendBuff, 0);
            RecvLen = 0xFF;
            int res = TransmitWithAutoFix();
            if (res != 0)
                return false;

            if (RecvBuff[0] == 0x80 || RecvBuff[0] == 0x77) {
                displayOut(0, 0, "GPO 成功返回（简化模式）");
                return true;
            }
            else {
                displayOut(0, 0, "简化 GPO 返回格式异常");
                return false;
            }
        }

        private int TransmitWithAutoFix() {
            int result = SendAPDUandDisplay();

            // === 情况 1：SW = 6C XX，表示Le不匹配，需要重新发一次 ===
            if (RecvLen == 2 && RecvBuff[0] == 0x6C) {
                SendBuff[SendLen - 1] = RecvBuff[1]; // 用推荐长度替换 Le
                RecvLen = RecvBuff[1] + 2;
                result = SendAPDUandDisplay();
                return result;
            }

            // === 情况 2：SW = 67 00，表示缺失 Le，也尝试补为 0xFF 再发一次 ===
            if (RecvLen == 2 && RecvBuff[0] == 0x67 && RecvBuff[1] == 0x00) {
                    SendBuff[SendLen - 1] = 0xFF;
                    RecvLen = 0xFF;
                    result = SendAPDUandDisplay();
                    return result;
            }

            // === 情况 3：SW = 61 XX，需要 GET RESPONSE ===
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

            if (buffer[0] == 0x77)  // GPO 返回模板 77
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
            else if (buffer[0] == 0x80)  // GPO 返回模板 80（Visa）
            {
                int totalLen = buffer[1];
                if (totalLen + 2 > buffer.Length)
                    return aflList;

                int pos = 2;
                pos += 2; // 跳过 AIP（2字节）

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

            // === 1. 选择 PSE 应用（1PAY.SYS.DDF01） ===
            string selectPSE = "00 A4 04 00 0E 31 50 41 59 2E 53 59 53 2E 44 44 46 30 31";
            int cmdLen = FillBufferFromHexString(selectPSE, SendBuff, 0);
            SendLen = cmdLen;
            RecvLen = 0xFF;
            int result = TransmitWithAutoFix(); // 自动处理 61
            if (result != 0) {
                displayOut(0, 0, "选择 PSE 应用失败");
                return;
            }

            // === 2. 从 SFI 1 开始逐条读取 Record，直到返回 6A 83 ===
            for (int record = 1; ; record++) {
                string readSFI = $"00 B2 {record:X2} 0C 00"; // SFI=1, P2=0C, Le=00
                SendLen = FillBufferFromHexString(readSFI, SendBuff, 0);
                RecvLen = 0xFF;

                result = TransmitWithAutoFix();

                // 检查是否为“记录不存在”
                if (RecvLen == 2 && RecvBuff[0] == 0x6A && RecvBuff[1] == 0x83) {
                    displayOut(0, 0, $"Record {record} 不存在，结束读取 AID");
                    break;
                }

                // 检查是否为成功
                if (result != 0 || RecvLen < 2 || !(RecvBuff[RecvLen - 2] == 0x90 && RecvBuff[RecvLen - 1] == 0x00)) {
                    displayOut(0, 0, $"Record {record} 读取失败，停止");
                    break;
                }

                displayOut(0, 0, $"解析 Record {record} 中的 AID 信息");
                ParseSFIRecord(RecvBuff, RecvLen - 2); // 忽略尾部 SW1 SW2
            }

            // === 自动选中第一个应用（如有）===
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

            // === 1. 选择 PPSE 应用 ===
            string selectPPSE = "00 A4 04 00 0E 32 50 41 59 2E 53 59 53 2E 44 44 46 30 31";
            int cmdLen = FillBufferFromHexString(selectPPSE, SendBuff, 0);
            SendLen = cmdLen;
            RecvLen = 0xFF;
            int result = TransmitWithAutoFix();
            if (result != 0 || RecvLen < 2 || !(RecvBuff[RecvLen - 2] == 0x90 && RecvBuff[RecvLen - 1] == 0x00)) {
                displayOut(0, 0, "选择 PPSE 应用失败");
                return;
            }

            // === 2. 从返回的 FCI Template 中查找所有 Application Template (61) ===
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

            // === 自动选中第一个 ===
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

                    // 防止异常长度声明（EMV 一般不会超过 3 字节）
                    if (lenLen <= 0 || lenLen > 3) {
                        displayOut(0, 0, $"TLV 长度字段异常：lenLen={lenLen}，index={index}");
                        break;
                    }

                    len = 0;
                    for (int i = 0; i < lenLen; i++) {
                        if (index >= buffer.Length) {
                            displayOut(0, 0, "TLV 长度字段读取越界");
                            break;
                        }
                        len = (len << 8) + buffer[index++];
                    }
                }

                // 安全性检查
                if (len < 0 || len > 4096 || index + len > buffer.Length) {
                    displayOut(0, 0, $"TLV 长度非法：len={len}，index={index}");
                    // 打印当前片段用于调试
                    long printStart = Math.Max(index - 5, 0);
                    long printLen = Math.Min(10, buffer.Length - printStart);
                    displayOut(0, 0, "可疑 TLV 片段: " + BitConverter.ToString(buffer, (int)printStart, (int)printLen));
                    break;
                }

                byte[] value = new byte[len];
                Array.Copy(buffer, index, value, 0, len);
                index += len;

                switch (tagValue) {
                    case 0x5A: // PAN
                        string pan = BitConverter.ToString(value).Replace("-", "");
                        textCardNum.Text = pan;
                        displayOut(0, 0, "卡号(PAN): " + pan);
                        break;

                    case 0x5F24: // Expiry
                        string rawDate = BitConverter.ToString(value).Replace("-", "");
                        if (rawDate.Length >= 4) {
                            string expiry = "20" + rawDate.Substring(0, 2) + "-" + rawDate.Substring(2, 2);
                            textEXP.Text = expiry;
                            displayOut(0, 0, "有效期: " + expiry);
                        }
                        break;

                    case 0x5F20: // Name
                        string name = Encoding.ASCII.GetString(value);
                        textHolder.Text = name;
                        displayOut(0, 0, "持卡人姓名: " + name);
                        break;

                    case 0x57: // Track2
                        string track2 = BitConverter.ToString(value).Replace("-", "");
                        textTrack.Text = track2;
                        displayOut(0, 0, "Track2 数据: " + track2);
                        break;

                    case 0x70: // FCI or EMV Template
                        ParseTLV(value, 0, value.Length); // 递归解析
                        break;

                    default:
                        // 其他不处理，可以根据需要添加更多 tag 支持
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
                ref Protocol,           // 或用 ref proto
                atr,
                ref atrLen
            );

            if (retCode == ModWinsCard64.SCARD_S_SUCCESS) {
                string atrStr = BitConverter.ToString(atr, 0, (int)atrLen);
                displayOut(0, 0, "ATR: " + atrStr);

                // 简单判断是否接触式卡
                if (atrLen > 0 && (atr[0] == 0x3B || atr[0] == 0x3F)) {
                    displayOut(0, 0, "卡片默认在接触式模式工作");
                }
                else {
                    displayOut(0, 0, "卡片默认在非接触式模式工作");
                }
            }
            else {
                displayOut(1, retCode, "无法读取 ATR");
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

                // 处理两字节 Tag（如 9F 12）
                if ((tag & 0x1F) == 0x1F) {
                    tag2 = buffer[index++];
                }

                int tagValue = tag2.HasValue ? (tag << 8 | tag2.Value) : tag;

                // 获取长度
                int len = buffer[index++];
                if (len > 0x80) {
                    int lenLen = len & 0x7F;
                    len = 0;
                    for (int i = 0; i < lenLen; i++)
                        len = (len << 8) + buffer[index++];
                }

                // 获取 Value
                byte[] value = new byte[len];
                Array.Copy(buffer, index, value, 0, len);
                index += len;

                // 解析并打印常见字段
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
                        // 其他字段可根据需要添加
                        break;
                }
            }
        }

        public int FillBufferFromHexString(string hexString, byte[] buffer, int startIndex) {
            if (string.IsNullOrWhiteSpace(hexString))
                throw new ArgumentException("输入字符串不能为空", nameof(hexString));
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (startIndex < 0 || startIndex >= buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex), "起始位置超出缓冲区范围");

            string[] hexValues = hexString.Split(' ', (char)StringSplitOptions.RemoveEmptyEntries);
            int byteCount = hexValues.Length;

            if (startIndex + byteCount > buffer.Length)
                throw new ArgumentException("buffer 不够大，无法容纳所有数据");

            for (int i = 0; i < byteCount; i++) {
                if (!byte.TryParse(hexValues[i], System.Globalization.NumberStyles.HexNumber, null, out byte result))
                    throw new FormatException($"无法解析 '{hexValues[i]}' 为十六进制字节");
                buffer[startIndex + i] = result;
            }

            return byteCount;
        }

    }

}
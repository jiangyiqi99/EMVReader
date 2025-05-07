/*=========================================================================================
'   Author :  Aileen Grace L. Sarte
'
'   Module :  ModWinscard.cs
'
'   Company:  Advanced Card Systems Ltd.
'
'   Date   :  October 14, 2006
'
'   Revision  :
'               Name                    Date                Brief Description
'               Aileen Grace L. Sarte   May 27, 2008        Added IOCTL_SMARTCARD_ACR128_ESCAPE_COMMAND
'                                                           constant for ACR128)
'
'               Gil Bagaporo            April 20, 2010      Modified and added error code values and messages. 
'                                                           Modified for 64-Bit.
'=========================================================================================*/

using System;
using System.Text;
using System.Runtime.InteropServices;

	
	public class ModWinsCard64
	{
		[StructLayout(LayoutKind.Sequential)]	
			public struct SCARD_IO_REQUEST  	
		{
			public int dwProtocol;
			public int cbPciLength;
		}

		[StructLayout(LayoutKind.Sequential)]
			public struct APDURec
		{
			public byte bCLA; 
			public byte bINS; 
			public byte bP1; 
			public byte bP2; 
			public byte bP3; 
			[MarshalAs(UnmanagedType.ByValArray, SizeConst=256)]
			public byte[] Data;                       
			[MarshalAs(UnmanagedType.ByValArray, SizeConst=3)]
			public byte[] SW;                          
			public bool IsSend; 
		}

		[StructLayout(LayoutKind.Sequential)]
			public struct SCARD_READERSTATE
		{
            public string RdrName;
			public Int64 UserData;      
			public uint   RdrCurrState;  
			public uint   RdrEventState;  
			public uint   ATRLength;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst=37)]
			public byte[] ATRValue;
		}
	
		public const int SCARD_S_SUCCESS = 0;
		public const int SCARD_ATR_LENGTH = 33;


		/* ===========================================================
		'  Memory Card type constants
		'===========================================================*/
		public const int CT_MCU = 0x00;                    // MCU
		public const int CT_IIC_Auto = 0x01;               // IIC (Auto Detect Memory Size)
		public const int CT_IIC_1K = 0x02;                 // IIC (1K)
		public const int CT_IIC_2K = 0x03;                 // IIC (2K)
		public const int CT_IIC_4K = 0x04;                 // IIC (4K)
		public const int CT_IIC_8K = 0x05;                 // IIC (8K)
		public const int CT_IIC_16K = 0x06;                // IIC (16K)
		public const int CT_IIC_32K = 0x07;                // IIC (32K)
		public const int CT_IIC_64K = 0x08;                // IIC (64K)
		public const int CT_IIC_128K = 0x09;               // IIC (128K)
		public const int CT_IIC_256K = 0x0A;               // IIC (256K)
		public const int CT_IIC_512K = 0x0B;               // IIC (512K)
		public const int CT_IIC_1024K = 0x0C;              // IIC (1024K)
		public const int CT_AT88SC153 = 0x0D;              // AT88SC153
		public const int CT_AT88SC1608 = 0x0E;             // AT88SC1608
		public const int CT_SLE4418 = 0x0F;                // SLE4418
		public const int CT_SLE4428 = 0x10;                // SLE4428
		public const int CT_SLE4432 = 0x11;                // SLE4432
		public const int CT_SLE4442 = 0x12;                // SLE4442
		public const int CT_SLE4406 = 0x13;                // SLE4406
		public const int CT_SLE4436 = 0x14;                // SLE4436
		public const int CT_SLE5536 = 0x15;                // SLE5536
		public const int CT_MCUT0 = 0x16;                  // MCU T=0
		public const int CT_MCUT1 = 0x17;                  // MCU T=1
		public const int CT_MCU_Auto = 0x18;               // MCU Autodetect

		/*===============================================================
		' CONTEXT SCOPE
		===============================================================	*/
		public const int SCARD_SCOPE_USER = 0;
		/*===============================================================
		' The context is a user context, and any database operations 
		'  are performed within the domain of the user.
		'===============================================================*/
		public const  int SCARD_SCOPE_TERMINAL = 1;
		/*===============================================================
		' The context is that of the current terminal, and any database 
		'operations are performed within the domain of that terminal.  
		'(The calling application must have appropriate access permissions 
		'for any database actions.)
		'===============================================================*/
		
		public const int  SCARD_SCOPE_SYSTEM = 2;
		/*===============================================================
		' The context is the system context, and any database operations 
		' are performed within the domain of the system.  (The calling
		' application must have appropriate access permissions for any 
		' database actions.)
		'===============================================================*/
		/*=============================================================== 
		' Context Scope
		'===============================================================*/ 
		public const int  SCARD_STATE_UNAWARE = 0x00;
		/*===============================================================
		' The application is unaware of the current state, and would like 
		' to know. The use of this value results in an immediate return
		' from state transition monitoring services. This is represented
		' by all bits set to zero.
		'===============================================================*/
		public const int SCARD_STATE_IGNORE = 0x01;
		/*===============================================================
		' The application requested that this reader be ignored. No other
		' bits will be set.
		'===============================================================*/
		
		public const int SCARD_STATE_CHANGED = 0x02;
		/*===============================================================
		' This implies that there is a difference between the state 
		' believed by the application, and the state known by the Service
		' Manager.When this bit is set, the application may assume a
		' significant state change has occurred on this reader.
		'===============================================================*/

		public const int SCARD_STATE_UNKNOWN = 0x04;
		/*===============================================================
		' This implies that the given reader name is not recognized by
		' the Service Manager. If this bit is set, then SCARD_STATE_CHANGED
		' and SCARD_STATE_IGNORE will also be set.
		'===============================================================*/
		public const int SCARD_STATE_UNAVAILABLE = 0x08;
		/*===============================================================
		' This implies that the actual state of this reader is not
		' available. If this bit is set, then all the following bits are
		' clear.
		'===============================================================*/
		public const int SCARD_STATE_EMPTY = 0x10;
		/*===============================================================
		'  This implies that there is not card in the reader.  If this bit
		'  is set, all the following bits will be clear.
		 ===============================================================*/
		public const int SCARD_STATE_PRESENT = 0x20;
		/*===============================================================
		'  This implies that there is a card in the reader.
		 ===============================================================*/
		public const int SCARD_STATE_ATRMATCH = 0x40;
		/*===============================================================
		'  This implies that there is a card in the reader with an ATR
		'  matching one of the target cards. If this bit is set,
		'  SCARD_STATE_PRESENT will also be set.  This bit is only returned
		'  on the SCardLocateCard() service.
		 ===============================================================*/
		public const int SCARD_STATE_EXCLUSIVE = 0x80;
		/*===============================================================
		'  This implies that the card in the reader is allocated for 
		'  exclusive use by another application. If this bit is set,
		'  SCARD_STATE_PRESENT will also be set.
		 ===============================================================*/
		public const int SCARD_STATE_INUSE = 0x100;
		/*===============================================================
		'  This implies that the card in the reader is in use by one or 
		'  more other applications, but may be connected to in shared mode. 
		'  If this bit is set, SCARD_STATE_PRESENT will also be set.
		 ===============================================================*/
		public const int SCARD_STATE_MUTE = 0x200;
		/*===============================================================
		'  This implies that the card in the reader is unresponsive or not
		'  supported by the reader or software.
		' ===============================================================*/
		public const int SCARD_STATE_UNPOWERED = 0x400;
		/*===============================================================
		'  This implies that the card in the reader has not been powered up.
		' ===============================================================*/

		public const int SCARD_SHARE_EXCLUSIVE = 1;
		/*===============================================================
		' This application is not willing to share this card with other 
		'applications.
		'===============================================================*/
		public const int  SCARD_SHARE_SHARED = 2;
		/*===============================================================
		' This application is willing to share this card with other 
		'applications.
		'===============================================================*/
		public const int SCARD_SHARE_DIRECT = 3;
		/*===============================================================
		' This application demands direct control of the reader, so it 
		' is not available to other applications.
		'===============================================================*/

		/*===========================================================
		'   Disposition
		'===========================================================*/
		public const int SCARD_LEAVE_CARD =   0;   // Don't do anything special on close
		public const int SCARD_RESET_CARD =   1;   // Reset the card on close
		public const int SCARD_UNPOWER_CARD = 2;   // Power down the card on close
		public const int SCARD_EJECT_CARD =   3;   // Eject the card on close


		/* ===========================================================
		' ACS IOCTL class
		'===========================================================*/
		public const long FILE_DEVICE_SMARTCARD = 0x310000; // Reader action IOCTLs
		
		public const long IOCTL_SMARTCARD_DIRECT  = FILE_DEVICE_SMARTCARD + 2050 * 4;
		public const long IOCTL_SMARTCARD_SELECT_SLOT = FILE_DEVICE_SMARTCARD + 2051 * 4;
		public const long IOCTL_SMARTCARD_DRAW_LCDBMP  = FILE_DEVICE_SMARTCARD + 2052 * 4;
		public const long IOCTL_SMARTCARD_DISPLAY_LCD  = FILE_DEVICE_SMARTCARD + 2053 * 4;
		public const long IOCTL_SMARTCARD_CLR_LCD  = FILE_DEVICE_SMARTCARD + 2054 * 4;
		public const long IOCTL_SMARTCARD_READ_KEYPAD  = FILE_DEVICE_SMARTCARD + 2055 * 4;
		public const long IOCTL_SMARTCARD_READ_RTC  = FILE_DEVICE_SMARTCARD + 2057 * 4;
		public const long IOCTL_SMARTCARD_SET_RTC  = FILE_DEVICE_SMARTCARD + 2058 * 4;
		public const long IOCTL_SMARTCARD_SET_OPTION  = FILE_DEVICE_SMARTCARD + 2059 * 4;
		public const long IOCTL_SMARTCARD_SET_LED  = FILE_DEVICE_SMARTCARD + 2060 * 4;
		public const long IOCTL_SMARTCARD_LOAD_KEY  = FILE_DEVICE_SMARTCARD + 2062 * 4;
		public const long IOCTL_SMARTCARD_READ_EEPROM  = FILE_DEVICE_SMARTCARD + 2065 * 4;
		public const long IOCTL_SMARTCARD_WRITE_EEPROM  = FILE_DEVICE_SMARTCARD + 2066 * 4;
		public const long IOCTL_SMARTCARD_GET_VERSION  = FILE_DEVICE_SMARTCARD + 2067 * 4;
		public const long IOCTL_SMARTCARD_GET_READER_INFO = FILE_DEVICE_SMARTCARD + 2051 * 4;
		public const long IOCTL_SMARTCARD_SET_CARD_TYPE  = FILE_DEVICE_SMARTCARD + 2060 * 4;
        public const long IOCTL_SMARTCARD_ACR128_ESCAPE_COMMAND = FILE_DEVICE_SMARTCARD + 2079 * 4;

		/*===========================================================
		'   Error Codes
		'===========================================================*/
        public const int SCARD_F_INTERNAL_ERROR                 = -2146435071; //  An internal consistency check failed.  
        public const int SCARD_E_CANCELLED                      = -2146435070; //  The action was cancelled by an SCardCancel request.  
        public const int SCARD_E_INVALID_HANDLE                 = -2146435069; //  The supplied handle was invalid.  
        public const int SCARD_E_INVALID_PARAMETER              = -2146435068; //  One or more of the supplied parameters could not be properly interpreted.  
        public const int SCARD_E_INVALID_TARGET                 = -2146435067; //  Registry startup information is missing or invalid. 
        public const int SCARD_E_NO_MEMORY                      = -2146435066; //  Not enough memory available to complete this command. 
        public const int SCARD_F_WAITED_TOO_LONG                = -2146435065; //  An internal consistency timer has expired. 
        public const int SCARD_E_INSUFFICIENT_BUFFER            = -2146435064; //  The data buffer to receive returned data is too small for the returned data.
        public const int SCARD_E_UNKNOWN_READER                 = -2146435063; //  The specified reader name is not recognized.
        public const int SCARD_E_TIMEOUT                        = -2146435062; //  The user-specified timeout value has expired.
        public const int SCARD_E_SHARING_VIOLATION              = -2146435061; //  The smart card cannot be accessed because of other connections outstanding.
        public const int SCARD_E_NO_SMARTCARD                   = -2146435060; //  The operation requires a Smart Card, but no Smart Card is currently in the device.
        public const int SCARD_E_UNKNOWN_CARD                   = -2146435059; //  The specified smart card name is not recognized.
        public const int SCARD_E_CANT_DISPOSE                   = -2146435058; //  The system could not dispose of the media in the requested manner.
        public const int SCARD_E_PROTO_MISMATCH                 = -2146435057; //  The requested protocols are incompatible with the protocol currently in use with the smart card.
        public const int SCARD_E_NOT_READY                      = -2146435056; //  The reader or smart card is not ready to accept commands.
        public const int SCARD_E_INVALID_VALUE                  = -2146435055; //  One or more of the supplied parameters values could not be properly interpreted.
        public const int SCARD_E_SYSTEM_CANCELLED               = -2146435054; //  The action was cancelled by the system, presumably to log off or shut down.
        public const int SCARD_F_COMM_ERROR                     = -2146435053; //  An internal communications error has been detected.
        public const int SCARD_F_UNKNOWN_ERROR                  = -2146435052; //  An internal error has been detected, but the source is unknown.
        public const int SCARD_E_INVALID_ATR                    = -2146435051; //  An ATR obtained from the registry is not a valid ATR string.
        public const int SCARD_E_NOT_TRANSACTED                 = -2146435050; //  An attempt was made to end a non-existent transaction.
        public const int SCARD_E_READER_UNAVAILABLE             = -2146435049; //  The specified reader is not currently available for use.
        public const int SCARD_P_SHUTDOWN                       = -2146435048; //  The operation has been aborted to allow the server application to exit.
        public const int SCARD_E_PCI_TOO_SMALL                  = -2146435047; //  The PCI Receive buffer was too small.
        public const int SCARD_E_READER_UNSUPPORTED             = -2146435046; //  The reader driver does not meet minimal requirements for support.
        public const int SCARD_E_DUPLICATE_READER               = -2146435045; //  The reader driver did not produce a unique reader name.
        public const int SCARD_E_CARD_UNSUPPORTED               = -2146435044; //  The smart card does not meet minimal requirements for support.
        public const int SCARD_E_NO_SERVICE                     = -2146435043; //  The Smart card resource manager is not running.
        public const int SCARD_E_SERVICE_STOPPED                = -2146435042; //  The Smart card resource manager has shut down.
        public const int SCARD_E_UNEXPECTED                     = -2146435041; //  An unexpected card error has occurred.
        public const int SCARD_E_ICC_INSTALLATION               = -2146435040; //  No Primary Provider can be found for the smart card.
        public const int SCARD_E_ICC_CREATEORDER                = -2146435039; //  The requested order of object creation is not supported.
        public const int SCARD_E_UNSUPPORTED_FEATURE            = -2146435038; //  This smart card does not support the requested feature.
        public const int SCARD_E_DIR_NOT_FOUND                  = -2146435037; //  The identified directory does not exist in the smart card.
        public const int SCARD_E_FILE_NOT_FOUND                 = -2146435036; //  The identified file does not exist in the smart card.
        public const int SCARD_E_NO_DIR                         = -2146435035; //  The supplied path does not represent a smart card directory.
        public const int SCARD_E_NO_FILE                        = -2146435034; //  The supplied path does not represent a smart card file.
        public const int SCARD_E_NO_ACCESS                      = -2146435033; //  Access is denied to this file.
        public const int SCARD_E_WRITE_TOO_MANY                 = -2146435032; //  The smartcard does not have enough memory to store the information.
        public const int SCARD_E_BAD_SEEK                       = -2146435031; //  There was an error trying to set the smart card file object pointer.
        public const int SCARD_E_INVALID_CHV                    = -2146435030; //  The supplied PIN is incorrect.
        public const int SCARD_E_UNKNOWN_RES_MNG                = -2146435029; //  An unrecognized error code was returned from a layered component.
        public const int SCARD_E_NO_SUCH_CERTIFICATE            = -2146435028; //  The requested certificate does not exist.
        public const int SCARD_E_CERTIFICATE_UNAVAILABLE        = -2146435027; //  The requested certificate could not be obtained.
        public const int SCARD_E_NO_READERS_AVAILABLE           = -2146435026; //  Cannot find a smart card reader.
        public const int SCARD_E_COMM_DATA_LOST                 = -2146435025; //  A communications error with the smart card has been detected.  Retry the operation.
        public const int SCARD_E_NO_KEY_CONTAINER               = -2146435024; //  The requested key container does not exist on the smart card.
        public const int SCARD_E_SERVER_TOO_BUSY                = -2146435023; //  The Smart card resource manager is too busy to complete this operation.
        // These are warning codes.
        public const int SCARD_W_UNSUPPORTED_CARD               = -2146434971; //  The reader cannot communicate with the smart card, due to ATR configuration conflicts.
        public const int SCARD_W_UNRESPONSIVE_CARD              = -2146434970; //  The smart card is not responding to a reset.
        public const int SCARD_W_UNPOWERED_CARD                 = -2146434969; //  Power has been removed from the smart card, so that further communication is not possible.
        public const int SCARD_W_RESET_CARD                     = -2146434968; //  The smart card has been reset, so any shared state information is invalid.
        public const int SCARD_W_REMOVED_CARD                   = -2146434967; //  The smart card has been removed, so that further communication is not possible.
        public const int SCARD_W_SECURITY_VIOLATION             = -2146434966; //  Access was denied because of a security violation.
        public const int SCARD_W_WRONG_CHV                      = -2146434965; //  The card cannot be accessed because the wrong PIN was presented.
        public const int SCARD_W_CHV_BLOCKED                    = -2146434964; //  The card cannot be accessed because the maximum number of PIN entry attempts has been reached.
        public const int SCARD_W_EOF                            = -2146434963; //  The end of the smart card file has been reached.
        public const int SCARD_W_CANCELLED_BY_USER              = -2146434962; //  The action was cancelled by the user.
        public const int SCARD_W_CARD_NOT_AUTHENTICATED         = -2146434961; //  No PIN was presented to the smart card.
        public const int SCARD_W_CACHE_ITEM_NOT_FOUND           = -2146434960; //  The requested item could not be found in the cache.
        public const int SCARD_W_CACHE_ITEM_STALE               = -2146434959; //  The requested cache item is too old and was deleted from the cache.
        public const int SCARD_W_CACHE_ITEM_TOO_BIG             = -2146434958; //  The new cache item exceeds the maximum per-item size defined for the cache.

		/*===========================================================
		'   PROTOCOL
		'===========================================================*/
		public const int SCARD_PROTOCOL_UNDEFINED = 0x00;          // There is no active protocol.
		public const int SCARD_PROTOCOL_T0 = 0x01;                 // T=0 is the active protocol.
		public const int SCARD_PROTOCOL_T1 = 0x02;                 // T=1 is the active protocol.
		public const int SCARD_PROTOCOL_RAW = 0x10000;             // Raw is the active protocol.
		//public const int SCARD_PROTOCOL_DEFAULT = 0x80000000;      // Use implicit PTS.
		/*===========================================================
		'   READER STATE
		'===========================================================*/
		public const int SCARD_UNKNOWN = 0;
		/*===============================================================
		' This value implies the driver is unaware of the current 
		' state of the reader.
		'===============================================================*/
		public const int SCARD_ABSENT = 1;
		/*===============================================================
		' This value implies there is no card in the reader.
		'===============================================================*/
		public const int SCARD_PRESENT = 2;
		/*===============================================================
		' This value implies there is a card is present in the reader, 
		'but that it has not been moved into position for use.
		'===============================================================*/
		public const int SCARD_SWALLOWED = 3;
		/*===============================================================
		' This value implies there is a card in the reader in position 
		' for use.  The card is not powered.
		'===============================================================*/
		public const int SCARD_POWERED = 4;
		/*===============================================================
		' This value implies there is power is being provided to the card, 
		' but the Reader Driver is unaware of the mode of the card.
		'===============================================================*/
		public const int SCARD_NEGOTIABLE = 5;
		/*===============================================================
		' This value implies the card has been reset and is awaiting 
		' PTS negotiation.
		'===============================================================*/
		public const int SCARD_SPECIFIC = 6;
		/*===============================================================
		' This value implies the card has been reset and specific 
		' communication protocols have been established.
		'===============================================================*/

		/*==========================================================================
		' Prototypes
		'==========================================================================*/


		[DllImport("winscard.dll")]
		public static extern int SCardEstablishContext(int dwScope, int pvReserved1, int pvReserved2, ref Int64 phContext);
		
		[DllImport("winscard.dll")]
		public static extern int SCardReleaseContext(Int64 phContext);
		
		[DllImport("winscard.dll")]
		public static extern int SCardConnect(Int64 hContext, string szReaderName, int dwShareMode, int dwPrefProtocol, ref Int64 phCard, ref int ActiveProtocol);
	
		[DllImport("winscard.dll")]
		public static extern int SCardBeginTransaction (Int64 hCard);

		[DllImport("winscard.dll")]
		public static extern int SCardDisconnect(Int64 hCard, int Disposition);
		
		[DllImport("winscard.dll")]
		public static extern int SCardListReaderGroups(Int64 hContext, ref string mzGroups, ref int pcchGroups);
		
		[DllImport("winscard.dll")]
		public static extern int SCardListReaders(Int64 hContext, string mzGroup, ref string ReaderList, ref int pcchReaders);
        
        //Redefinition of SCardListReaders
        [DllImport("winscard.DLL", EntryPoint = "SCardListReadersA", CharSet = CharSet.Ansi)]
        public static extern int SCardListReaders(
            Int64 hContext,
            byte[] Groups,
            byte[] Readers,
            ref int pcchReaders
            );

		[DllImport("winscard.dll")]
        public static extern int SCardStatus(Int64 hCard, string szReaderName, ref int pcchReaderLen, ref int State, ref int Protocol, byte[] ATR, ref Int64 ATRLen);
		
		[DllImport("winscard.dll")]
		public static extern int SCardEndTransaction (Int64 hCard, int Disposition);
		
		[DllImport("winscard.dll")]
		public static extern int SCardState (Int64 hCard, ref Int64 State, ref Int64 Protocol, byte[] ATR, ref Int64 ATRLen); 

		[DllImport("winscard.dll")]
        public static extern int SCardTransmit(Int64 hCard, ref SCARD_IO_REQUEST pioSendRequest, byte[] SendBuff, Int64 SendBuffLen, ref SCARD_IO_REQUEST pioRecvRequest, byte[] RecvBuff, ref Int64 RecvBuffLen);

        [DllImport("winscard.dll")]
        public static extern int SCardGetStatusChange(Int64 hContext, int TimeOut, ref SCARD_READERSTATE ReaderState, int ReaderCount); 

		public static string GetScardErrMsg(int ReturnCode)
		{
			switch(ReturnCode)
			{
	            case SCARD_F_INTERNAL_ERROR:
		            return ("An internal consistency check failed.");
		            
	            case SCARD_E_CANCELLED:
		            return ("The action was cancelled by an SCardCancel request.");
		            
	            case SCARD_E_INVALID_HANDLE:
		            return ("The supplied handle was invalid.");
		            
	            case SCARD_E_INVALID_PARAMETER:
		            return ("One or more of the supplied parameters could not be properly interpreted.");
		            
	            case SCARD_E_INVALID_TARGET:
		            return ("Registry startup information is missing or invalid.");
		            
	            case SCARD_E_NO_MEMORY:
		            return ("Not enough memory available to complete this command.");
		            
	            case SCARD_F_WAITED_TOO_LONG:
		            return ("An internal consistency timer has expired.");
		            
	            case SCARD_E_INSUFFICIENT_BUFFER:
		            return ("The data buffer to receive returned data is too small for the returned data.");
		            
	            case SCARD_E_UNKNOWN_READER:
		            return ("The specified reader name is not recognized.");
		            
	            case SCARD_E_TIMEOUT:
		            return ("The user-specified timeout value has expired.");
		            
	            case SCARD_E_SHARING_VIOLATION:
		            return ("The smart card cannot be accessed because of other connections outstanding.");
		            
	            case SCARD_E_NO_SMARTCARD:
		            return ("The operation requires a Smart Card, but no Smart Card is currently in the device.");
		            
	            case SCARD_E_UNKNOWN_CARD:
		            return ("The specified smart card name is not recognized.");
		            
	            case SCARD_E_CANT_DISPOSE:
		            return ("The system could not dispose of the media in the requested manner.");
		            
	            case SCARD_E_PROTO_MISMATCH:
		            return ("The requested protocols are incompatible with the protocol currently in use with the smart card.");
		            
	            case SCARD_E_NOT_READY:
		            return ("The reader or smart card is not ready to accept commands.");
		            
	            case SCARD_E_INVALID_VALUE:
		            return ("One or more of the supplied parameters values could not be properly interpreted.");
		            
	            case SCARD_E_SYSTEM_CANCELLED:
		            return ("The action was cancelled by the system, presumably to log off or shut down.");
		            
	            case SCARD_F_COMM_ERROR:
		            return ("An internal communications error has been detected.");
		            
	            case SCARD_F_UNKNOWN_ERROR:
		            return ("An internal error has been detected, but the source is unknown.");
		            
	            case SCARD_E_INVALID_ATR:
		            return ("An ATR obtained from the registry is not a valid ATR string.");
		            
	            case SCARD_E_NOT_TRANSACTED:
		            return ("An attempt was made to end a non-existent transaction.");
		            
	            case SCARD_E_READER_UNAVAILABLE:
		            return ("The specified reader is not currently available for use.");
		            
	            case SCARD_P_SHUTDOWN:
		            return ("The operation has been aborted to allow the server application to exit.");
		            
	            case SCARD_E_PCI_TOO_SMALL:
		            return ("The PCI Receive buffer was too small.");
		            
	            case SCARD_E_READER_UNSUPPORTED:
		            return ("The reader driver does not meet minimal requirements for support.");
		            
	            case SCARD_E_DUPLICATE_READER:
		            return ("The reader driver did not produce a unique reader name.");
		            
	            case SCARD_E_CARD_UNSUPPORTED:
		            return ("The smart card does not meet minimal requirements for support.");
		            
	            case SCARD_E_NO_SERVICE:
		            return ("The Smart card resource manager is not running.");
		            
	            case SCARD_E_SERVICE_STOPPED:
		            return ("The Smart card resource manager has shut down.");
		            
	            case SCARD_E_UNEXPECTED:
		            return ("An unexpected card error has occurred.");
		            
	            case SCARD_E_ICC_INSTALLATION:
		            return ("No Primary Provider can be found for the smart card.");
		            
	            case SCARD_E_ICC_CREATEORDER:
		            return ("The requested order of object creation is not supported.");
		            
	            case SCARD_E_UNSUPPORTED_FEATURE:
		            return ("This smart card does not support the requested feature.");
		            
	            case SCARD_E_DIR_NOT_FOUND:
		            return ("The identified directory does not exist in the smart card.");
		            
	            case SCARD_E_FILE_NOT_FOUND:
		            return ("The identified file does not exist in the smart card.");
		            
	            case SCARD_E_NO_DIR:
		            return ("The supplied path does not represent a smart card directory.");
		            
	            case SCARD_E_NO_FILE:
		            return ("The supplied path does not represent a smart card file.");
		            
	            case SCARD_E_NO_ACCESS:
		            return ("Access is denied to this file.");
		            
	            case SCARD_E_WRITE_TOO_MANY:
		            return ("The smartcard does not have enough memory to store the information.");
		            
	            case SCARD_E_BAD_SEEK:
		            return ("There was an error trying to set the smart card file object pointer.");
		            
	            case SCARD_E_INVALID_CHV:
		            return ("The supplied PIN is incorrect.");
		            
	            case SCARD_E_UNKNOWN_RES_MNG:
		            return ("An unrecognized error code was returned from a layered component.");
		            
	            case SCARD_E_NO_SUCH_CERTIFICATE:
		            return ("The requested certificate does not exist.");
		            
	            case SCARD_E_CERTIFICATE_UNAVAILABLE:
		            return ("The requested certificate could not be obtained.");
		            
	            case SCARD_E_NO_READERS_AVAILABLE:
		            return ("Cannot find a smart card reader.");
		            
	            case SCARD_E_COMM_DATA_LOST:
		            return ("A communications error with the smart card has been detected.  Retry the operation.");
		            
	            case SCARD_E_NO_KEY_CONTAINER:
		            return ("The requested key container does not exist on the smart card.");
		            
	            case SCARD_E_SERVER_TOO_BUSY:
		            return ("The Smart card resource manager is too busy to complete this operation.");
		            
                    // Warning codes
	            case SCARD_W_UNSUPPORTED_CARD:
		            return ("The reader cannot communicate with the smart card, due to ATR configuration conflicts.");
		            
	            case SCARD_W_UNRESPONSIVE_CARD:
		            return ("The smart card is not responding to a reset.");
		            
	            case SCARD_W_UNPOWERED_CARD:
		            return ("Power has been removed from the smart card, so that further communication is not possible.");
		            
	            case SCARD_W_RESET_CARD:
		            return ("The smart card has been reset, so any shared state information is invalid.");
		            
	            case SCARD_W_REMOVED_CARD:
		            return ("The smart card has been removed, so that further communication is not possible.");
		            
	            case SCARD_W_SECURITY_VIOLATION:
		            return ("Access was denied because of a security violation.");
		            
	            case SCARD_W_WRONG_CHV:
		            return ("The card cannot be accessed because the wrong PIN was presented.");
		            
	            case SCARD_W_CHV_BLOCKED:
		            return ("The card cannot be accessed because the maximum number of PIN entry attempts has been reached.");
		            
	            case SCARD_W_EOF:
		            return ("The end of the smart card file has been reached.");
		            
	            case SCARD_W_CANCELLED_BY_USER:
		            return ("The action was cancelled by the user.");
		            
	            case SCARD_W_CARD_NOT_AUTHENTICATED:
		            return ("No PIN was presented to the smart card.");
		            
	            case SCARD_W_CACHE_ITEM_NOT_FOUND:
		            return ("The requested item could not be found in the cache.");
		            
	            case SCARD_W_CACHE_ITEM_STALE:
		            return ("The requested cache item is too old and was deleted from the cache.");
		            
	            case SCARD_W_CACHE_ITEM_TOO_BIG:
		            return ("The new cache item exceeds the maximum per-item size defined for the cache.");
		            
				default:
					return("? - " + ReturnCode.ToString());
			}
		}

		public ModWinsCard64()
		{
			//
			// TODO: Add constructor logic here
			//
		}


	}


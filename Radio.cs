//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;


namespace Icom_Proxy
{
    class Radio
    {

        public class CIV
        {
            //Address
            public static byte Address = 0x00;

            //PTT
            public static byte[] PttOn  = new byte[] { 0xFE, 0xFE, 0x00, 0xE0, 0x1C, 0x00, 0x01, 0xFD };
            public static byte[] PttOff = new byte[] { 0xFE, 0xFE, 0x00, 0xE0, 0x1C, 0x00, 0x00, 0xFD };

            //Response
            public static byte[] CodeOK = new byte[] { 0xFE, 0xFE, 0xE0, 0x00, 0xFB, 0xFD };
            public static byte[] CodeNG = new byte[] { 0xFE, 0xFE, 0xE0, 0x00, 0xFA, 0xFD };

            //Dummy - Initiate
            //public static byte[] Initiate = new byte[] { 0xFE, 0xFE, 0x00, 0x00, 0xFD };
            //public static byte[] Initiate = new byte[] { 0xFE, 0xFE, 0x00, 0xE0, 0x1C, 0x00, 0x00, 0xFD };
            public static byte[] Initiate = new byte[] { 0xFE, 0xFE, 0x00, 0xE0, 0x19, 0x00, 0xFD };
        }
    }

}


using System;
using System.Runtime.InteropServices;
using System.Text;

// PInvoke Tutorial
// http://msdn.microsoft.com/en-us/library/aa288468(v=vs.71).aspx

namespace Clearsilver {

    // these allocation functions should be used when we expect the Clearsilver C-dll
    // to take ownership of memory.... this assures the same allocator/decallocator is used

    internal class NeoUtil {
        [DllImport("libneo")]
        internal static extern IntPtr neo_malloc(int length);

        [DllImport("libneo")]
        internal static extern void neo_free(IntPtr buf);

        internal static unsafe string PtrToStringUTF8(STR* buf) {
            byte* walk = (byte *)buf;
            // byte* buf2 = stackalloc byte[4000];

            // find the end of the string
            while (*walk != 0) {
                walk++;
            }
            int length = (int)(walk - (byte *)buf);

            string data;
            byte[] strbuf = new byte[length];  // should not be null terminated
            if (length > 0) {
                Marshal.Copy((IntPtr)buf, strbuf, 0, length);
                data = Encoding.UTF8.GetString(strbuf);
            } else {
                data = "";
            }            

            return data;                       
        }
    }

    internal class UTF8Marshaler : ICustomMarshaler {
        static UTF8Marshaler static_instance;

        public IntPtr MarshalManagedToNative(object managedObj) {
            if (managedObj == null)
                return IntPtr.Zero;
            if (!(managedObj is string))
                throw new MarshalDirectiveException("UTF8Marshaler must be used on a string.");

            byte[] strbuf = Encoding.UTF8.GetBytes((string)managedObj); // not null terminated
            IntPtr buffer = Marshal.AllocHGlobal(strbuf.Length + 1);            
            Marshal.Copy(strbuf, 0, buffer, strbuf.Length);
            Marshal.WriteByte((IntPtr)((uint)buffer + strbuf.Length), 0); // write the terminating null

            return buffer;
        }

        public unsafe object MarshalNativeToManaged(IntPtr pNativeData) {
            byte* walk = (byte*)pNativeData;

            // find the end of the string
            while (*walk != 0) {
                walk++;
            }
            int length = (int)(walk - (byte*)pNativeData);

            byte[] strbuf = new byte[length - 1];  // should not be null terminated
            Marshal.Copy((IntPtr)pNativeData, strbuf, 0, length - 1); // skip the trailing null
            string data = Encoding.UTF8.GetString(strbuf);
            return data;
        }

        public void CleanUpNativeData(IntPtr pNativeData) {
            Marshal.FreeHGlobal(pNativeData);            
        }

        public void CleanUpManagedData(object managedObj) {
        }

        public int GetNativeDataSize() {
            return -1;
        }

        public static ICustomMarshaler GetInstance(string cookie) {
            if (static_instance == null) {
                return static_instance = new UTF8Marshaler();
            }

            return static_instance;
        }
    }

}

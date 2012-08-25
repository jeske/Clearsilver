
using System;
using System.Runtime.InteropServices;




// PInvoke Tutorial
// http://msdn.microsoft.com/en-us/library/aa288468(v=vs.71).aspx

namespace Clearsilver {
    // opaque types
    internal unsafe struct HDF { };

    public unsafe class Hdf : IDisposable {

        internal unsafe HDF* hdf_root;

        public Hdf() {
            fixed (HDF** hdf_ptr = &hdf_root) {
                hdf_init(hdf_ptr);
            }

            // Console.WriteLine("Hdf.Hdf() hdf_root = {0}",(int)hdf_root);
        }

        // this is used by callbacks and other elements that get an HDF pointer
        internal unsafe Hdf(HDF* from_hdf) {
            hdf_root = from_hdf;
        }

        #region Most DLL Imports

        [DllImport("libneo", EntryPoint = "hdf_init")]
        private static extern unsafe NEOERR* hdf_init(HDF** foo);

        // NEOERR* hdf_set_value (HDF *hdf, char *name, char *value)
        [DllImport("libneo")]
        private static unsafe extern NEOERR* hdf_set_value(HDF* hdf,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8Marshaler))] string name,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8Marshaler))] string value);

        // char* hdf_get_value (HDF *hdf, char *name, char *defval)

        [DllImport("libneo")]
        // [return: MarshalAs(UnmanagedType.LPStr)] 
        private static unsafe extern STR* hdf_get_value(HDF* hdf,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8Marshaler))] string name,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8Marshaler))] string defval);

        // NEOERR* hdf_dump (HDF *hdf, char *prefix);

        [DllImport("libneo")]
        private static extern void hdf_dump(HDF* hdf,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8Marshaler))]string prefix);

        [DllImport("libneo")]
        private static extern NEOERR* hdf_read_file(HDF* hdf, [MarshalAs(UnmanagedType.LPStr)] string fname);

        // HDF* hdf_get_obj (HDF *hdf, char *name)

        [DllImport("libneo")]
        private static extern HDF* hdf_get_obj(HDF* hdf, 
            [MarshalAs(UnmanagedType.CustomMarshaler,MarshalTypeRef=typeof(UTF8Marshaler))] string name);


        [DllImport("libneo")]
        extern static unsafe void hdf_destroy(HDF** hdf);

        private unsafe void hdfDestroy() {
            fixed (HDF** phdf = &hdf_root) {
                hdf_destroy(phdf);
            }
        }


        #endregion

        #region HDF Register Fileload

        [DllImport("libneo")]
        private static extern void hdf_register_fileload(HDF* hdf, void* ctx,
            [MarshalAs(UnmanagedType.FunctionPtr)] HDFFILELOAD cb);

#if !__MonoCS__
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
#endif
        private unsafe delegate NEOERR* HDFFILELOAD(void* ctx, HDF* hdf, STR* filename, STR** contents);
        // contents is a malloced copy of the file which the parser will own and free

        public delegate byte[] loadFileDelegate(Hdf hdf, string filename);
        loadFileDelegate cur_delegate;
        unsafe HDFFILELOAD thunk_delegate;  // we have to hold onto the delegate to make sure the pinned thunk sticks around

        private unsafe NEOERR* hdfFileLoad(void* ctx, HDF* raw_hdf, STR* pFilename, STR** contents) {
            // Console.WriteLine("hdfFileLoad delegate called");
            IntPtr buf = IntPtr.Zero;
            try {
                Hdf hdf = new Hdf(raw_hdf);
                string filename = Marshal.PtrToStringAnsi((IntPtr)pFilename);                
                byte[] data = this.cur_delegate(hdf, filename);
                byte[] end_null = new byte[] { 0 };                
                buf = NeoUtil.neo_malloc(data.Length + 1);
                Marshal.Copy(data, 0, buf, data.Length);
                Marshal.Copy(end_null, 0, (IntPtr)((uint)buf + data.Length), 1); // write the end_null
                *contents = (STR*)buf;
            } catch (Exception e) {
                // Console.WriteLine("hdfFileLoad Thunk Exception + " + e);
                if (buf != IntPtr.Zero) {
                    NeoUtil.neo_free(buf);                    
                }
                return NeoErr.nERR(e.ToString());
            }
            // Console.WriteLine("hdfFileLoad delegate finishing normally...");
            return (NEOERR*)IntPtr.Zero;
        }

        public unsafe void registerFileLoad(loadFileDelegate fn) {
            if (fn != null) {
                // set the fileload handler
                cur_delegate = fn;
                thunk_delegate = new HDFFILELOAD(hdfFileLoad);

                hdf_register_fileload(this.hdf_root, null, thunk_delegate);
            } else {
                // clear the fileload handler
                hdf_register_fileload(this.hdf_root, null, null);
                cur_delegate = null;
                thunk_delegate = null;
            }
        }

        #endregion

        // -----------------------------------------------------------

        public void setValue(string name, string value) {
            hdf_set_value(hdf_root, name, value);
        }
        public string getValue(string name, string defvalue) {
            STR* x = hdf_get_value(hdf_root, name, defvalue);
            // this allows us to marshall out the string value without freeing it
            string value = Marshal.PtrToStringAnsi((IntPtr)x);
            return value;
        }

        public void readFile(string filename) {
            NeoErr.hNE(hdf_read_file(this.hdf_root, filename));
        }

        public void Dispose() {
            // cleanup the unmanaged data when we are freed
            this.hdfDestroy();
        }

    }

}

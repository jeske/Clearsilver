
using System;
using System.Text;
using System.Runtime.InteropServices;

// PInvoke Tutorial
// http://msdn.microsoft.com/en-us/library/aa288468(v=vs.71).aspx

// TODO: figure out if the default LPStr marshaling does UTF8, and if not fix it so it does


// TODO: is there an issue in handleOutput() because we expect a buffer to be a "complete" UTF8 string 
//       (i.e. can a UTF8 string cross a handleOutput callback?)

namespace Clearsilver {
    // opaque types
    unsafe struct CSPARSE {};

    

    public class CSTContext : IDisposable {
       unsafe CSPARSE *csp;
       unsafe public CSTContext(Hdf hdf) {
         fixed (CSPARSE **csp_ptr = &csp) {
           cs_init(csp_ptr, hdf.hdf_root);       
         }
         // Console.WriteLine("CSt.Cst() hdf_root = {0}", (int)hdf.hdf_root);
       }

       #region Most CS DllImports

       [DllImport("libneo")]
       extern static unsafe NEOERR *cs_init (CSPARSE **parse, HDF *hdf);

       public unsafe void parseFile(string filename) {
           NeoErr.hNE(cs_parse_file(csp, filename));
       }

       [DllImport("libneo")]
       extern static unsafe NEOERR *cs_parse_file (CSPARSE *parse, 
           [MarshalAs(UnmanagedType.LPStr)] 
           string path);

       [DllImport("libneo")]
       extern static unsafe NEOERR *cs_parse_string (CSPARSE *parse,       
                        STR* buffer, 
                        int buf_len);

       public unsafe void parseString(string data) {     
           // neo is going to take ownership of this string, so we need to
           // use neo_malloc to create it. 

           byte[] strbuf = Encoding.UTF8.GetBytes(data); // not null terminated
           IntPtr buffer = NeoUtil.neo_malloc(strbuf.Length + 1);
           Marshal.Copy(strbuf, 0, buffer, strbuf.Length);
           Marshal.WriteByte((IntPtr)((uint)buffer + strbuf.Length), 0); // write the terminating null
           NeoErr.hNE(cs_parse_string(csp, (STR*) buffer, data.Length));
       }

       //  NEOERR *cs_render (CSPARSE *parse, void *ctx, CSOUTFUNC cb);
       //  typedef NEOERR* (*CSOUTFUNC)(void *ctx, char *more_str_bytes);

       [DllImport("libneo")]
       extern static unsafe NEOERR *cs_render (CSPARSE *parse, void *ctx, 
               [MarshalAs(UnmanagedType.FunctionPtr)] CSOUTFUNC cb);

       // about calling convention Cdecl
       // http://www.gamedev.net/community/forums/topic.asp?topic_id=270670

#if  ! __MonoCS__
       [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
#endif
       private unsafe delegate NEOERR* CSOUTFUNC(void* ctx, STR* more_bytes);
       // about GCHandle and pinning delegates
       // http://blogs.msdn.com/b/cbrumme/archive/2003/05/06/51385.aspx


       #endregion


       #region CS File Load Callback

       [DllImport("libneo")]
       extern static unsafe void cs_register_fileload(CSPARSE* csp, void* ctx,
               [MarshalAs(UnmanagedType.FunctionPtr)] CSFILELOAD cb);

#if  ! __MonoCS__
       [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
#endif
       private unsafe delegate NEOERR* CSFILELOAD(void* ctx, HDF* hdf, STR* filename, STR** contents);
       // contents is a malloced copy of the file which clearsilver will own and free

       private class OutputBuilder {
          private string output = "";
       
          public unsafe NEOERR* handleOutput(void* ctx, STR* more_bytes) {                     
              string data = NeoUtil.PtrToStringUTF8(more_bytes);
              output += data;           
              return null;
          }
          public string result() {
             return output;
          }
       }

       public delegate byte[] loadFileDelegate(Hdf hdf, string filename);
       loadFileDelegate cur_delegate;
       unsafe CSFILELOAD thunk_delegate;  // we have to hold onto the delegate to make sure the pinned thunk sticks around
       private unsafe NEOERR* csFileLoad(void* ctx, HDF* raw_hdf, STR* pFilename, STR** contents) {
           // Console.WriteLine("csFileLoad delegate called");
           IntPtr buf = IntPtr.Zero;
           try {
               Hdf hdf = new Hdf(raw_hdf);
               string filename = Marshal.PtrToStringAnsi((IntPtr)pFilename);
               byte[] data = cur_delegate(hdf, filename);
               byte[] end_null = new byte[] { 0 };               
               buf = NeoUtil.neo_malloc(data.Length + 1); // +1 so we can null terminate
               Marshal.Copy(data, 0, buf, data.Length);
               Marshal.Copy(end_null, 0, (IntPtr)((uint)buf + data.Length), 1); // write the end_null
               *contents = (STR*)buf;
           } catch (Exception e) {
               // Console.WriteLine("csFileLoad Thunk Exception + " + e);
               // should return a neo error
               if (buf != IntPtr.Zero) {
                   NeoUtil.neo_free(buf);                   
               }
               return NeoErr.nERR(e.ToString());
           } 
           return (NEOERR*) IntPtr.Zero;
       }

       public unsafe void registerFileLoad(loadFileDelegate fn) {
           if (fn != null) {
               // set the fileload handler
               cur_delegate = fn;
               thunk_delegate = new CSFILELOAD(csFileLoad);

               cs_register_fileload(csp, null, thunk_delegate);
           } else {
               // clear the fileload handler
               cs_register_fileload(csp, null, null);
               cur_delegate = null;
               thunk_delegate = null;
           }
       }

       #endregion

       public unsafe string render() {
         OutputBuilder ob = new OutputBuilder();      
         NeoErr.hNE(cs_render(csp, null, new CSOUTFUNC(ob.handleOutput)));
         return ob.result();
       }

       [DllImport("libneo")]
       extern static unsafe void cs_destroy (CSPARSE **parse);
       private unsafe void csDestroy() {
           fixed (CSPARSE **pcsp = &csp) {
               cs_destroy(pcsp);
           }
       }

       public void Dispose() {
           this.csDestroy();
       }
    }
} // namespace Clearsilver

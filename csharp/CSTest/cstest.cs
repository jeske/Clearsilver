using System;
using System.IO;
using System.Runtime.InteropServices;

using Clearsilver;

public class CSTest {

   public static byte[] loadFileHandler(Hdf hdf, string fname) {
       Console.WriteLine("my load file handler for: " + fname);
       return File.ReadAllBytes(fname);      
   }
   public static int Main(string[] argv) {
     
      Hdf h = new Hdf();

      h.setValue("foo.1","1");
      h.setValue("foo.2","2");

      h.registerFileLoad(new Hdf.loadFileDelegate(loadFileHandler));

      h.readFile("test.hdf");

      Console.WriteLine("foo.2 = {0}", h.getValue("foo.2","def"));

      CSTContext cs = new CSTContext(h);
      
      Console.WriteLine("parsing file");
      h.setValue("hdf.loadpaths.0", ".");
      cs.registerFileLoad(new CSTContext.loadFileDelegate(loadFileHandler));
      try {
          cs.parseFile("test.cst");
      } catch (NeoException e) {
          Console.WriteLine("error: {0}", e.reason);
          Console.WriteLine("tb: {0}", e.full_traceback);
      }

      // cs.parseString(" foo.1 = <?cs var:foo.1 ?> ");
      // cs.parseString("this is a big tesT............ this is a big tesT............ this is a big tesT............ this is a big tesT............ this is a big tesT............ this is a big tesT............ this is a big tesT............ this is a big tesT............ .");

      Console.WriteLine("render file");
      Console.WriteLine(cs.render());
      return 0;
   }
  


}

package org.clearsilver;

import java.io.FileNotFoundException;
import java.io.IOException;

import java.util.Calendar;
import java.util.Date;
import java.util.TimeZone;

/** This class is a wrapper around the HDF C API.  Many features of the C API
 *  are not yet exposed through this wrapper.
 */
public class HDF {
  int hdfptr;  // stores the C HDF* pointer
  HDF root;    // If this is a child HDF node, points at the root node of
               // the tree.  For root nodes this is null.  A child node needs
               // to hold a reference on the root to prevent the root from
               // being GC-ed.
  static {
    JNI.loadLibrary();
  }

  /** Constructs an empty HDF dataset */
  public HDF() {
    hdfptr = _init();
    root = null;
  }

  /** Constructs an HDF child node.  Used by other methods in this class when
   * a child node needs to be constructed.
   */
  private HDF(int hdfptr, HDF parent) {
    this.hdfptr = hdfptr;
    this.root = (parent.root != null) ? parent.root : parent;
  }

  /** Clean up allocated memory if neccesary. close() allows application
   *  to force clean up.
   */
  public void close() {
    // Only root nodes have ownership of the C HDF pointer, so only a root
    // node needs to dealloc hdfptr.dir
    if ( root == null) {
      if (hdfptr != 0) {
        _dealloc(hdfptr);
        hdfptr = 0;
      }
    }
  }

  /** Call close() just in case when deallocating Java object.
   */
  // Should be protected access (like Object).
  protected void finalize() throws Throwable {
    close();
    super.finalize();
  }

  /** Loads the contents of the specified HDF file from disk into the current
   *  HDF object.  The loaded contents are merged with the existing contents.
   */
  public boolean readFile(String filename) throws IOException,
         FileNotFoundException {
    if (hdfptr == 0) {
      throw new NullPointerException("HDF is closed.");
    }
    return _readFile(hdfptr, filename, fileLoader != null);
  }

  protected String fileLoad(String filename) throws IOException,
            FileNotFoundException {
    if (hdfptr == 0) {
      throw new NullPointerException("HDF is closed.");
    }
    CSFileLoader aFileLoader = fileLoader;
    if (aFileLoader == null) {
      throw new NullPointerException("No fileLoader specified.");
    } else {
      String result = aFileLoader.load(this, filename);
      if (result == null) {
        throw new NullPointerException("CSFileLoader.load() returned null");
      }
      return result;
    }
  }

  // The optional CS file loader to use to read in files
  private CSFileLoader fileLoader = null;

  /**
   * Get the file loader in use, if any.
   * @return the file loader in use.
   */
  public CSFileLoader getFileLoader() {
    return fileLoader;
  }

  /**
   * Set the CS file loader to use
   * @param fileLoader the file loader that should be used.
   */
  public void setFileLoader(CSFileLoader fileLoader) {
    this.fileLoader = fileLoader;
  }

  /** Serializes HDF contents to a file (readable by readFile)
   */
  public boolean writeFile(String filename) throws IOException {
    if (hdfptr == 0) {
      throw new NullPointerException("HDF is closed.");
    }
    return _writeFile(hdfptr, filename);
  }

  /** Serializes HDF contents to a file (readable by readFile), but
   *  writes the file atomically by writing to a temp file then doing a
   *  rename(2) on it.
   */
  public boolean writeFileAtomic(String filename) throws IOException {
    if (hdfptr == 0) {
      throw new NullPointerException("HDF is closed.");
    }
    return _writeFileAtomic(hdfptr, filename);
  }

  /** Parses/loads the contents of the given string as HDF into the current
   *  HDF object.  The loaded contents are merged with the existing contents.
   */
  public boolean readString(String data) {
    if (hdfptr == 0) {
      throw new NullPointerException("HDF is closed.");
    }
    return _readString(hdfptr, data);
  }

  /** Serializes HDF contents to a string (readable by readString)
   */
  public String writeString() {
    if (hdfptr == 0) {
      throw new NullPointerException("HDF is closed.");
    }
    return _writeString(hdfptr);
  }

  /** Retrieves the integer value at the specified path in this HDF node's
   *  subtree.  If the value does not exist, or cannot be converted to an
   *  integer, default_value will be returned. */
  public int getIntValue(String hdfname, int default_value) {
    if (hdfptr == 0) {
      throw new NullPointerException("HDF is closed.");
    }
    return _getIntValue(hdfptr,hdfname,default_value);
  }

  /** Retrieves the value at the specified path in this HDF node's subtree.
  */
  public String getValue(String hdfname, String default_value) {
    if (hdfptr == 0) {
      throw new NullPointerException("HDF is closed.");
    }
    return _getValue(hdfptr,hdfname,default_value);
  }

  /** Sets the value at the specified path in this HDF node's subtree. */
  public void setValue(String hdfname, String value) {
    if (hdfptr == 0) {
      throw new NullPointerException("HDF is closed.");
    }
    _setValue(hdfptr,hdfname,value);
  }

  /** Remove the specified subtree. */
  public void removeTree(String hdfname) {
    if (hdfptr == 0) {
      throw new NullPointerException("HDF is closed.");
    }
    _removeTree(hdfptr,hdfname);
  }

  /** Links the src hdf name to the dest. */
  public void setSymLink(String hdf_name_src, String hdf_name_dest) {
    if (hdfptr == 0) {
      throw new NullPointerException("HDF is closed.");
    }
    _setSymLink(hdfptr,hdf_name_src,hdf_name_dest);
  }

  /** Export a date to a clearsilver tree using a specified timezone */
  public void exportDate(String hdfname, TimeZone timeZone, Date date) {
    if (hdfptr == 0) {
      throw new NullPointerException("HDF is closed.");
    }

    Calendar cal = Calendar.getInstance(timeZone);
    cal.setTime(date);

    String sec = Integer.toString(cal.get(Calendar.SECOND));
    setValue(hdfname + ".sec", sec.length() == 1 ? "0" + sec : sec);

    String min = Integer.toString(cal.get(Calendar.MINUTE));
    setValue(hdfname + ".min", min.length() == 1 ? "0" + min : min);

    setValue(hdfname + ".24hour",
             Integer.toString(cal.get(Calendar.HOUR_OF_DAY)));
    // java.util.Calendar uses represents 12 o'clock as 0
    setValue(hdfname + ".hour",
             Integer.toString(
                 cal.get(Calendar.HOUR) == 0 ? 12 : cal.get(Calendar.HOUR)));
    setValue(hdfname + ".am",
             cal.get(Calendar.AM_PM) == Calendar.AM ? "1" : "0");
    setValue(hdfname + ".mday",
             Integer.toString(cal.get(Calendar.DAY_OF_MONTH)));
    setValue(hdfname + ".mon",
             Integer.toString(cal.get(Calendar.MONTH)+1));
    setValue(hdfname + ".year",
             Integer.toString(cal.get(Calendar.YEAR)));
    setValue(hdfname + ".2yr",
             Integer.toString(cal.get(Calendar.YEAR)).substring(2));
    setValue(hdfname + ".wday",
             Integer.toString(cal.get(Calendar.DAY_OF_WEEK)));

    boolean tzNegative = timeZone.getRawOffset() < 0;
    int tzAbsolute = java.lang.Math.abs(timeZone.getRawOffset()/1000);
    String tzHour = Integer.toString(tzAbsolute/3600);
    String tzMin = Integer.toString(tzAbsolute/60 - (tzAbsolute/3600)*60);
    String tzString = (tzNegative ? "-" : "+")
                      + (tzHour.length() == 1 ? "0" + tzHour : tzHour)
                      + (tzMin.length() == 1 ? "0" + tzMin : tzMin);
    setValue(hdfname + ".tzoffset", tzString);
  }

  /** Export a date to a clearsilver tree using a specified timezone */
  public void exportDate(String hdfname, String tz, int tt) {
    if (hdfptr == 0) {
      throw new NullPointerException("HDF is closed.");
    }

    TimeZone timeZone = TimeZone.getTimeZone(tz);

    if (timeZone == null) {
      throw new RuntimeException("Unknown timezone: " + tz);
    }

    Date date = new Date((long)tt * 1000);

    exportDate(hdfname, timeZone, date);
  }

  /** Retrieves the HDF object that is the root of the subtree at hdfpath, or
   *  null if no object exists at that path. */
  public HDF getObj(String hdfpath) {
    if (hdfptr == 0) {
      throw new NullPointerException("HDF is closed.");
    }
    int obj_ptr = _getObj(hdfptr, hdfpath);
    if ( obj_ptr == 0 ) {
      return null;
    }
    return new HDF(obj_ptr, this);
  }

  /** Retrieves the HDF for the first child of the root of the subtree
   *  at hdfpath, or null if no child exists of that path or if the
   *  path doesn't exist. */
  public HDF getChild(String hdfpath) {
    if (hdfptr == 0) {
      throw new NullPointerException("HDF is closed.");
    }
    int obj_ptr = _getChild(hdfptr, hdfpath);
    if ( obj_ptr == 0 ) {
      return null;
    }
    return new HDF(obj_ptr, this);
  }

  /** Return the root of the tree where the current node lies.  If the
   *  current node is the root, return this. */
  public HDF getRootObj() {
    return root != null ? root : this;
  }

  /** Retrieves the HDF object that is the root of the subtree at
   *  hdfpath, create the subtree if it doesn't exist */
  public HDF getOrCreateObj(String hdfpath) {
    if (hdfptr == 0) {
      throw new NullPointerException("HDF is closed.");
    }
    int obj_ptr = _getObj(hdfptr, hdfpath);
    if ( obj_ptr == 0 ) {
      // Create a node
      _setValue(hdfptr, hdfpath, "");
      obj_ptr = _getObj( hdfptr, hdfpath );
      if ( obj_ptr == 0 ) {
        return null;
      }
    }
    return new HDF(obj_ptr, this);
  }

  /** Returns the name of this HDF node.   The root node has no name, so
   *  calling this on the root node will return null. */
  public String objName() {
    if (hdfptr == 0) {
      throw new NullPointerException("HDF is closed.");
    }
    return _objName(hdfptr);
  }

  /** Returns the value of this HDF node, or null if this node has no value.
   *  Every node in the tree can have a value, a child, and a next peer. */
  public String objValue() {
    if (hdfptr == 0) {
      throw new NullPointerException("HDF is closed.");
    }
    return _objValue(hdfptr);
  }

  /** Returns the child of this HDF node, or null if there is no child.
   *  Use this in conjunction with objNext to walk the HDF tree.  Every node
   *  in the tree can have a value, a child, and a next peer. */
  public HDF objChild() {
    if (hdfptr == 0) {
      throw new NullPointerException("HDF is closed.");
    }
    int child_ptr = _objChild(hdfptr);
    if ( child_ptr == 0 ) {
      return null;
    }
    return new HDF(child_ptr, this);
  }

  /** Returns the next sibling of this HDF node, or null if there is no next
   *  sibling.  Use this in conjunction with objChild to walk the HDF tree.
   *  Every node in the tree can have a value, a child, and a next peer. */
  public HDF objNext() {
    if (hdfptr == 0) {
      throw new NullPointerException("HDF is closed.");
    }
    int next_ptr = _objNext(hdfptr);
    if ( next_ptr == 0 ) {
      return null;
    }
    return new HDF(next_ptr, this);
  }

  public void copy(String hdfpath, HDF src) {
    if (hdfptr == 0 || src.hdfptr == 0) {
      throw new NullPointerException("HDF is closed.");
    }
    _copy(hdfptr, hdfpath, src.hdfptr);
  }

  /**
   * Generates a string representing the content of the HDF tree rooted at
   * this node.
   */
  public String dump() {
    if (hdfptr == 0) {
      throw new NullPointerException("HDF is closed.");
    }
    return _dump(hdfptr);
  }

  private static native int _init();
  private static native void _dealloc(int ptr);
  private native boolean _readFile(int ptr, String filename, boolean use_cb);
  private static native boolean _writeFile(int ptr, String filename);
  private static native boolean _writeFileAtomic(int ptr, String filename);
  private static native boolean _readString(int ptr, String data);
  private static native String _writeString(int ptr);
  private static native int _getIntValue(int ptr, String hdfname,
                                         int default_value);
  private static native String _getValue(int ptr, String hdfname,
                                         String default_value);
  private static native void _setValue(int ptr, String hdfname,
                                       String hdf_value);
  private static native void _removeTree(int ptr, String hdfname);
  private static native void _setSymLink(int ptr, String hdf_name_src,
                                       String hdf_name_dest);
  private static native int _getObj(int ptr, String hdfpath);
  private static native int _getChild(int ptr, String hdfpath);
  private static native int _objChild(int ptr);
  private static native int _objNext(int ptr);
  private static native String _objName(int ptr);
  private static native String _objValue(int ptr);
  private static native void _copy(int destptr, String hdfpath, int srcptr);

  private static native String _dump(int ptr);
}

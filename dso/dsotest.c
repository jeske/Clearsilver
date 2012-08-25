
#include <ClearSilver.h>


int main() {
  HDF *hdf;
  NEOERR *err;

  err = hdf_init(&hdf);

  if (err) {
      printf("error: %s\n", err->desc);
      return 1;
   }

   printf("success: 0x%X\n", hdf);


   hdf_set_value(hdf,"a.b.c","somevalue");
   printf("get_value returned: %s", hdf_get_value(hdf,"a.b.c","default"));

}

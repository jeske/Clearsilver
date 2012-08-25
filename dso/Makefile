

ifeq ($(NEOTONIC_ROOT),)
NEOTONIC_ROOT = ..
endif

include $(NEOTONIC_ROOT)/rules.mk

NEO_SO = libneo.so
NEO_STATIC_LIBS =  

LIBS += $(DLIBS) $(DB2_LIB)

ifeq ($(USE_MINGW32),1)
TARGETS = libneo.DLL 
else
TARGETS = $(NEO_SO) dsotest
endif

all: $(TARGETS)

libneo.DLL: $(DEP_LIBS)
	gcc -shared -o libneo.DLL -Wl,--export-all-symbols -Wl,--enable-auto-import -Wl,--whole-archive ../libs/libneo_cgi.a ../libs/libneo_cs.a ../libs/libneo_utl.a -Wl,--no-whole-archive $(LIBS) $(LDFLAGS)

$(NEO_SO): $(NEO_STATIC_LIBS) $(DEP_LIBS) Makefile 
	 ld  -rpath ../libs -shared -o libneo.so -whole-archive ../libs/libneo_cgi.a ../libs/libneo_cs.a ../libs/libneo_utl.a -no-whole-archive $(LDFLAGS) $(LIBS)

#	$(LDSHARED) -o $@ $(LDFLAGS) -Wl,-whole-archive $(DLIBS)

install: all
	$(NEOTONIC_ROOT)/mkinstalldirs $(DESTDIR)$(PYTHON_SITE)
	$(INSTALL) $(TARGETS) $(DESTDIR)$(PYTHONnt_SITE)

dsotest: dsotest.c $(NEO_SO)
	gcc -o dsotest dsotest.c -lneo -L. -I..

dlltest: dsotest.c neo_cs.dll
	gcc -o dlltest dsotest.c -lneo_cs -L. -I..


clean: 
	$(RM) *.o *.DLL

distclean:
	$(RM) Makefile.depends $(TARGETS) *.o


# default values....

CSHARP_CC=$(CSHARP_PATH)/bin/mcs
MONO_JIT=$(CSHARP_PATH)/bin/mono

# common build environment

ifeq ($(NEOTONIC_ROOT),)
NEOTONIC_ROOT = ..
endif

include $(NEOTONIC_ROOT)/rules.mk

# our targets

TARGETS = clearsilver.dll subdirs
SUBDIRS = CSTest


all: $(TARGETS)

clearsilver.dll: CS.cs HDF.cs NeoErr.cs NeoUtil.cs
	$(CSHARP_CC) -target:library -unsafe CS.cs HDF.cs NeoErr.cs NeoUtil.cs -out:clearsilver.dll

subdirs:
	@for mdir in $(SUBDIRS); do \
	  echo "subdir: $$mdir"; \
	  if test -d $$mdir; then \
            if test -f $$mdir/Makefile.PL -a ! -f $$mdir/Makefile; then \
              cd $$mdir; $(PERL) Makefile.PL PREFIX=$(prefix); cd ..; \
            fi; \
            $(MAKE) -C $$mdir PREFIX=$(prefix); \
          fi; \
        done


clean:
	rm -f core.*

distclean:
	rm -f $(TARGETS) core.* Makefile.depend

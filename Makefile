# Embeddable C-based FORTH interpreter.
# Copyright 2025 L.C. Benschop, Vught, The Netherlands.
# The program is released under the GNU General Public License version 2 or
# later. There is NO WARRANTY.

CFLAGS=-O3 -DFORTH_ALLOW_UNALIGNED
#CFLAGS=-ggdb

default: forth forth_src/forth_glossary.txt 

forth: forth_main.o forth_engine.o forth_io.o 
	${CC} -o $@ $^ -lm

bforth: bforth_main.o forth_engine.o forth_io.o
	${CC} -o $@ $^ -lm

bforth_main.o: src/forth_main.c src/forth_engine.h
	${CC} -DFORTH_SKIP_DEFAULT_DICT $(CFLAGS) -o $@ -c $<

forth_engine.o: src/forth_engine.c src/forth_engine.h src/forth_opcodes.h
	${CC}  $(CFLAGS) -o $@ -c $<
forth_main.o: src/forth_main.c src/forth_engine.h  src/default_dict.h
	${CC}  $(CFLAGS) -o $@ -c $<
forth_io.o: src/forth_io.c src/forth_engine.h
	${CC}  $(CFLAGS) -o $@ -c $<

forth.img: kernel.img bforth forth_src/extend.fs forth_src/float.fs
	./bforth -d kernel.img forth_src/extend.fs

src/default_dict.h: bforth forth.img forth_src/mkdefdict.fs
	./bforth -d forth.img forth_src/mkdefdict.fs

clean:
	rm -f *.o forth bforth forth.img src/default_dict.h forth_src/forth_glossary.txt

cross: forth forth_src/cross.fs forth_src/kernel.fs
	./bforth -d forth.img  forth_src/cross.fs

forth_src/forth_glossary.txt: forth_src/kernel.fs forth_src/extend.fs forth_src/float.fs
	./bforth -d forth.img forth_src/doglos.fs

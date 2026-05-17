# 32-BIT EMBEDDABLE FORTH

This is a 32-bit embeddable FORTH written in standard C. It has the following
features:
* The version as supplied runs under Linux and similar systems.
* It runs on 32-bit and 64-bit machines, but cell size is always 32-bit.
* It works only on little-endian machines (for now). The integer format in the
  cells is little-endian. This is okay for x86 and for most ARM and RISC-V
  systens.
* @ and ! use addresses relative to the base of the dictionary.
* It inncludes floating point support, assumes IEEE754 64-bit binary
  floating point representation.
* It uses byte code (one byte per FORTH primitive, multi-byte instructions for
  LIT, BRANCH, CALL, etc.).
* CALL address is limited to 24-bit. 16 MB should be enough for everybody.
* I/O functions are separate from core FORTH engine, so can be ported
  to different platforms.
* The dictionary format is portable across machines. The host can run
  FORTH natively to extend its own dictionary and build a more complex
  application. The dictionary image can then be embedded into the C
  code on the target platform. THe target platform does not have to
  support file operations.
* Licensed under MIT license.

## HISTORY

This is based on SOD32, which was written by me in 1994. Like SOD32 it is
a virtual machine, but the differences are as follows:
* SOD32 attempted to sandbox the virtual machine and limit all memory
  accesses to the dictionary space.
* SOD32 was big-endian (with support to run also on little-endian
  hardware).  This one is little-endian.
* SOD32 was licensed under GNU GPL, this one is relicensed under MIT so it
  better fits in projects that I want to embed it in.
* SOD32 had an instruction word with 6 5-bit instructions packet into a 32-bit
  word. This FORTH has conventional byte code, one FORTH primitive per byte,
  unaligned operands. There are far more primitives and the set can still
  be extended.
* This FORTH has floating point and local variable support.

Some of my own stuff that I wrote for Agon is also used in this FORTH
version. In particular the floating point input and output routines
were taken from Agon FORTH.

Like SOD32 FORTH this one marks some colon definitions as macros, so
their instructions can be expended inline by the compiler (omitting
the terminating return). Expansion is much more straightforward than
with SOD32. Branches are relative, calls are absolute (with respect to
the dictionary base), so you can expand almost any colon definitions
inline, but we only do short ones (some are less than 4 bytes). A
special case of these are special macros for single instructions.

## GETTING STARTED

The makefile is set up to run out of the box on Linux. Simply type:
```
make
```
If everything goes well, you have an executable forth and some other
files.

Start Forth with

```
 ./forth
```
This uses the default dictionary. You can add a FORTH source file at the command line, like so.

```
 ./forth examples/compute_pi.fs
```
You can also specify an alternative dictionary file, like
```
 ./forth -d mydict.img
```

If FORTH is started, you can type Forth words. 

```
134 2 * .
```

will show 

```
268 OK
```

`WORDS` will show all available forth words.
You can load other files with

```
INCLUDE file.fs
```

You leave Forth by typing
```
BYE
```

The following command will cause `kernel.img` to be rebuilt from its 
sources:
```
make cross
```

# FILES

The following files are included:
* `src/forth_engine.h` header file for the system.
* `src/forth_opcodes.h` forth opcodes. Thse must match the ones
  defined by `forth_src/kernel.fs` and `forth_src/float.fs`. Opcodes
  in the range 0xc9..0xff are still unused.
* `src/forth_engine.c` The main FORTH engine.
* `src/forth_main.c` The main program that handles command line argumennts
  loads up a dictionary and runs the engine.
* `src/forth_io.c` The FORTH I/O routines, should work on most POSIX systems. 
* `Makefile` the makefile.
* `kernel.img` minimal dictionary image to allow the system to expand its
  own functionality.
* `forth_src/cross.fs` FORTH cross compiler, generates`kernel.img`
  This can be run from this FORTH itself, but also from a different ANSI FORHT
  like gforth.
* `forth_src/kernel.fs` source code for `kernel.img`
* `forth_src/extend.fs` source code that `kernel.img` can use to extend
  itself to the full FORTH system.
* `forth_src/float.fs` source code for floating point support (included
  by `extend.fs`).
* `forth_src/locals.fs` source code for local variable support (included
  by `extend.fs`).
* `forth_src/doglos.fs` script to generate `forth_glossary.txt`.
* `forth_src/glosgen.fs` glossary generator tool.
* `forth_src/mkdefdict.fs` tool te generate `default_dict.h` from `forth.img`.
  This could be done much eaeier with Python, but we're using FORHT now. And
  now we do not depend on Python.
* `examples/tetris.fs` The classic Tetris-like game for terminals, written 
  originally by Dirk Zoller in 1994.
* `examples/tester.fs` and `examples/core.fs` A test suite for ANSI Forth.
* `examples/testlocals.fs` A simple test and demo of local variables.
* `examples/sunrise.fs` Computes sunrise and sunset times anywhere on
  Earth and makes a nice calendar.
* `examples/compute_pi.fs` A program to compute PI to 6000 decimal places.
  The original version of this I wrote for the ZX-Spectrum and then it took
  6 hours or so to run. Now we run in 0.5 seconds on a modern PC.

The make process generates the following files:
* `bforth` A version of the C program that does not have the FORTH 
  dictionary included and can only be run with an image file. Needed to
  bootstrap the full FORTH.
* `forth.img` The dictionary image of the full FORTH system.
* `src/default_dict.h` The same image but as a C header file.
* `forth` The full FORTH binary, that includes the default dictionary.
* `forth_src/forth_glossary.txt` An glossary, generated from the source files.  


   

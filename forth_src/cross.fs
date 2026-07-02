\ CROSS COMPILER FOR Embeddable C-based FORTH interpreter. 
\ Copyright 2025 L.C. Benschop Vught, The Netherlands.
\ The program is released under the MIT license.
\ There is NO WARRANTY.
\ 
\ This serves as an introduction to Forth cross compiling, so it is excessively 
\ commented.  
\ 
\ This cross compiler can be run on any ANS Forth with the necessary 
\ extension wordset that is at least 32-bit, including SOD-32 Forth. 
\ 
\ It creates the memory image of a new Forth system that is to be run 
\ by the Embeddable C-based FORTH interpreter.
\ This is a little-endian 32-bit system. It can run on 64-bit processors,
\ but tthe cellssize is 32 bit and the addresses span a 32-bit range.
\ 
\ The cross compiler (or meta compiler or target compiler) is similar 
\ to a regular Forth compiler, except that it builds definitions in 
\ a dictionary in the memory image of a different Forth system. 
\ We call this the target dictionary in the target space of the 
\ target system.  
\ 
\ As the new definitions are for a different Forth system, the cross 
\ compiler cannot EXECUTE them. Neither can it easily find the new 
\ definitions in the target dictionary. Hence a shadow definition 
\ for each target definition is made in the normal Forth dictionary. 
\
\ The names of the new definitions overlap with the names of existing
\ elementary. Forth words. Therefore they need to be in a wordlist 
\ different from the normal Forth wordlist.

\ PART 1: THE VOCABULARIES.

\ We need the word VOCABULARY. It's not in the standard though it will
\ be in most actual implementations.
: VOCABULARY WORDLIST CREATE  ,  \ Make a new wordlist and store it in def.
  DOES> >R                      \ Replace last item in the search order.
  GET-ORDER SWAP DROP R> @ SWAP SET-ORDER ;


VOCABULARY TARGET
\ This vocabulary will hold shadow definitions for all words that are in
\ the target dictionary. When a shadow definition is executed, it 
\ performs the compile action in the target dictionary.

VOCABULARY TRANSIENT
\ This vocabulary will hold definitions that must be executed by the
\ host system ( the system on which the cross compiler runs) and that
\ compile to the target system.

\ Expl: The word IF occurs in all three vocabularies. The word IF in the
\       FORTH vocabulary is run by the host system and is used when
\       compiling host definitions. A different version is in the
\       TRANSIENT vocabulary. This one runs on the host system and
\       is used when compiling target definitions. The version in the
\       TARGET vocabulary is the version that will run on the target
\       system. 

\ : \D ; \ Uncomment one of these. If uncommented, display debug info.
: \D POSTPONE \ ; IMMEDIATE 

\ PART 2: THE TARGET DICTIONARY SPACE.

\ Next we need to define the target space and the words to access it.

20000 CONSTANT IMAGE_SIZE

CREATE IMAGE IMAGE_SIZE CHARS ALLOT \ This space contains the target image.
       IMAGE IMAGE_SIZE 0 FILL      \ Initialize it to zero.

\ Fetch and store characters in the target space.
: C@-T ( t-addr --- c) CHARS IMAGE + C@ ;
: C!-T ( c t-addr ---) CHARS IMAGE + C! ;

\ Fetch and strore halfwords (16-bit) in the target space.
: H@-T ( t-addr --- u16) CHARS IMAGE + DUP C@ SWAP 1 CHARS + C@ 8 LSHIFT + ;
: H!-T ( u16 t-addr ---) CHARS IMAGE + OVER OVER C!
    SWAP 8 RSHIFT SWAP 1 CHARS + C! ;

\ Fetch and strore triplebytes (24-bit) in the target space.
: T@-T ( t-addr --- u16) CHARS IMAGE + DUP C@ OVER 1 CHARS + C@ 8 LSHIFT +
    SWAP 2 CHARS + C@ 16 LSHIFT + ;
: T!-T ( u16 t-addr ---) CHARS IMAGE + OVER OVER C!
    OVER 8 RSHIFT OVER 1 CHARS + C! SWAP 16 RSHIFT SWAP 2 CHARS + C! ;

\ Fetch and store cells in the target space.
\ Target is little-endian so store explicitly little-endian.
\ We do support unaligned loads and stores.
: @-T  ( t-addr --- x)
       CHARS IMAGE + DUP C@  OVER 1 CHARS + C@ 8 LSHIFT +
       OVER 2 CHARS + C@ 16 LSHIFT + SWAP 3 CHARS + C@ 24 LSHIFT + ;
: !-T  ( x t-addr ---)
       CHARS IMAGE + OVER  OVER C! OVER 8 RSHIFT OVER 1 CHARS + C!
       OVER 16 RSHIFT OVER 2 CHARS + C! SWAP 24 RSHIFT SWAP 3 CHARS + C! ;


\ A dictionary is constructed in the target space. Here are the primitives
\ to maintain the dictionary pointer and to reserve space.

VARIABLE DP-T                       \ Dictionary pointer for target dictionary.
0 DP-T !                            \ Initialize it to zero, SOD starts at 0.
: THERE ( --- t-addr) DP-T @ ;      \ Equivalent of HERE in target space.                                
: ALLOT-T ( n --- ) DP-T +! ;       \ Reserve n bytes in the dictionary.
: CHARS-T ( n1 --- n2 ) ;      
: CELLS-T ( n1 --- n2 ) 2 LSHIFT ;  \ Cells are 4 chars.
: ALIGN-T                           \ REguular @ and ! only at aligned addresses
  BEGIN THERE 3 AND WHILE 1 ALLOT-T REPEAT ;
: ALIGNED-T ( n1 --- n2 ) 3 + -4 AND ; 
: C,-T  ( c --- )  THERE C!-T 1 CHARS ALLOT-T ;
: H,-T  ( 16u --- )  THERE H!-T 2 CHARS ALLOT-T ;
: T,-T  ( 24u --- )  THERE T!-T 3 CHARS ALLOT-T ;
: ,-T   ( x --- )  THERE !-T  1 CELLS-T ALLOT-T ;

: PLACE-T ( c-addr len t-addr --- ) \ Move counted string to target space.
  OVER OVER C!-T 1+ CHARS IMAGE + SWAP CHARS CMOVE ;      

\ After the Forth system is constructed, its image must be saved.
: SAVE-IMAGE ( "name" --- )
  32 WORD COUNT W/O BIN CREATE-FILE ABORT" Can't open file" >R
  IMAGE THERE R@ WRITE-FILE ABORT" Can't write file" 
  R> CLOSE-FILE ABORT" Can't close file" ;

\ PART 3: CREATING NEW DEFINITIONS IN THE TARGET SYSTEM.

\ These words create new target definitions, both the shadow definition
\ and the header in the target dictionary. The layout of target headers
\ can be changed but FIND in the target system must be changed accordingly. 

\ All definitions are linked together in a number of threads. Each word
\ is linked in only one thread. Which thread the word is linked to, can be
\ determined from the name by a 'hash' code. To find a word, one can compute
\ the hash code and then one can search just one thread that contains a 
\ small fraction of the words. 

32 CONSTANT #THREADS \ Number of threads 

CREATE TLINKS #THREADS CELLS ALLOT   \ This array points to the names
                           \ of the last definition in each thread.
TLINKS #THREADS CELLS 0 FILL 

VARIABLE LAST-T          \ Address of last definition.
VARIABLE LAST-CFA        \ Addres (in target definition) or last CFA.

: HASH ( c-addr u #threads --- n)
  >R OVER C@ 1 LSHIFT OVER 1 > IF ROT CHAR+ C@ 2 LSHIFT XOR ELSE ROT DROP 
   THEN XOR 
  R> 1- AND 
;  



: "HEADER >IN @ CREATE >IN ! \ Create the shadow definition.
  BL WORD
  DUP COUNT #THREADS HASH >R \ Compute the hash code.                            
  ALIGN-T TLINKS R@ CELLS + @ ,-T        \ Lay out the link field.   
\D  DUP COUNT CR ." Creating: " TYPE ."  Hash:" R@ . 
  COUNT DUP >R THERE PLACE-T  \ Place name in target dictionary. 
  THERE TLINKS R> R> SWAP >R CELLS + !
  THERE LAST-T !               
  THERE C@-T 128 OR THERE C!-T R> 1+ ALLOT-T ALIGN-T  
      \ Set bit 7 of count byte as a marker.
  0 ,-T \ Lay out 'setter' /'macro size' field.
;
\ : "HEADER CREATE ALIGN-T ;  \ Alternative for "HEADER in case the target system
                      \ is just an application without headers.

: MACRO LAST-T @ DUP C@-T 32 OR SWAP C!-T ; 
   \ Set the MACRO bit of last name. This indicates to the compiler in the
   \ target Forth system that this def may be expanded.

ALSO TRANSIENT DEFINITIONS 
: IMMEDIATE LAST-T @ DUP C@-T 64 OR SWAP C!-T ; 
            \ Set the IMMEDIATE bit of last name.
PREVIOUS DEFINITIONS

\ PART 4: CODE GENERATION

\ Our Forth is implenmented in C as a switch-statement operating on byte
\ values: each opcode is one byte. Some opcodes take addtional operaands
\ from the instruction stream (e,g, LIT, BRANCH and CALL).

\ Forth primitives such as + and R> are single opcodes. The compiler 
\ compiles them as opcodes rather than calls to a definition. Other
\ definitions such as S>D qand */ consist of only a few opcodes and are
\ expanded by the compiler into the constituent opcodes.


VARIABLE STATE-T 0 STATE-T ! \ State variable for cross compiler.
: T] 1 STATE-T ! ;
: T[ 0 STATE-T ! ;

VARIABLE CSP   \ Stack pointer checking between : and ;
: !CSP DEPTH CSP ! ;
: ?CSP DEPTH CSP @ - ABORT" Incomplete control structure" ;

\ There are several literal opcodes, for positive and negative numbers and
\ for various size numbers. 
: LITERAL-T ( n --- ) 
\D DUP ."  Literal:" . CR
    DUP 0< IF
	NEGATE
	DUP 1 = IF
	    DROP $85 C,-T \ special opcode for constant -1
	ELSE
	    DUP $100 U< IF
		$07 C,-T C,-T \ 8-bit negative literal opcode.
	    ELSE
		DUP $10000 U< IF
		    $09 C,-T H,-T \ 16-bit negative literal opcdeo.
		ELSE
		    DUP $1000000 U< IF
			$0B C,-T T,-T \ 24-bit negative literal opcode
		    ELSE
			NEGATE $0C C,-T ,-T \ Full 32-bit literal
		    THEN
		THEN
	    THEN
	THEN
    ELSE
	DUP 5 < IF
	    $80 + C,-T \ Dedicated opcodes for constants 0..4
	ELSE
	    DUP $100 < IF
		$06 C,-T C,-T \ 8-bit literal opcode
	    ELSE
		DUP $10000 < IF
		    $08 C,-T H,-T \ 16-bit literal opcode
		ELSE
		    DUP $1000000 < IF
			$0A C,-T T,-T \ 24-bit literal opcode
		    ELSE
			$0C C,-T ,-T \ Full 32-bit literal
		    THEN
		THEN
	    THEN
	THEN
    THEN
;
	    

TRANSIENT DEFINITIONS FORTH
\ Now define the words that do compile code. 

: OPCODE ( c --- )
  "HEADER DUP C, 1 THERE 4 - !-T C,-T $15 C,-T MACRO \ Create an executable
                                     \ target definition with opcode & return.
  DOES> C@ C,-T \ compile just this opcode.
;

: 2OPCODE ( c --- ) \ Same as OPCODE but for 2-byte opcode primitives.
  "HEADER DUP , 2 THERE 4 - !-T DUP 8 RSHIFT C,-T C,-T $15 C,-T MACRO \ Create an executable
                                     \ target definition with opcode & return.
  DOES> @ DUP 8 RSHIFT C,-T C,-T  \ compile just these 2 opcode bytes
;

    
: : !CSP "HEADER THERE DUP LAST-CFA ! , T]
    DOES> @ $01 C,-T T,-T ; \ Compile call opcode and 24-bit address.

: M: "HEADER THERE DUP LAST-CFA ! , MACRO \ M: makes a definition identical to that made by :
  T]                       \ but with macro bit set. If executed it copies 
                           \ all opcodes from the macro definition to the 
                           \ current definition. The macro itself is just
                           \ a target definition that can be executed.
  DOES>
    @ DUP 4 - @-T SWAP IMAGE + THERE IMAGE + ROT DUP ALLOT-T CMOVE
;

: ;
    THERE LAST-CFA @ - LAST-CFA @ 4 - !-T
    $15 C,-T \ Add EXIT opcode
    T[ ?CSP \ Quit compilation state.
  ;

  
FORTH DEFINITIONS

\ PART 5: FORWARD REFERENCES 

\ Some definitions are referenced before they are defined. A definition
\ in the TRANSIENT voc is created for each forward referenced definition.
\ This links all addresses together where the forward reference is used.
\ The word RESOLVE stores the real address everywhere it is needed.    

: FORWARD  
  CREATE $FFFFFFFF ,              \ Store head of list in the definition.
  DOES>
    DUP @ ,-T THERE 1 CELLS-T - SWAP ! \ Reserve a cell in the dictionary
                  \ where the call to the forward definition must come.
	          \ As the call address is unknown, store link to next 
                  \ reference instead.                                
;

: RESOLVE
  ALSO TARGET >IN @ ' >BODY @ >R >IN ! \ Find the resolving word in the 
                          \ target voc. and take the CFA out of the definition.
\D >IN @ BL WORD COUNT CR ." Resolving: " TYPE >IN !
  TRANSIENT ' >BODY  @                 \ Find the forward ref word in the
                                       \ TRANSIENT VOC and take list head.   
  BEGIN
   DUP $FFFFFFFF -                     \ Traverse all the links until end.
  WHILE
   DUP @-T                             \ Take address of next link from dict.
   R@ 8 LSHIFT 1 OR ROT !-T            \ Set call to resolved address in dict.
  REPEAT DROP R> DROP PREVIOUS
;

\ PART 6: DEFINING WORDS.

TRANSIENT DEFINITIONS FORTH
 
FORWARD DOVAR \ Dovar is the runtime part of a variable.
FORWARD DOCON \ Docon is the runtime part of a constant.

: VARIABLE "HEADER THERE , [ TRANSIENT ] DOVAR [ FORTH ]  0 ,-T
\ Create a variable.
  DOES> @ 4 + LITERAL-T  \ Compile var address as a literal for speed.  
; 

: CONSTANT "HEADER THERE ,
  [ TRANSIENT ] DOCON [ FORTH ]   \ Assemble the instruction LIT with RETURN.
  ,-T
  DOES> @ 4 + @-T LITERAL-T \ Compile const as a literal for speed.
;

FORTH DEFINITIONS

: T' ( --- t-addr) \ Find the execution token of a target definition.
  ALSO TARGET ' >BODY @ \ Get the address from the shadow definition.
  PREVIOUS
;

: >BODY-T ( t-addr1 --- t-addr2 ) \ Convert executing token to param address.
  1 CELLS-T + ;

\ PART 7: COMPILING WORDS 


TRANSIENT DEFINITIONS FORTH

\ The TRANSIENT definitions for IF, THEN etc. compile the conditional
\ and unconditional branch instructions of the virtual machine. We have
\ different opcodes for forward and backward branches and displacements are
\ 16-bit. 

: BEGIN THERE ;
: UNTIL $05 C,-T THERE 2 + SWAP - H,-T ; 
: IF $04 C,-T THERE 2 ALLOT-T ;
: THEN THERE OVER - 2 -  SWAP H!-T ; TARGET
: ELSE $02 C,-T THERE 2 ALLOT-T SWAP [ TRANSIENT ] THEN [ FORTH ] ; 
: WHILE [ TRANSIENT ] IF [ FORTH ] SWAP ; TARGET
: REPEAT $03 C,-T THERE 2 + SWAP - H,-T [ TRANSIENT ] THEN [ FORTH ] ; 

FORWARD (.")
FORWARD (POSTPONE)

: DO $11 C,-T THERE ;
: LOOP $12 C,-T THERE 2 + SWAP -  H,-T ;
: ." [ TRANSIENT ] (.") [ FORTH ] 34 WORD COUNT DUP 1+ >R 
      THERE PLACE-T R> ALLOT-T  ;
: POSTPONE [ TRANSIENT ] (POSTPONE) [ FORTH ] T' ,-T ;

: \ POSTPONE \ ; IMMEDIATE
: \G POSTPONE \ ; IMMEDIATE
: ( POSTPONE ( ; IMMEDIATE \ Move duplicates of comment words to TRANSIENT
: CHARS-T CHARS-T ; \ Also words that must be executed while cross compiling.
: CELLS-T CELLS-T ;
: ALLOT-T ALLOT-T ;
: ['] T' LITERAL-T ;
: EXIT $15 C,-T ;

FORTH DEFINITIONS

\ PART 8: THE CROSS COMPILER ITSELF.

VARIABLE DPL
: NUMBER? ( c-addr ---- d f)
  -1 DPL !
  BASE @ >R
  COUNT   
  OVER C@ 45 = DUP >R IF 1 - SWAP 1 + SWAP THEN \ Get any - sign 
  OVER C@ 36 = IF 16 BASE ! 1 - SWAP 1 + SWAP THEN   \ $ sign for hex.
  OVER C@ 35 = IF 10 BASE ! 1 - SWAP 1 + SWAP THEN   \ # sign for decimal
  DUP  0 > 0= IF  R> DROP R> BASE ! 0 EXIT THEN   \ Length 0 or less?
  >R >R 0 0 R> R>
  BEGIN  
   >NUMBER  
   DUP IF OVER C@ 46 = IF 1 - DUP DPL ! SWAP 1 + SWAP ELSE \ handle point. 
         R> DROP R> BASE ! 0 EXIT THEN   \ Error if anything but point  
       THEN    
  DUP 0= UNTIL DROP DROP R> IF DNEGATE THEN    
  R> BASE ! -1  
;

: CROSS-COMPILE
  ONLY TARGET DEFINITIONS ALSO TRANSIENT \ Restrict search order.
  BEGIN 
   BL WORD 
 \D CR DUP COUNT TYPE 
   DUP C@ 0= IF \ Get new word
    DROP REFILL DROP                      \ If empty, get new line.
   ELSE
    DUP COUNT S" END-CROSS" COMPARE 0=    \ Exit cross compiler on END-CROSS
    IF
     ONLY FORTH ALSO DEFINITIONS          \ Normal search order again.
     DROP EXIT
    THEN
    FIND IF                               \ Execute if found.
     EXECUTE
    ELSE
     NUMBER? 0= ABORT" Undefined word" DROP 
     STATE-T @ IF \ Parse it as a number.
      LITERAL-T   \ If compiling then compile as a literal. 
     THEN  
    THEN
   THEN
  0 UNTIL
;

\ PART 9: CROSS COMPILING THE KERNEL 

\ Up till now not a single byte of the new Forth kernel has actually been 
\ compiled. 

TRANSIENT DEFINITIONS
FORWARD WARM
FORWARD THROW
FORTH DEFINITIONS

S" forth_src/kernel.fs" INCLUDED

\ PART 10: FINISHING AND SAVING THE TARGET IMAGE.

\ Resolve the forward references created by the cross compiler.
RESOLVE DOVAR RESOLVE DOCON
RESOLVE (.") 
RESOLVE THROW RESOLVE WARM
RESOLVE (POSTPONE)
\ Fill the entry points in the dictionary header.
T' COLD $14 !-T
T' DIV-EX $28 !-T
T' BREAK-EX $2C !-T
T' TIMER-EX $30 !-T
T' SEG-EX $34 !-T

\ Store appropriate values into some of the new Forth's variables.
: CELLS>TARGET
  0 DO OVER I CELLS + @ OVER I CELLS-T + !-T LOOP 2DROP ;

#THREADS T' FORTH-WORDLIST >BODY-T 4 + !-T
TLINKS T' FORTH-WORDLIST >BODY-T 8 + #THREADS CELLS>TARGET 
THERE   $0C !-T  \ DP stored in dictionary header, = image size.

SAVE-IMAGE kernel.img \ Save the newly constructed Forth system to disk.
 
BYE \ All's been done. 

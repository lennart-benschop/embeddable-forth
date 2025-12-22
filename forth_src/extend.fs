\ Extensions to sod Forth kernel to make a complete Forth system.
\ Copyright 1994 L.C. Benschop Eindhoven, The Netherlands.
\ The program is released under the MIT license
\ There is NO WARRANTY.

\ Changes: 2025-01-11: Added ERASE/BLANK, make sure to erase new wordlist.
\                      Added comments to some words.

: \G POSTPONE \ ; IMMEDIATE
\G comment till end of line for inclusion in glossary.

\ PART 1: MISCELLANEOUS WORDS.

: ?TERMINAL ( ---f)
\G Test whether the ESC key is pressed, return a flag.     
    KEY? IF KEY 27 = IF -1 ELSE KEY DROP 0 THEN  ELSE 0 THEN ;

: ERASE ( c-addr u )
\G Fill memory region of u bytes starting at c-addr with zero.    
    0 FILL ;

: M: ( --- )
\G Make a definition similar to : that will be expanded as a macro.
  : LAST @ DUP C@ 32 OR SWAP C! ;

: OPCODE ( c --- )
\G Create FORTH primitive for single-byte opcode.    
    M: C, 4 CSP +! POSTPONE ; ;

: 2OPCODE ( c --- )
\G Create FORTH primitive for two-byte opcode.    
    M: DUP 8 RSHIFT C, C, 4 CSP +! POSTPONE ; ;

M: BOUNDS ( addr1 n --- addr2 addr1)
\G Convert address and length to two bounds addresses for DO LOOP 
  OVER + SWAP ;

: D0= ( d ---f)
\G f is true if and only if d is equal to zero.    
  OR 0= ;

: -TRAILING ( c-addr1 u1 --- c-addr2 u2)
\G Adjust the length of the string such that trailing spaces are excluded.
  BEGIN
   2DUP + 1- C@ BL = 
  WHILE
   1-
  REPEAT
;

: .(  ( "ccc<rparen>" ---)
\G Print the string up to the next right parenthesis.
   41 PARSE TYPE ;

\ PART 2: SEARCH ORDER WORDLIST

VARIABLE VOC-LINK ( --- a-addr)
\G Variable that links all vocabularies together, so we can link.
FORTH-WORDLIST VOC-LINK !

VARIABLE FENCE ( --- a-addr)
\G Address below which we are not allowed to forget.

: GET-ORDER ( --- w1 w2 ... wn n )
\G Return all wordlists in the search order, followed by the count.
  #ORDER @ 0 ?DO CONTEXT I CELLS + @ LOOP #ORDER @ ;

: SET-ORDER ( w1 w2 ... wn n --- )
\G Set the search order to the n wordlists given on the stack.
  #ORDER ! 0 #ORDER @ 1- DO CONTEXT I CELLS + ! -1 +LOOP ;

: ALSO ( --- )
\G Duplicate the last wordlist in the search order.
  CONTEXT #ORDER @ CELLS + DUP CELL- @ SWAP ! 1 #ORDER +! ;

: PREVIOUS ( --- )
\G Remove the last wordlist from search order.
   -1 #ORDER +! ;
 
VARIABLE #THREADS ( --- a-addr)
\G This variable holds the number of threads a word list will have.

: WORDLIST ( --- wid)
\G Make a new wordlist and give its address.
    HERE DUP VOC-LINK @ , VOC-LINK !
    #THREADS @ , HERE #THREADS @ CELLS DUP ALLOT ERASE ;

: DEFINITIONS  ( --- )
\G Set the definitions wordlist to the last wordlist in the search order.
CONTEXT #ORDER @ 1- CELLS + @ CURRENT ! ;

: FORTH ( --- )
\G REplace the last wordlist in the search order with FORTH-WORDLIST
  FORTH-WORDLIST CONTEXT #ORDER @ 1- CELLS + ! ;

1 #THREADS !
WORDLIST 
CONSTANT ROOT-WORDLIST ( --- wid )
\G Minimal wordlist for ONLY

32 #THREADS ! 

: ONLY ( --- )
\G Set the search order to the minimal wordlist.
  1 #ORDER ! ROOT-WORDLIST CONTEXT ! ;

: VOCABULARY ( --- )
\G Make a definition that will replace the last word in the search order
\G by its wordlist.
  WORDLIST CREATE  ,            \ Make a new wordlist and store it in def.
  DOES> >R                      \ Replace last item in the search order.
  GET-ORDER SWAP DROP R> @ SWAP SET-ORDER ;

: (FORGET) ( xt ---)
\G Forget the word indicated by xt and everything defined after it.    
    >NAME CELL- DUP FENCE @ U< -6 ?THROW \ Check we are not below fence.
    >R \ Store new dictionary pointer to return stack.
    VOC-LINK @   
    BEGIN  \ Traverse all worlists
	DUP R@ U> IF
	    DUP @ VOC-LINK ! \ Wordlist entirely above new DP, remove it.
	ELSE
	    R@
	    OVER CELL+ @ 0 DO
	   	OVER I 2 + CELLS + CELL+
		BEGIN
	   	   CELL- @ DUP 2 PICK U<
		UNTIL
		2 PICK I 2 + CELLS + !
	    LOOP
	    DROP
	THEN
	@
	DUP 0=
    UNTIL DROP
    R> DP ! \ Adjust dictionary pointer.
;

: FORGET ( "ccc" ---)
\G Remove word "ccc" from the dictionary, and anything defined later.
    32 WORD UPPERCASE? FIND 0=
    IF
	DROP \ Exit silently if word not found.
    ELSE
	(FORGET)
    THEN
;

: MARKER ( "ccc" --)
\G Create a word that when executeed forgets itself and everything defined
\G after it.
   CREATE DOES> 4 - (FORGET)    
;

: ENVIRONMENT? ( c-addr u --- false | val true)
\G Return an environmental query of the string c-addr u    
    2DROP 0 ;

\ Part 2A: Conditional compilation

: [IF] ( f ---)
\G If the flag is false, conditionally skip till the next [ELSE] or [ENDIF]
    0= IF
	BEGIN 
	    BEGIN
		BL WORD UPPERCASE? COUNT
		DUP WHILE
		    2DUP S" [ELSE]" COMPARE 0= IF
			2DROP NESTING @ 0= IF EXIT THEN
		    ELSE
			2DUP S" [THEN]" COMPARE 0= IF
			    2DROP NESTING @ 0= IF EXIT ELSE -1 NESTING +! THEN
			ELSE
			    S" [IF]" COMPARE 0= IF
				1 NESTING +!
			    THEN
			THEN
		    THEN	    
	    REPEAT
	    2DROP REFILL 0=
	UNTIL
	NESTING OFF
    THEN	
; IMMEDIATE

: [ELSE] ( --- )
    0 POSTPONE [IF] ; IMMEDIATE
\G Used in [IF] [ELSE] [THEN] for conditional compilation.    

: [THEN] ( --- )
\G Terminate [IF] [THEN] does nothing.
    ; IMMEDIATE

: [DEFINED] ( "ccc" --- f)
\G Produce a flag indicating whether the next word is defined.	
    BL WORD UPPERCASE? FIND SWAP DROP 0<> ; IMMEDIATE

\ PART 3: SOME UTILITIES, DUMP .S WORDS
 
: DL ( addr1 --- addr2 )
\G hex/ascii dump in one line of 16 bytes at addr1 addr2 is addr1+16
  BASE @ >R 16 BASE ! CR
  DUP 0 <# # # # # # # # # #> TYPE ." : "
  16 0 DO
   DUP I + C@ 0 <# # # #> TYPE SPACE 
  LOOP 
  16 0 DO
   DUP I + C@ DUP 32 < OVER 126 > OR IF DROP ." ." ELSE EMIT THEN
  LOOP 
  16 + R> BASE ! ;


: DUMP ( addr len --- )
\G Show a hex/ascii dump of the memory block of len bytes at addr  
  15 + 4 RSHIFT 0 DO
   DL ?TERMINAL IF LEAVE THEN
  LOOP DROP ; 

: H. ( u ---- )
\G Show a number (unsigned in hex)
  BASE @ >R HEX U. R> BASE ! ;

: .S ( --- )
\G Show the contents of the stack.
     DEPTH IF
      0 DEPTH 2 - DO I PICK . -1 +LOOP 
     ELSE ." Empty " THEN ;


: ID. ( nfa --- )
\G Show the name of the word with name field address nfa.
  COUNT 31 AND TYPE SPACE ;

: WORDS ( --- )
\G Show all words in the last wordlist of the search order.
  CONTEXT #ORDER @ 1- CELLS + @ CELL+
  DUP @ >R \ number of threads to return stack.
  CELL+ R@ 0 DO DUP I CELLS + @ SWAP LOOP DROP \ All thread pointers to stack.
  BEGIN
   0 0  
   R@ 0 DO 
    I 2 + PICK OVER U> IF  
     DROP DROP I I 1 + PICK
    THEN 
   LOOP \ Find the thread pointer with the highest address. 
   ?TERMINAL 0= AND
  WHILE
   DUP 1+ PICK DUP ID. \ Print the name.
   CELL- @             \ Link to previous.
   SWAP 2 + CELLS SP@ + ! \ Update the right thread pointer.
  REPEAT
  DROP R> 0 DO DROP LOOP  \ Drop the thread pointers.  
;

: CAT ( --- )
\G List the current directory    
    CR S" ls" SYSTEM DROP ;

: CD ( "ccc"  --)
\G Change to the specified directory
  BL WORD COUNT CHDIR -40 ?THROW ;
  
: DELETE ( "ccc"  --)
\G Delete the specified file.    
  BL WORD COUNT DELETE-FILE -38 ?THROW ;


ROOT-WORDLIST CURRENT !
: FORTH FORTH ;
: ALSO ALSO ;
: ONLY ONLY ;
: PREVIOUS PREVIOUS ;
: DEFINITIONS DEFINITIONS ;
: WORDS WORDS ;
DEFINITIONS
\ Fill the ROOT wordlist.

\ PART 4: ERROR MESSAGES

: MESS" ( n "ccc" --- )
\G Create an error message for throw code n.
  ALIGN , ERRORS @ , HERE 8 - ERRORS ! 34 WORD C@ 1+ ALLOT ;

-3 MESS" Stack overflow"
-4 MESS" Stack underflow"
-5 MESS" Dictionary full"
-6 MESS" Below fence"
-10 MESS" Divide overflow"
-13 MESS" Undefined word"
-22 MESS" Incomplete control structure"
-28 MESS" BREAK key pressed"
-37 MESS" File I/O error"
-38 MESS" File does not exist"
-39 MESS" Bad system command"
-40 MESS" Directory does not exist"
-41 MESS" Unimplemented system call"
-54 MESS" Floating point stack underflow"

: 2CONSTANT  ( d --- )
\G Create a new definition that has the following runtime behavior.
\G Runtime: ( --- d) push the constant double number on the stack. 
  CREATE HERE 2! 8 ALLOT DOES> 2@ ;

: 2VARIABLE ( --- )
\G Create a new definition that has the following runtime behavior.
\G Runtime: ( --- a-addr) push address onto the stack. 
    CREATE 0 , 0 , ;
    
: D.R ( d n --- )
\G Print double number d right-justified in a field of width n. 
  >R SWAP OVER DABS <# #S ROT SIGN #> R> OVER - 0 MAX SPACES TYPE ;

: U.R ( u n --- )
\G Print unsigned number u right-justified in a field of width n. 
  >R 0 R> D.R ;

: .R ( n1 n2 --- )
\G Print number n1 right-justified in a field of width n2. 
 >R S>D R> D.R ;

: AT-XY ( x y --- )
\G Put screen cursor at location (x,y) (0,0) is upper left corner.
  27 EMIT [CHAR] [ EMIT SWAP 1+  SWAP 0 .R [CHAR] ; EMIT 
   1+ 0 .R [CHAR] H EMIT ;

: PAGE 
\G Clear the screen.
  27 EMIT ." [2J" 0 0 AT-XY ;


: BLANK ( c-addr u ----)
\G Fill the memory region of u bytes starting at c-addr with spaces.
  32 FILL ;


: AGAIN ( x n ---)
\G Terminate a loop forever BEGIN..AGAIN loop.
  $80 C, POSTPONE UNTIL ; IMMEDIATE

: CASE ( --- )
\G Start a CASE..ENDCASE construct. Inside are one or more OF..ENDOF blocks.
\G runtime the CASE blocks takes one value from the stack and uses it to
\G select one OF..ENDOF block.
  CSP @ SP@ CSP ! ; IMMEDIATE
: OF ( --- x)
\G Start an OF..ENDOF block. At runtime it pops a value from the stack and
\G executes the block if this value is equal to the CASE value.
  POSTPONE OVER POSTPONE = POSTPONE IF POSTPONE DROP ; IMMEDIATE
: ENDOF ( x1 --- x2)
\G Terminate an OF..ENDOF block.
  POSTPONE ELSE ; IMMEDIATE
: ENDCASE ( variable# ---)
\G Terminate a CASE..ENDCASE construct.
  POSTPONE DROP BEGIN SP@ CSP @ - WHILE POSTPONE THEN REPEAT
  CSP ! ; IMMEDIATE


: VALUE ( n --- ) 
\G Create a variable that returns its value when executed, prefix it with TO
\G to change its value.    
    CREATE , IMMEDIATE DOES>
    STATE @ IF
	POSTPONE LITERAL
	CASE
	    TO-STATE @
	    0 OF POSTPONE @ ENDOF
	    1 OF POSTPONE ! ENDOF
	    2 OF POSTPONE +! ENDOF
	ENDCASE
    ELSE
	CASE
	    TO-STATE @
	    0 OF @ ENDOF
	    1 OF ! ENDOF
	    2 OF +! ENDOF
	ENDCASE
    THEN
    TO-STATE OFF
;

VARIABLE TIMER
: TIMER-INT ( x ---) 
\G Timer interrupt handler.
  1 TIMER +! RTI
;

: MS ( n --- )
\G Delay for n milliseconds.
  ['] TIMER-INT $30 ! 1000 * SETALARM TIMER @
  BEGIN TIMER @ OVER - UNTIL DROP ;

: SAVE-SYSTEM ( "ccc" --- )
\G Save the Forth system to a file.
  BL WORD COUNT W/O BIN CREATE-FILE ABORT" Can't open"
  0 HERE 2 PICK WRITE-FILE DROP
  CLOSE-FILE DROP ;

CAPS ON

S" forth_src/float.fs" INCLUDED

HERE FENCE !

SAVE-SYSTEM forth.img
BYE

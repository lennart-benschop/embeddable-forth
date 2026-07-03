\ Local variables for Embeddable FORTH.
\ Copyright 2025, L.C. Benschop. MIT license

\ Note: the variables LVARS and LVAR-CLEAR-VECTOR are included in kernel.fs

\ Temporary dictionary space for local variables. Will be erased
\ after function definition.
CREATE TMPDICT 500 ALLOT
VARIABLE TMPDP \ Dictionary pointer for this dictionary space.
VARIABLE LASTFRAME-P \ Pointer to Number of variables at end of last frame.

#THREADS @ \ Save old #THREADS
1 #THREADS !
WORDLIST CONSTANT TMP-LOCALS-WORDLIST \ Wordlist to store local variables.
WORDLIST CONSTANT LOCALS-WORDLIST \ Wordlist for words inside locals block.

#THREADS ! \ Restore it.

VARIABLE LOCAL-TYPE-CODE

LOCALS-WORDLIST CURRENT ! \ Create new defs in 

: W: ( ---)
  0 LOCAL-TYPE-CODE ! ;
: W^ ( ---)
  4 LOCAL-TYPE-CODE ! ;
: C: ( ---)
  0 LOCAL-TYPE-CODE ! ;
: C^ ( ---)
  4 LOCAL-TYPE-CODE ! ;
: D: ( ---)
  1 LOCAL-TYPE-CODE ! ;
: D^ ( ---)
  5 LOCAL-TYPE-CODE ! ;
: F: ( ---)
  2 LOCAL-TYPE-CODE ! ;
: F^ ( ---)
  6 LOCAL-TYPE-CODE ! ;
: -- ( ---) \ Mark end of locals list.
  -1 LOCAL-TYPE-CODE ! ;

DEFINITIONS \ Restore the old definitions.

: CREATE-LOCAL ( "ccc" --- type-code)
  \ In the build phase, stack one type code (will be collected at end of :})
  DP @ TMPDP @ DP ! \ set dictionary pointer temporarily to temp space.
  CREATE IMMEDIATE
  LASTFRAME-P @ , \ This location will hold the LVARS value at end of current frame.
  LOCAL-TYPE-CODE @ DUP C, 0 LOCAL-TYPE-CODE !
  LVARS @  DUP LASTFRAME-P @ @ - C, 1+ LVARS !
  DP @ TMPDP ! SWAP DP ! \ Update TMPDP and restore DP.
  DOES>
   DUP @ @ ( lastframe ) LVARS @ SWAP -
   \ Compute the number of lvars used in later frames. Add this to lvar-index
   OVER CELL+ CHAR+ C@ ( lvar-index ) + >R ( store lvar-index to ret stack)
   CELL+ C@ ( type-code)
   DUP 4 AND IF ( Are we a pointer to the local var? )
      DROP POSTPONE LP@
      R> 8 * POSTPONE LITERAL
      POSTPONE +
   ELSE
      CASE
        0 OF \ Word variable
	  CASE
	    TO-STATE @
	  0 OF $8A C, R> C, ENDOF
	  1 OF $8B C, R> C, ENDOF
	  2 OF $8A C, R@ C, POSTPONE + $8B C, R> C, ENDOF
	  ENDCASE
        ENDOF
	1 OF \ double variable
	  CASE
	    TO-STATE @
	  0 OF $8C C, R> C, ENDOF
	  1 OF $8D C, R> C, ENDOF
	  2 OF $8C C, R@ C, POSTPONE D+ $8D C, R> C, ENDOF
	  ENDCASE
	ENDOF
	2 OF \ float variable
	  CASE
	    TO-STATE @
	  0 OF $8E C, R> C, ENDOF
	  1 OF $8F C, R> C, ENDOF
	  2 OF $8E C, R@ C, POSTPONE F+ $8F C, R> C, ENDOF
	  ENDCASE
	ENDOF
      ENDCASE
      TO-STATE OFF
   THEN	
;


: {: ( --- )
\G Start local variable definition.
\ Run a mini-interpreter that only executes words from thje LOCALS-WORDLIST
\ and creates new locals via CREATE-LOCAL for any names not defined.
\ End processing when :} is encountered
  0 LOCAL-TYPE-CODE !
  LVARS @ 0= IF
    TMPDICT TMPDP ! \ Start with empty temporary dictionary.
    ALSO
    TMP-LOCALS-WORDLIST CONTEXT #ORDER @ 1- CELLS + ! \ Add TMP-LOCALS-WORDLIST to search order
  THEN
  LVARS @ >R \ Number of local vars in previous frames.
  \ Create a cell to hold the last var index of the current frame.
  TMPDP @ ALIGNED TMPDP !
  TMPDP @ LASTFRAME-P !
  1 CELLS TMPDP +!
  LVARS @ LASTFRAME-P @ !
  BEGIN
    >IN @ >R
    BL WORD UPPERCASE? COUNT
    2DUP S" :}" COMPARE 0= 0=
    OVER AND ( string length must be >0)
  WHILE
    LOCAL-TYPE-CODE @ -1 =
    IF
      2DROP R> DROP \ Ignore all words after "--"
    ELSE
      LOCALS-WORDLIST SEARCH-WORDLIST IF
        EXECUTE R> DROP
      ELSE
        LAST @ CURRENT @ TMP-LOCALS-WORDLIST CURRENT ! 
	R> >IN ! CREATE-LOCAL
	-ROT CURRENT ! LAST !       \ Restore CURRENT and LAST.
      THEN
    THEN
  REPEAT R> DROP 2DROP
  LVARS @ LASTFRAME-P @ !
  
  \ For each local variable whose type code is stacked, compile the
  \ appropriate >LOC primitive
  LVARS @ R> - 0 ?DO
    3 AND CASE
      0 OF $86 C, ENDOF \ single word
      1 OF $87 C, ENDOF \ double number
      2 OF $88 C, ENDOF \ Floating point
    ENDCASE	
  LOOP
; IMMEDIATE


: LVAR-CLEAR
  0 LVARS !
  CONTEXT #ORDER @ 1- CELLS + @ TMP-LOCALS-WORDLIST = IF
    PREVIOUS \ Remove the TMP-LOCALS-WORDLIST from the search order
  THEN     
  0 TMP-LOCALS-WORDLIST 2 CELLS + ! \ Set the link in that wordlist to 0.
; 

' LVAR-CLEAR LVAR-CLEAR-VECTOR !

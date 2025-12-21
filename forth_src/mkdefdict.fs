\ Create default dictiinary.

32768 CONSTANT DICT-MAX

CREATE DICT-SPACE DICT-MAX ALLOT
DICT-SPACE DICT-MAX ERASE

VARIABLE DICT-HANDLE

: WL ( c-addr u ---)
    DICT-HANDLE @ WRITE-LINE DROP
;
    
CREATE LINEBUF 64 ALLOT
VARIABLE LINE-LENGTH
: FLUSH-LINE
    LINE-LENGTH @ IF
	LINEBUF LINE-LENGTH @ WL
	0 LINE-LENGTH !
    THEN
;


: ADD-WORD ( w ---)
    BASE @ >R HEX
    0 <# BL HOLD ',' HOLD # # # # # # # # [CHAR] x HOLD '0' HOLD #>
    DUP >R LINEBUF LINE-LENGTH @ + SWAP CMOVE R> LINE-LENGTH +!
    R> BASE !
    LINE-LENGTH @ 70 > IF  FLUSH-LINE THEN
;


: NEW-DICT ( c-addr u --- )
    W/O OPEN-FILE -38 ?THROW DICT-HANDLE !
    S" /* Forth dictionary image, converted from forth.img */" WL
    0 0  WL
    S" const uint32_t forth_default_dict[] = {" WL
;

: CONVERT-DICT ( c-addr u --- )
    ALIGNED BOUNDS ?DO
	I @ ADD-WORD
    4 +LOOP
;

: CLOSE-DICT ( --- )
    FLUSH-LINE
    S" };" WL
    DICT-HANDLE @ CLOSE-FILE DROP 
;

: MAKE-DEFAULT-DICT 
    S" src/default_dict.h" NEW-DICT
    S" forth.img" R/O BIN OPEN-FILE -38 ?THROW
    DICT-SPACE DICT-MAX 2 PICK READ-FILE -37 ?THROW
    DICT-SPACE SWAP CONVERT-DICT
    CLOSE-DICT DROP
;

MAKE-DEFAULT-DICT
BYE

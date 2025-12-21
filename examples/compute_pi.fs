\ Compute PI to 6000 digits.

1510 CONSTANT LNUM_WORDS 

: LNUM ( --- ) \ Create a large number 
  CREATE LNUM_WORDS 1+ CELLS 
  DUP ALLOT          \ reserve room for n+1 words, first is leading zeros.
  HERE OVER - SWAP ERASE ; \ Clear the number with zeros.

: L. ( addr --- ) \ Print large number.
  CR
  LNUM_WORDS 1+ 1 DO
    DUP I CELLS + @ 0 \ Get Cell #i, convert to double.
    <# # # # # #> TYPE   \ Convert to 4 decimal digits and print.
    I 1 = IF              
      46 EMIT            \ First digit group is followed by .
    ELSE                 
      I 16 MOD 16 = IF   \ CR after 16 groups of 4 digits
        CR
      ELSE
        SPACE            \ otherwise space.
      THEN
    THEN
  LOOP DROP CR ;	

VARIABLE SRC_ADDR
VARIABLE DST_ADDR
VARIABLE DIVISOR

: L+  ( dest src --- ) \
  SRC_ADDR !
  DST_ADDR !
  0 
  SRC_ADDR @ @ 
     DUP LNUM_WORDS >= IF 2DROP EXIT THEN 
  1+ 
  LNUM_WORDS
  DO
     SRC_ADDR @ I CELLS + @ +
     DST_ADDR @ I CELLS + @ +
     DUP 9999 > IF
       10000 - 1
     ELSE 
       0 
     THEN
     SWAP DST_ADDR @ I CELLS + !
  -1 +LOOP 
  1 SRC_ADDR @ @ DUP 0= IF DROP 2DROP EXIT THEN
  DO
     0= IF 0 LEAVE THEN
     DST_ADDR @ I CELLS + @ 1+
     DUP 9999 > IF
       10000 - 1
     ELSE 
       0 
     THEN
     SWAP DST_ADDR @ I CELLS + !
  -1 +LOOP 
  DROP ;


: L-  ( dest src --- ) \
  SRC_ADDR !
  DST_ADDR !
  0 
  SRC_ADDR @ @ 
     DUP LNUM_WORDS >= IF 2DROP EXIT THEN 
  1+ 
  LNUM_WORDS
  DO
     DST_ADDR @ I CELLS + @ SWAP -
     SRC_ADDR @ I CELLS + @ -
     DUP 0< IF
       10000 + 1
     ELSE 
       0 
     THEN
     SWAP DST_ADDR @ I CELLS + !
  -1 +LOOP 
  1 SRC_ADDR @ @ DUP 0= IF DROP 2DROP EXIT THEN
  DO
     0= IF 0 LEAVE THEN
     DST_ADDR @ I CELLS + @ 1-
     DUP 0< IF
       10000 + 1
     ELSE 
       0 
     THEN
     SWAP DST_ADDR @ I CELLS + !
  -1 +LOOP 
  DROP ;


: L/ ( dest src div --- )
  DIVISOR !
  SRC_ADDR !
  DST_ADDR !
  0 LNUM_WORDS 1+ SRC_ADDR @ @ 1+
  DO 
    SRC_ADDR @ I CELLS + @ 0 
    ROT 10000 UM* 
    D+
    DIVISOR @ UM/MOD 
    DST_ADDR @ I CELLS + !
  LOOP 
  DROP 
  SRC_ADDR @ @ DUP 1+ CELLS DST_ADDR @ + @ 0= IF 1+ THEN
  DST_ADDR @ !
;

LNUM NUM_PI
LNUM TERM
LNUM X**N

VARIABLE 1/X
VARIABLE TERM_SIGN

: COMPUTE_ATAN
  10000 1 DO
    X**N X**N 1/X @ L/
    1/X @ 239 = 
    IF   
      X**N X**N 1/X @ L/
    THEN
    \  I .
    \ X**N L.
    TERM X**N I L/
    TERM @ LNUM_WORDS < 0=
    \ TERM L.
    IF
      LEAVE
    THEN
    TERM_SIGN @ 
    IF 
       NUM_PI TERM L-
    ELSE
       NUM_PI TERM L+
    THEN
    \ NUM_PI L. 
    1 TERM_SIGN @ - TERM_SIGN !
  2 +LOOP ;
     

0 TERM_SIGN !                  \ atan must be added to result
25 1/X !                       \ compute atan(1/5)
16 5 * X**N CELL+ !            \ Initialize x**n to 16*5, i.e. 16*atan(1/5)
COMPUTE_ATAN

X**N LNUM_WORDS 1+ CELLS ERASE \ Clear the term.
1 TERM_SIGN !                  \ atan must be subtracted from result.
239 1/X !                      \ Compute atan(1/239)
4 239 * X**N CELL+ !	       \ Initialize x**n to 4*239, i.e. 4*atan(1/239)
COMPUTE_ATAN

NUM_PI L.

BYE

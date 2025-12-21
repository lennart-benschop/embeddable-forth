\ Floating point wordset for eZ80 Agon FORTH.
\ Copyright 2025, L.C. Benschop. MIT license

\ Note: VARIABLEs FP and F0 already defined in kernel.

\ Floating point format:
\ byte 0..1 little endian: bit 15 is sign (0 = positive, 1 is negative).
\           bits 14..0 offset exponent. 16384 is means range 1.0<=x<2.0
\ bytes 2..7 48-bit significand little endian order, scaled integer,
\            MSB usually set.

$90
OPCODE F+ ( F: r1 r2 --- r3)
\G Add floating point numbers r1 and r2, giving r3

$91
OPCODE F- ( F: r1 r2 --- r3)
\G Subtract floating point numbers r1 and r2, giving r3    

$92
OPCODE F* ( F: r1 r2 --- r3)
\G Multiply floating point numbers r1 and r2, giving r3    

$93    
OPCODE F/ ( F: r1 r2 --- r3)
\G Divide floating point numbers r1 and r2, giving r3    

$94
OPCODE FMOD ( F: r1 r2 --- r3)
\G Modulu of floating point numbers r1 and r2, giving r3    

$95
OPCODE D>F ( d --- F: --- r)
\G Convert signed double number to floating point.

$96
OPCODE F>D ( --- d F: r --- )
\G Convert floating point number to signed double, return -2**63 if out of range

$97
OPCODE FROUND ( F: r1 --- r2)
\G Round to nearest integer value.

$98
OPCODE FTRUNC ( F: r1 --- r2)
\G Round down to zero..

$99
OPCODE FSCALE ( n --- F: r1 --- r2)
\G scale the number by a power of 2, given by n.

$9A
OPCODE S>F ( n --- F: --- r)
\G Convert signed number to floating point.

$9B
OPCODE F>S ( --- n F: r --- )
\G Convert floating point number to signed , return -2**31 if out of range

$9C
OPCODE FABS ( F: r1 --- r2)
\G Take the absolute value of the floating point number.    

$9D
OPCODE FNEGATE ( F; r1 --- r2)
\G Negate the floating point number.    

$9F00
2OPCODE FSQRT ( F; r1 --- r2)
\G Square root of floating point number.

$9F01
2OPCODE FSIN ( F; r1 --- r2)
\G Sine of floating point number.

$9F02
2OPCODE FCOS ( F; r1 --- r2)
\G Cosine of floating point number.

$9F03
2OPCODE FTAN ( F; r1 --- r2)
\G Tangent of floating point number.

$9F04
2OPCODE FASIN ( F; r1 --- r2)
\G Arcsin of floating point number.

$9F05
2OPCODE FACOS ( F; r1 --- r2)
\G Arccos of floating point number.

$9F06
2OPCODE FATAN ( F; r1 --- r2)
\G Arctan of floating point number.

$9F07
2OPCODE FATAN2 ( F; r1 r2--- r3)
\G Arctan of angle  given by x=r1, y=r2.

$9F08
2OPCODE FLN ( F; r1 --- r2)
\G Natural logarithm of floating point number.

$9F09
2OPCODE FLOG ( F; r1 --- r2)
\G Base-10 Logarithm of floating point number.

$9F0A
2OPCODE FEXP ( F; r1 --- r2)
\G Raise e to r1.

$9F0B
2OPCODE F** ( F; r1 r2 --- r3)
\G Raise r1 to power r2.

$A0
OPCODE FDROP ( F: r ---- )
\G Remove top from the FP stack    

$A1
OPCODE FDUP ( F: r --- r r)
\G Duplicate top item on the FP stack.    

$A2
OPCODE FSWAP ( F: r1 r2 --- r2 r1 )
\G Swap top two items on the FP stack

$A3
OPCODE FOVER ( F: r1 r2 --- r1 r2 r1 )
\G Duplicate second item on the FP stack.    

$A4
OPCODE FROT ( F: r1 r2 r3 --- r2 r3 r1)
\G Rotate top three items on the FP stack

$A8
OPCODE F@ ( addr --- , F: --- r)
\G Read a floating point number from memory and add it to the FP stack.

$A8
OPCODE DF@ ( addr --- , F: --- r)
\G Read a floating point number from memory and add it to the FP stack.

$A9
OPCODE F! ( addr ----. F: r ---)
\G Write a floating point number to memory, which was taken from the FP stack.

$A9
OPCODE DF! ( addr ----. F: r ---)
\G Write a floating point number to memory, which was taken from the FP stack.

$AA
OPCODE SF@ ( addr --- , F: --- r)
\G Read a single-precision floating point number from memory and add
\G it to the FP stack.

$AB
OPCODE SF! ( addr ----. F: r ---)
\G Write a single-precision floating point number to memory, which was taken
\G from the FP stack.

$AC
OPCODE F@U ( addr --- , F: --- r)
\G Read a floating point number unaligned from memory and add it to the FP stack.

$AD
OPCODE F!U ( addr ----. F: r ---)
\G Write a floating point number unaligned to memory, which was taken from the FP stack.

$AE
OPCODE FISNEG ( --- f  F: r -- -)
\G Test if floating point number has sign bit set, (will also be true for -0.0).

$AF
OPCODE FISINF ( --- f  F: r -- -)
\G Test if floating point number is infinity or NaN.

$B0
OPCODE F0= ( --- f  F: r -- -)
\G Test if floating point number is equal to zero.    

$B1
OPCODE F0<> ( --- f  F: r -- -)
\G Test if floating point number is not equal to zero.    

$B2
OPCODE F0< ( --- f  F: r -- -)
\G Test if floating point number is less than 0    

$B3
OPCODE F0> ( --- f  F: r -- -)
\G Test if floating point number is greater than 0    

$B4
OPCODE F0<= ( --- f  F: r -- -)
\G Test if floating point number is less than or equal to  0    

$B5
OPCODE F0>= ( --- f  F: r -- -)
\G Test if floating point number is greater than or equal to 0    

$B6
OPCODE F= ( --- f F: r1 r2 ---)
\G Return true if r1 is equal to r2.

$B7
OPCODE F<> ( --- f F: r1 r2 ---)
\G Return true if r1 is not equal to r2.

$B8
OPCODE F< ( --- f F: r1 r2 ---)
\G Return true if r1 is less than r2.

$B9
OPCODE F> ( --- f F: r1 r2 ---)
\G Return true if r1 is greater than r2.

$BA
OPCODE F<= ( --- f F: r1 r2 ---)
\G Return true if r1 is less than or equal to r2.

$BB
OPCODE F>= ( --- f F: r1 r2 ---)
\G Return true if r1 is greater than or euqal to r2.

$BC
OPCODE FREXP ( --- n F: r --- )
\G Return the exponent value of a floating point number.

$BD
OPCODE FLOOR ( F: r1 --- r2)
\G floor function.

: FALIGN ( --- )
    \G align the dictionary pointer to align to a float addres
    ALIGN 
;

M: FALIGNED ( addr1 -- addr2)
    \G increment address to next float-aligned address, no-op.
   ALIGNED
;

M: FLOAT+ ( addr1 --- addr2)
\G Point the address to the next float     
    8 + ;

M: FLOATS ( n --- n2)
\G Compute the number of bytes occupied by n floats.
    CELLS 2*
;

: FDEPTH ( --- n)
\G Return the depth of the FP stack    
    F0 @ FP@ - 2/ 2/ 2/ ;
    
: F, ( F: r --- )
\G Add the floating point number r to the dictionary.    
    HERE F!U 8 ALLOT ;

M: F+! ( addr --- F: r )
\G Add r to the value already stored at the float value stored at
\G address addr
    DUP F@ F+ F! ;

: FVARIABLE ( --- )
    CREATE 8 ALLOT ;

: FCONSTANT ( F: r --- )
    CREATE F, DOES> F@ ;

: FLITERAL ( F: r --- )
   $0D C,  F, ; IMMEDIATE

: FVALUE ( n --- ) 
\G Create a float variable that returns its value when executed,
\G prefix it with TO to change its value.
    CREATE F, IMMEDIATE DOES>
    STATE @ IF
	POSTPONE LITERAL
	CASE
	    TO-STATE @
	    0 OF POSTPONE F@ ENDOF
	    1 OF POSTPONE F! ENDOF
	    2 OF POSTPONE F+! ENDOF
	ENDCASE
    ELSE
	CASE
	    TO-STATE @
	    0 OF F@ ENDOF
	    1 OF F! ENDOF
	    2 OF F+! ENDOF
	ENDCASE
    THEN
    TO-STATE OFF
;


: FMIN ( F: r1 r2 --- r3)
\G Return the minimum of r1 and r2
  FOVER FOVER F< IF FDROP ELSE FSWAP FDROP THEN ;

: FMAX ( F: r1 r2 --- r3)
\G Return the maximum of r1 and r2
  FOVER FOVER F< IF FSWAP FDROP ELSE FDROP THEN ;

0. D>F FCONSTANT 0.0E
1. D>F FCONSTANT 1.0E
10. D>F FCONSTANT 10.0E
1.0e 2. d>f f/ FCONSTANT 0.5E

: FI** ( n --- F: r1 ---r2)
\G Raise a floating point number to an integer power.
    DUP 0< >R ABS
    1.0E 
    BEGIN
	DUP
    WHILE
	DUP 1 AND IF
	    FOVER F* 
	THEN
	FSWAP FDUP F* FSWAP
	1 RSHIFT
    REPEAT
    FSWAP FDROP DROP R> IF 1.0E fSWAP F/ THEN ;

VARIABLE EXP-STATE
VARIABLE EXP
: >FLOAT ( c-addr u --- true | false F: ---r | ) 
\G Convert the string at c-addr u to a floating point number. If
\G success rutrn true and a floating point number on the FP stack, else
    \G return false and nothing on the FP stack.
    0 EXP-STATE !
    0 EXP !
    0.0E \ Initial value of float
    BL SKIP
    -1 DPL !
    DUP IF
	OVER C@ '+' = IF
	    SWAP 1+ SWAP 1-
	ELSE
	    OVER C@ '-' = IF
		SWAP 1+ SWAP 1- -1
	    ELSE
		0
	    THEN
	ELSE
	    0
	THEN >R \ Store sign.
	BOUNDS ?DO
	    \ CR I . I C@ . DPL @ . EXP-STATE @ . EXP @ .
	    EXP-STATE @ 0= IF
		I C@ DIGIT? IF
		    10.0E F*
		    0 D>F F+ 
		    DPL @ 0< 0= IF 1 DPL +! THEN
		ELSE
		    I C@ '.' =  DPL @ 0<  AND  IF
			0 DPL !
		    ELSE
			I C@ $DE AND 'D' = IF
			    1 EXP-STATE !
			ELSE
			    I C@ BL <> IF FDROP UNLOOP R> DROP FALSE  EXIT THEN
			THEN
		    THEN
		THEN
	    ELSE
		EXP-STATE @ 1 = IF
		    I C@ '+' = IF
			2 EXP-STATE !
		    ELSE
			I C@ '-' = IF
			    3 EXP-STATE !
			ELSE
			    2 EXP-STATE !
			    I C@ DIGIT? IF
				 EXP !
			    ELSE
				I C@ BL <> IF FDROP  UNLOOP  R> DROP FALSE  EXIT THEN
			    THEN
			THEN
		    THEN
		ELSE
		    I C@ DIGIT? IF
			EXP @ 10 * + EXP !
		    ELSE
			I C@ BL <> IF FDROP  UNLOOP  R> DROP FALSE  EXIT THEN
		    THEN
		THEN
	    THEN
	LOOP
	EXP-STATE @ 3 = IF EXP @ NEGATE EXP ! THEN
	EXP @ DPL @ 0< 0= IF DPL @ - THEN
	DUP 0< 0= IF
	    10.0E FI** F*
	ELSE
	    NEGATE 10.0E FI** F/
	THEN
	R> IF FNEGATE  THEN
    ELSE 2DROP	
    THEN
    TRUE
;

: IS-FLOAT-LIT? ( c-addr --- c-addr f)
\G Test if the counted string at c-addr can be a floating point
\G literal. BASE has to be 10 and the string has to contain an E.
    BASE @ 10 <> IF
	0
    ELSE
	DUP COUNT 'E' SCAN 0<> NIP 
    THEN
;
    
: FNUMBER-EXEC ( c-addr ---- c-addr 0 | -1 )
\G Code to handle float literals in the interpreter.    
  IS-FLOAT-LIT? IF
      DUP COUNT >FLOAT IF
	  DROP STATE @ IF POSTPONE FLITERAL THEN TRUE
      ELSE
	  FALSE
      THEN
  ELSE
      FALSE
  THEN
;

' FNUMBER-EXEC FNUMBER-VECTOR !
\ From now on, floating point literals are accepted in the interpreter.

FVARIABLE F-LIMIT
: REPRESENT ( c-addr u --- n flag1 flag2 F: r ---)
\G Convert floating point nunmber to a string of decimal dtgits at c-addr
\G (length is u).
\G n is the base 10 exponent. 0 is decimal point should be just before the
\G digits, +n means the decimal point should be after n digits, -n means
\G decimal point should be n zeros to the left of the digits returned.
\G Flag1 is the sign (true for negative), flag2 is set if there was a valid
\G number (not infinity or NaN)
    2DUP '0' FILL \ Fill buffer with zeros.
    DUP 1- 16 MIN EXP !  \ store precision-1 
    FDUP FISNEG >R
    FDUP FISINF IF
	FDROP 2DROP 0 R> 0
    ELSE
	FDUP F0= IF
	    FDROP 2DROP 1 R> -1 
	ELSE
	    FABS FDUP FREXP 301 1000 */ \ Convert binary exponent to base 10 exp
	    DUP >R \ Store exponent on return stack
	    NEGATE EXP @ + DUP 0< IF
		NEGATE 10.0e FI** F/
	    ELSE
		10.0e FI** F* \ Try to get num in range 10*(prec-1)..10**prec
	    THEN
	    10.0e EXP @ FI** 0.5E F- F-LIMIT F!
	    BEGIN
		FDUP F-LIMIT F@ F<
	    WHILE
		    10.0e F*
		    R> 1- >R 
	    REPEAT
	    F-LIMIT F@ 10.0E F* 4.5E F+ F-LIMIT F!
	    BEGIN
		FDUP F-LIMIT F@ F< 0=
	    WHILE
		    10.0e F/
		    R> 1+ >R
	    REPEAT
	    0.5e f+
	    \ Now the number should be in range 10**(prec-1)..10**prec, correct deciamal exp
	    F>D <# EXP @ 1+ 0 DO # LOOP #> \ Convert to double int, to decimal digits
	    DROP SWAP 17 MIN >R SWAP R> CMOVE \ store digits
	    R> 1+ R> -1
	THEN
    THEN	
;

14 VALUE PRECISION

: SET-PRECISION
  1 MAX 17 MIN TO PRECISION ;


: FS. ( F: --- r )
\G Print a floating point number in scientific notation.    
    PAD PRECISION REPRESENT
    SWAP IF '-' EMIT THEN
    0= IF
	DROP ." Inf "
    ELSE
	PAD C@ EMIT '.' EMIT PAD 1+ PRECISION 1- TYPE
	'E' EMIT 1- .
    THEN
;


: F. ( F: --- r )
\G Print a floating point number in normal (non-scientific) notation.    
    FDUP FISNEG IF '-' EMIT THEN
    FABS
    FDUP F0= IF
	FDROP ." 0.0 "
    ELSE
	FDUP 1E-5 F< FDUP PRECISION 10.0E FI** 0.5e F- F< 0= OR IF
	    FS.
	ELSE
	    PAD PRECISION REPRESENT
	    2DROP
	    DUP 0<= IF
		." 0." NEGATE 0 ?DO '0' EMIT LOOP PAD PRECISION TYPE 
	    ELSE
		PAD OVER TYPE '.' EMIT PAD OVER + PRECISION ROT -
		DUP 0> IF TYPE ELSE 2DROP THEN
	    THEN
	    SPACE
	THEN
    THEN
;

: F.S ( --- )
\G Print the contents of the floating point stack.
  FDEPTH DUP 0= IF
    DROP ." Empty "
  ELSE	
    0 DO F0 @ I 1+ FLOATS - F@ F. LOOP
  THEN
;

1E 0E F/ FCONSTANT NAN

: F.R ( u1 u2 --- F: r ---)
\G Print a floating point number with u2 digits after the decimal point and
\G n1 positions total, right-justified (like printf %9.5f notation)
    FDUP FISNEG >R FABS
    10.0e DUP FI** F* 0.5e F+ F>D
    2DUP -$8000000000000000. D= IF
	\ Overflows integer range
	R> 2DROP 2DROP  0 DO '*' EMIT LOOP
    ELSE
	<# ROT 0 ?DO # LOOP '.' HOLD #S R> SIGN #> ROT OVER - 0 MAX SPACES TYPE
    THEN
;

3.141592653589793e
FCONSTANT PI ( F: --- r)
\ The floating point constant PI.

PI 180e F/ FCONSTANT PI/180
180e PI F/ FCONSTANT 180/PI

: FRAD ( F: r1 --- r2)
\G Convert angle from degrees to radians.
  PI/180 F* ;    

: FDEG ( F: r1 --- r2)
\G Convert angle from radians to degrees.
  180/PI F* ;    

: FALOG ( F: r1 --- r2)
\G 10 to the power of X (inverse base 10 logarithm)
    10e FSWAP F** ;

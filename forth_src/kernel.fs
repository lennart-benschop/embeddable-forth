\ This is the file kernel.4th, included by the cross compiler.
\ Copyright 2025 L.C. Benschop Vught, The Netherlands.
\ The program is released under the MIT license
\ There is NO WARRANTY.

\ It is extensively commented as it must serve as an introduction to the
\ construction of Forth compilers. 

\ Lines starting with \G are comments that are included in the glossary.


$200000 CONSTANT DICT-SIZE \ Total dictionary space size in bytes.
$400   CONSTANT RSIZE \ Return stack size in bytes
$400   CONSTANT LSIZE \ Local vars stack size in bytes
$420   CONSTANT FSIZE \ Floating point stack size in bytes


\ PART 0: Dictionary header. 
 $54524F46 ,-T \ $00 Magic number
 0 ,-T         \ $04 Minimum VM level
 0 ,-T         \ $08 Required feature flags.
 0 ,-T         \ $0C image size, where target DP shall be stored.
 DICT-SIZE ,-T \ $10 total dictionary space size,
 0 ,-T         \ $14 entry point (COLD),
 DICT-SIZE RSIZE - LSIZE - FSIZE - ,-T \ $18 S0 initial sp value below all other stacks.
 DICT-SIZE LSIZE - FSIZE - ,-T \ $1C R0 initial rp value.
 DICT-SIZE FSIZE - ,-T  \ $20 L0 initial lp value.
 DICT-SIZE 32 -    ,-T  \ $24 F0 initial fp stack pointer value, give it some room to underflow
 0 ,-T         \ $28 Placeholder for Div-ex function.
 0 ,-T         \ $2C Placeholder for break-ex function.
 0 ,-T         \ $30 Placeholder for Timer-ex function.
 0 ,-T         \ $34 Placeholder for seg-ex function.

\ All of the file after the next line is cross compiled.
CROSS-COMPILE

\ PART 1: SINGLE OPCODE WORDS

\ Their execution parts contain the single opcode plus a return
\ instruction. The compiler compiles the single opcode in-line.

$00 
OPCODE NOOP  ( --- )      
\G Do nothing


$0C 
OPCODE LIT    ( --- lit)
\G Push literal on the stack (literal number is in-line).


$16
OPCODE RTI   ( --- )
\G Return from interrupt.

$18 
OPCODE R>     ( --- x)
\G Pop the top of the return stack and place it on the stack.

$19 
OPCODE >R     ( x ---)
\G Push x on the return stack. 

$1A 
OPCODE R@     ( --- x)
\G x is a copy of the top of the return stack.

$1B 
OPCODE I     ( --- x)
\G x the innermost loop counter.

$1C 
OPCODE I'     ( --- x)
\G x is the limit of the innermost loop.

$1D
OPCODE J     ( --- x)
\G x is the next outer loop counter.

$1E
OPCODE UNLOOP ( --- )
\G Remove one set of loop parameters from the return stack. 

$20
OPCODE DROP   ( x ---)
\G Discard the top item on the stack.    

$21 
OPCODE DUP    ( x --- x x )
\G Duplicate the top cell on the stack.    

$22 
OPCODE SWAP  ( x1 x2 --- x2 x1 )   
\G Swap the two top items on the stack.

$23
OPCODE OVER   ( x1 x2 --- x1 x2 x1)
\G Copy the second cell of the stack. 

$24
OPCODE NIP    ( x1 x2 --- x2)
\G Discard the second item on the stack.

$25
OPCODE TUCK   ( x1 x2 --- x2 x1 x2)
\G Put a compy of the top of stack below the second item.

$26 
OPCODE ROT   ( x1 x2 x3 --- x2 x3 x1 )
\G Rotate the three top items on the stack.  

$27
OPCODE -ROT   ( x1 x2 x3 --- x3 x1 x2 )
\G Rotate the three top items on the stack in the other direction.  

$28
OPCODE 2DROP ( d --- )
\G Discard the top double number on the stack.

$29
OPCODE 2DUP  ( d --- d d)
\G Duplicate the top double number on the stack.

$2A
OPCODE 2SWAP ( d1 d2 --- d2 d1)
\G Swap the top two double numbers on the stack.

$2B
OPCODE 2OVER ( d1 d2 --- d1 d2 d1)
\G Take a copy of the second double number of the stack and push it on the 
\G stack.

$2C
OPCODE PICK  ( u --- x)
\G place a copy of stack cell number u on the stack. 0 PICK is DUP, 1 PICK
\G is OVER etc.

$2D
OPCODE ROLL ( u ---)
\G  Move stack cell number u to the top. 1 ROLL is SWAP, 2 ROLL is ROT etc.

$2E
OPCODE 2* ( w1 --- w2) 
\G Multiply w1 by 2.

$2F
OPCODE 2/  ( n1 --- n2)
\G Divide signed number n1 by 2.

$30 
OPCODE +       ( w1 w2 --- w3)    
\G Add the top two numbers on the stack.

$31 
OPCODE -       ( w1 w2 --- w3)    
\G Subtract the top two numbers on the stack (w2 from w1).

$32
OPCODE AND     ( x1 x2 --- x3)
\G Bitwise and of the top two cells on the stack. 

$33 
OPCODE OR      ( x1 x2 --- x3)
\G Bitwise or of the top two cells on the stack.

$34
OPCODE XOR     ( x1 x2 --- x3)
\G Bitwise exclusive or of the top two cells on the stack.

$35 
OPCODE NEGATE  ( n1 --- -n1)
\G Negate top number on the stack.    

$36
OPCODE INVERT ( x1 --- x2)
\G Invert all the bits of x1 (one's complement)

$37
OPCODE ABS ( n --- u)
\G u is the absolute value of n.

$38
OPCODE * ( w1 w2 --- w3)
\G Multiply single numbers, signed or unsigned give the same result.

$39 
OPCODE UM*     ( u1 u2 --- ud )
\G Multiply two unsigned numbers, giving double result. 

$3A
OPCODE M* ( n1 n2 --- d ) 
\G Multiply the signed numbers n1 and n2, giving the signed double number d.

$3B 
OPCODE UM/MOD ( ud u1 --- urem uquot)
\G Divide the unsigned double number ud by u1, giving unsigned quotient
\G and remainder.    

$3C
OPCODE FM/MOD ( d n1 --- nrem nquot )
\G Divide signed double number d by single number n1, giving quotient and
\G remainder. Round always down (floored division), 
\G remainder has same sign as divisor.

$3D
OPCODE SM/REM ( d n1 --- nrem nquot )
\G Divide signed double number d by single number n1, giving quotient and
\G remainder. Round towards zero, remainder has same sign as dividend.

$3E
OPCODE / ( n1 n2 --- n3) 
\G n3 is n1 divided by n2.

$3F
OPCODE MOD ( n1 n2 --- n3)
\G n3 is the remainder of n1 and n2.

$40
OPCODE D+ ( d1 d2 --- d3)
\G Add the double numbers d1 and d2.

$41
OPCODE DNEGATE ( d1 --- d2)
\G Negate the top double number on the stack.

$42
OPCODE D- ( d1 d2 --- d3)
\G Subtract the double numbers d1 and d2.

$43
OPCODE D* ( d1 d2 --- d3)
\G Multiply the double numbers d1 and d2.

$44
OPCODE UD/MOD ( ud1 ud2 --- ud3 ud4)
\G divide double number ud1 by ud2, ud3 is quotient and ud4 is modulus.

$45
OPCODE D/MOD ( d1 d2 --- d3 d4)
\G divide double number d1 by d2, d3 is quotient and d4 is modulus.

$46 
OPCODE LSHIFT ( x1 u --- x2)
\G Shift x1 left by u bits, zeros are added to the right.

$47
OPCODE RSHIFT ( x1 u --- x2)
\G Shift x1 right by u bits, zeros are added to the left.

$48 
OPCODE 0=    ( x --- f)
\G f is true if and only if x is 0.

$49
OPCODE 0<>    ( x --- f)
\G f is true if and only if x is not 0.

$4A
OPCODE 0<   ( n --- f)
\G f is true if and only if n is less than 0.

$4B
OPCODE 0>   ( n --- f)
\G f is true if and only if n is greater than 0.

$4C
OPCODE 0<=   ( n --- f)
\G f is true if and only if n is less than or euqal to 0.

$4D
OPCODE 0>=   ( n --- f)
\G f is true if and only if n is greater than or euqal to 0.

$4E
OPCODE D2* ( d1 --- d2) 
\G Multiply d1 by 2.

$4F
OPCODE D2/  ( d1 --- d2)
\G Divide signed double precision number n1 by 2.

$50
OPCODE =    ( x1 x2 --- f)
\G f is true if and only if x1 is equal to x2.

$51
OPCODE <>   ( x1 x2 --- f)
\G f is true if and only if x1 is not equal to x2.

$52
OPCODE <      ( n1 n2 --- f)
\G f is true if and only if signed number n1 is less than n2. 

$53
OPCODE >    ( n1 n2 --- f)
\G f is true if and only if the signed number n1 is greater than n2. 

$54
OPCODE <=   ( n1 n2 --- f)
\G f is true if and only if signed number n1 is less than or equal ton2. 

$55
OPCODE >=   ( n1 n2 --- f)
\G f is true if and only if the signed number n1 is greater than or equal to n2.

$56
OPCODE 1+ ( w1 --- w2 )
\G Add 1 to the top of the stack.

$56
OPCODE CHAR+ ( c-addr1 --- c-addr2)
\G c-addr2 is the next character address after c-addr1.

$57
OPCODE 1- ( w1 --- w2)
\G Subtract 1 from the top of the stack.

$57
OPCODE CHAR-  ( c-addr1 --- c-addr2)
\G c-addr2 is the previous character address before c-addr1.

$58 
OPCODE U<      ( u1 u2 ---- f)
\G f is true if and only if unsigned number u1 is less than u2.   

$59
OPCODE U>  ( u1 u2 --- f)
\G f is true if and only if the unsigned number u1 is greater than u2.

$5A
OPCODE U<=      ( u1 u2 ---- f)
\G f is true if and only if unsigned number u1 is less than or equal to u2.   

$5B
OPCODE U>=  ( u1 u2 --- f)
\G f is true if and only if the unsigned number u1 is greater than or euqal to u2.

$5C
OPCODE WITHIN ( u1 u2  u3 --- f)
\G f is true if u1 is greater or equal to u2 and less than u3

$5D
OPCODE CELL+ ( a-addr1 --- a-addr2)
\G a-addr2 is the address of the next cell after a-addr2.

$5E
OPCODE CELL- ( a-addr1 --- a-addr2)
\G a-addr2 is the address of the previous cell before a-addr1.

$5F
OPCODE CELLS ( n2 --- n1)
\G n2 is the number of address units occupied by n1 cells.

$60 
OPCODE C@      ( c-addr --- c)
\G Fetch character c at c-addr.

$61
OPCODE C! ( c c-addr --- )  
\G Store character c at c-addr

$62 
OPCODE H@      ( h-addr --- w)
\G Fetch halfword  w at h-addr.

$63
OPCODE H! ( x h-addr --- )
\G Store halfword x at h-addr

$64 
OPCODE @       ( a-addr --- x)
\G Fetch cell x at a-addr.
 
$65
OPCODE ! ( x a-addr --- )
\G Store cell x at a-addr

$66
OPCODE 2@ ( a-addr --- d )
\G Fetch double number d at a-addr. 

$67
OPCODE 2! ( d a-addr --- )
\G Store the double number d at a-addr.

$68
OPCODE PC@      ( c-addr --- c)
\G Fetch character c at absolute address c-addr.

$69
OPCODE PC! ( c c-addr --- )  
\G Store character c at absolute address c-addr

$6A 
OPCODE P@       ( a-addr --- x)
\G Fetch cell x at absolute address a-addr.
 
$6B
OPCODE P! ( x a-addr --- )
\G Store cell x at absolute address a-addr

$6C
OPCODE CMOVE ( c-addr1 c-addr2 u --- )
\G Copy u bytes starting at c-addr1 to c-addr2, proceeding in ascending
\G order.

$6D
OPCODE CMOVE> ( c-addr1 c-addr2 u --- )
\G Copy a block of u bytes starting at c-addr1 to c-addr2, proceeding in
\G descending order.

$6E
OPCODE FILL ( c-addr u c ---)
\G Fill a block of u bytes starting at c-addr with character c.

$6F
OPCODE ALIGNED ( c-addr --- a-addr )
\G a-addr is the first aligned address after c-addr.

$70
OPCODE COMPARE ( addr1 u1 addr2 u2 --- diff ) 
\G Compare two strings. diff is negative if addr1 u1 is smaller, 0 if it 
\G is equal and positive if it is greater than addr2 u2.

$71
OPCODE SCAN ( c-addr1 u1 c --- c-addr2 u2 )
\G Find the first occurrence of character c in the string c-addr1 u1
\G c-addr2 u2 is the remaining part of the string starting with that char.
\G It is a zero-length string if c was not found.

$72
OPCODE SKIP ( c-addr1 u1 c --- c-addr2 u2 )
\G Find the first character not equal to c in the string c-addr1 u1
\G c-addr2 u2 is the remaining part of the string starting with the
\G nonmatching char. It is a zero-length string if no other chars found.

$73
OPCODE (FIND) ( c-addr u nfa --- cfa/c-addr f)
\G Core function of SEARCH-WORDLIST, starting at one hash chain.

$74
OPCODE H@U     ( h-addr --- w)
\G Fetch halfword  w at h-addr.

$75 
OPCODE H!U ( x h-addr --- )
\G Store halfword x at h-addr

$76
OPCODE T@U     ( h-addr --- w)
\G Fetch triplebyte  w at h-addr.

$77
OPCODE T!U ( x h-addr --- )
\G Store triplebyte x at h-addr

$78 
OPCODE @U  ( c-addr --- x)
\G Fetch cell x at c-addr that can be unaligned.
 
$79
OPCODE !U ( x c-addr --- )
\G Store cell x at c-addr that can be unaligned.

$7A
OPCODE +! ( w a-addr ---)
\G Add w to the contents of the cell at a-addr.

$7B
OPCODE D= ( d1 d2 --- f)
\G Set f is true if d1 is equal to d2

$7C
OPCODE D< ( d1 d2 --- f)
\G Set f is true if d1 is less than d2

$7D
OPCODE DU< ( ud1 ud2 --- f)
\G Set f is true if ud1 is less than ud2

$80
OPCODE FALSE 
\G Constant 0 for false

$85
OPCODE TRUE
\G Constant -1 for true

\ We do not include dictionary words for the constants -1, 0, 1, 2, 3 and 4
\ that have their own opcodes, as the LITERAL word will generate these
\ opcodes already.

\ Floating point opcodes moved to float.4th

$C000
2OPCODE KEY ( --- c) 
\G Input the character c from the terminal.

$C001
2OPCODE KEY? ( --- f)
\G f is true if and only if a key is pressed and KEY will return immediately

$C002
2OPCODE ACCEPT ( c-addr n1 --- n2 )
\G Read a line from the terminal to a buffer starting at c-addr with 
\G length n1. n2 is the number of characters read,

$C003
2OPCODE EMIT ( c ---)
\G Output the character c to the terminal.

$C004
2OPCODE TYPE ( c-addr1 u --- )
\G Output the string starting at c-addr and length u to the terminal. 

$C005
2OPCODE BYE  ( ---)  
\G Terminate the execution of SOD-32 Forth, return to OS.

$C009
2OPCODE SETTERM ( f ---)
\G Set term in normal or raw mode.

$C00A
2OPCODE SETALARM ( n ---)
\G create an alarm after n microseconds


$C1
OPCODE SP@      ( --- a-addr)
\G Return the address of the stack pointer (before SP@ was executed).

$C2
OPCODE SP!      ( a-addr ---)
\G Set the stack pointer to a-addr.

$C3
OPCODE RP@      ( --- a-addr)
\G Return the address of the return stack pointer.

$C4
OPCODE RP!      ( a-addr ---)
\G Set the return stack pointer to a-addr.

$C5
OPCODE LP@      ( --- a-addr)
\G Return the address of the locals stack pointer.

$C6
OPCODE LP!      ( a-addr ---)
\G Set the locals stack pointer to a-addr.

$C7
OPCODE FP@      ( --- a-addr)
\G Return the address of the floating point stack pointer.

$C8
OPCODE FP!      ( a-addr ---)
\G Set the floating point stack pointer to a-addr.

\ PART 2: RUNTIME PARTS THE VARIOUS DEFINITION CLASSES.

\ Only VARIABLES (or CREATE) need a runtime part in this system.
\ As this is a native code compiler, colon definitions have no runtime
\ part.For variables, a call
\ to DOVAR is compiled. DOVAR pushes the return address (the address
\ where the data of the variable is stored) on the stack.
\ DOCON is the runtime part for constants, it fetches the value from the
\ address.

: DOVAR  ( --- a-addr)
\G Runtime part of variables.
  R> ; 

: DOCON  ( --- x )
\G Runtime part of constants.
  R> @ ;

\ PART 3: SIMPLE DEFINITIONS

\ This is a class of words, which would be written in machine code
\ on most non-native code systems. Many contain just a few words, so they
\ are implemented as macros. 
\ Compared to SDD32 we have many more primitives, hence fewer words in
\ this section.


: CR  ( --- )
\G Output a newline to the terminal.
   13 EMIT 10 EMIT ;


\ These variables are stored inside the dictionary header.

$18 CONSTANT S0 ( --- a-addr)
\G Variable that holds the bottom address of the stack.

$1C CONSTANT R0 ( --- a-addr)
\G Variable that holds the bottom address of the return stack.

$20 CONSTANT L0 ( --- a-addr)
\G Variable that holds the bottom address of the locals stack.

$24 CONSTANT F0 ( --- a-addr)
\G Variable that holds the bottom address of the floating point stack.


: DEPTH ( --- n )
\G n is the number of cells on the stack (before DEPTH was executed). 
  SP@ S0 @ SWAP - 2/ 2/ ; 

M: CHARS ( n1 --- n2) 
\G n2 is the number of address units occupied by n1 characters.
; \ A no-op

M: COUNT  ( c-addr1 --- c-addr2 c)
\G c-addr2 is the next address after c-addr1 and c is the character
\G stored at c-addr1.
\G This word is intended to be used with 'counted strings' where the
\G first character indicates the length of the string.
   DUP 1 + SWAP C@ ;

: (.") ( --- )
\G Runtime part of ."
\ This expects an in-line counted string. 
  R> COUNT OVER OVER TYPE +  >R ;
: (S")  ( --- c-addr u )
\G Runtime part of S"
\ It returns address and length of an in-line counted string.
  R> COUNT OVER OVER + >R ;


32 
CONSTANT BL ( --- 32 )
\G Constant 32, the blank character


M: OFF ( a-addr ---)
\G Store FALSE at a-addr.
   0 SWAP ! ;

M: ON ( a-addr ---)
\G Store TRUE at a-addr.
   -1 SWAP ! ;




\ The next few words manipulate addresses in a system-independent way.
\ Use CHAR+ instead of 1+ and it will be portable to systems where you
\ have to add something different from 1.
 


\ Double numbers occupy two cells in memory and on the stack.
\ The most significant half on the number is in the first memory
\ cell or in the top cell on the stack (which is also the first address).


: ?DUP ( n --- 0 | n n)
\G Duplicate the top cell on the stack, but only if it is nonzero.
   DUP IF DUP THEN ;

: MIN ( n1 n2 --- n3)
\G n3 is the minimum of n1 and n2. 
   OVER OVER > IF SWAP THEN DROP ;

: MAX ( n1 n2 --- n3)
\G n3 is the maximum of n1 and n2.
   OVER OVER < IF SWAP THEN DROP ;



: DABS ( d --- ud)
\G ud is the absolute value of d.
   DUP 0< IF DNEGATE THEN ;


: */MOD ( n1 n2 n3 --- nrem nquot)
\G Multiply signed numbers n1 by n2 and divide by n3, giving quotient and
\G remainder. Intermediate result is double.
  >R M* R> FM/MOD ;

: */    ( n1 n2 n3 --- n4 )
\G Multiply signed numbers n1 by n2 and divide by n3, giving quotient n4.
\G Intermediate result is double.
  */MOD SWAP DROP ;

M: S>D  ( n --- d)
\G Convert single number to double number. 
   DUP 0< ;

: /MOD  ( n1 n2 --- nrem nquot)
\G Divide signed number n1 by n2, giving quotient and remainder.
   SWAP S>D ROT FM/MOD ;

: EXECUTE ( xt ---) 
\G Execute the word with execution token xt. 
\ Return from EXECUTE goes to xt pushed on the ret stack by >R, return from
\ the word x1 returns to definition that calls EXECUTE
  >R ;

: ?THROW ( f n --- )
\G Perform n THROW if f is nonzero.
  SWAP IF THROW ELSE DROP THEN ;  

\ PART 4: NUMERIC OUTPUT WORDS.

VARIABLE BASE ( --- a-addr)
\G Variable that contains the numerical conversion base.

$0C CONSTANT DP   ( --- a-addr)
\G Variable that contains the dictionary pointer. New space is allocated
\G from the address in DP

VARIABLE HLD ( --- a-addr)
\G Variable that holds the address of the numerical output conversion
\G character.

VARIABLE DPL ( --- a-addr)
\G Variable that holds the decimal point location for numerical conversion.

: DECIMAL ( --- )
\G Set numerical conversion to decimal.
  10 BASE ! ;

: HEX     ( --- )
\G Set numerical conversion to hexadecimal.
  16 BASE ! ;

: SPACE  ( ---)
\G Output a space to the terminal.
  32 EMIT ;

: SPACES ( u --- )
\G Output u spaces to the terminal.
  ?DUP IF 0 DO SPACE LOOP THEN ;

M: HERE ( --- c-addr )
\G The address of the dictionary pointer. New space is allocated here.
  DP @ ;

: PAD ( --- c-addr )
\G The address of a scratch pad area. Right below this address there is
\G the numerical conversion buffer. 
  HERE 84 + ;

: MU/MOD ( ud u --- urem udquot )
\G Divide unsigned double number ud by u and return a double quotient and
\G a single remainder.
  >R 0 R@ UM/MOD R> SWAP >R UM/MOD R> ;

\ The numerical conversion buffer starts right below PAD and grows down.
\ Characters are added to it from right to left, as as the div/mod algorithm
\ to convert numbers to an arbitrary base produces the digits from right to
\ left.

: HOLD ( c ---)
\G Insert character c into the numerical conversion buffer.
  1 NEGATE HLD +! HLD @ C! ;

: # ( ud1 --- ud2)
\G Extract the rightmost digit of ud1 and put it into the numerical
\G conversion buffer.
  BASE @ MU/MOD ROT DUP 9 > IF 7 + THEN 48 + HOLD ; 

: #S ( ud --- 0 0 )
\G Convert ud by repeated use of # until ud is zero.
  BEGIN # OVER OVER OR 0= UNTIL ;

: SIGN ( n ---)
\G Insert a - sign in the numerical conversion buffer if n is negative.
  0< IF 45 HOLD THEN ;

: <# ( --- )
\G Reset the numerical conversion buffer.
  PAD HLD ! ;

: #> ( ud --- addr u )
\G Discard ud and give the address and length of the numerical conversion 
\G buffer. 
  DROP DROP HLD @ PAD OVER - ;

: D. ( d --- )
\G Type the double number d to the terminal.
  SWAP OVER DABS <# #S ROT SIGN #> TYPE SPACE ;

: U. ( u ---)
\G Type the unsigned number u to the terminal.
  0 D. ;

: . ( n ---)
\G Type the signed number n to the terminal.
  S>D D. ;

\ PART 5: MEMORY BLOCK MOVE AND RELATED WORDS. 


: MOVE ( c-addr1 c-addr2 u --- )
\G Copy a block of u bytes starting at c-addr1 to c-addr2. Order is such
\G that partially overlapping blocks are copied intact.
  >R OVER OVER U< IF R> CMOVE> ELSE R> CMOVE THEN ; 


\ PART 6: FILE ACCESS WORDS.

00 
CONSTANT R/O ( --- mode)
\G Read only file access mode.

02 
CONSTANT W/O ( --- mode)
\G Write only file access mode.

04 
CONSTANT R/W ( --- mode)
\G Read write file access mode.

M: BIN ( mode1 --- mode2)
\G Modify the R/O W/O or R/W mode so that it applies to binary files. 
  1 + ;


\ All open files are known by a file-id, which is a number between 1 and 20.
\ All file operations return an io result (ior), which is zero if the
\ operation was successful and nonzero in the case of an error.


\ File ID's must be nonzero, therefore 1 higher than those used
\ by the OS.

$C010
2OPCODE OPEN-FILE ( c-addr u mode --- fid ior)
\G Open the file with the name starting at c-addr and with length u.
\G File must already exist unless open mode is write only.
\G Return the file-ID and the IO result. (ior=0 if success)

: CREATE-FILE ( c-addr u mode --- fid ior)
\G Create a new file with the name starting at c-addr with length u. 
\G Return the file-ID and the IO result. (ior=0 if success)
  1 AND 02 + OPEN-FILE ;

$C011
2OPCODE CLOSE-FILE ( fid --- ior)
\G Close the open file described by fid.

$C017
2OPCODE WRITE-LINE ( c-addr u fid --- ior)
\G Write the string at addr c-addr with length u to the file described by
\G fid. Append the end of line character to it.

$C016
2OPCODE READ-LINE ( c-addr u1 fid --- u2 flag ior) 
\G Read a line from the file described by fid to a buffer at c-addr that
\G is u1+2 characters long. The line is at most u1 characters long.
\G flag is 0 at the end of file (no line could be read) TRUE otherwise.
\G (ior is 0 in this case.)
\G n2 is the length of the line read, 

$C013
2OPCODE WRITE-FILE  ( c-addr u fid --- ior)
\G Write a block of u bytes starting at c-addr to the file described by
\G fid. (file must be opened in BIN mode).

$C012
2OPCODE  READ-FILE   ( c-addr u1 fid --- u2 ior)
\G Read a block of u1 bytes starting at c-addr from the file described by
\G fid. (file must be opened in BIN mode). u2 is the number of bytes 
\G actually read. This is less than u1 at the end of the file. 

$C018
2OPCODE DELETE-FILE ( c-addr u --- ior)
\G Delete the file with a name starting at c-addr with length u.

$C014
2OPCODE REPOSITION-FILE ( ud fid --- ior)
\G Set the file position of the open file described by fid to ud.

$C015
2OPCODE FILE-POSITION  ( fid --- ud ior)
\G ud is the file position of the open file described by fid.

$C019
2OPCODE SYSTEM ( c-addr u --- ior)
\G Execute a the string at c-addr with length u as a system command.

$C01A
2OPCODE FILE-SIZE  ( fid --- ud ior)
\G ud is the file size of the file described by fid. 

$C01D
2OPCODE CHDIR ( c-addr u ---)
\G Change to the directiory given by the string at c-addr length u.

$C01E
2OPCODE ARG@ ( c-addr u1 n --- c-addr u2)
\G Get nth command line argument into buffer given by c-addr u1, return actual
\G length of argument. First argument = 0.

\ PART 7: SOURCE INPUT WORDS.

VARIABLE TERMMODE

: SETRAW ( --- )
\G Make the input raw mode.
   1 SETTERM 1 TERMMODE ! ;

: NONRAW ( --- )
\G Make the input nonraw.
   0 SETTERM 0 TERMMODE ! ;

VARIABLE TIB ( --- addr) 
\G is the standard terminal input buffer.
80 CHARS-T ALLOT-T

VARIABLE SPAN ( --- addr)
\G This variable holds the number of characters read by EXPECT.

VARIABLE #TIB ( --- addr)
\G This variable holds the number of characters in the terminal input buffer.

VARIABLE >IN  ( --- addr)
\G This variable holds an index in the current input source where the next word 
\G will be parsed.

VARIABLE SID  ( --- addr)
\G This variable holds the source i.d. returned by SOURCE-ID.

VARIABLE SRC  ( --- addr)
\G This variable holds the address of the current input source.

VARIABLE #SRC ( --- addr)
\G This variable holds the length of the current input source.

VARIABLE LOADLINE ( --- addr)
\G This variable holds the line number in the file being included.


: EXPECT ( c-addr u --- )
\G Read a line from the terminal to a buffer at c-addr with length u.
\G Store the length of the line in SPAN. 
  ACCEPT SPAN ! ;

: QUERY ( --- )
\G Read a line from the terminal into the terminal input buffer.
  TIB 80 ACCEPT #TIB ! ;

: SOURCE ( --- addr len)
\G Return the address and length of the current input source.
   SRC @ #SRC @ ;

: SOURCE-ID ( --- sid)
\G Return the i.d. of the current source i.d., 0 for terminal, -1 
\G for EVALUATE and positive number for INCLUDE file.
   SID @ ;

: REFILL ( --- f)
\G Refill the current input source when it is exhausted. f is
\G true if it was successfully refilled.
  SOURCE-ID -1 = IF
   0 \ Not refillable for EVALUATE
  ELSE
   SOURCE-ID IF
    SRC @ 256 SOURCE-ID READ-LINE -37 ?THROW
    SWAP #SRC ! 0 >IN !
    #SRC @ IF SOURCE OVER + SWAP DO I C@ 9 = IF 32 I C! THEN LOOP THEN
    1 LOADLINE +!
    \ Change tabs to space. 
    \ flag from READ-LINE is returned (no success at EOF)
   ELSE
       QUERY #TIB @ #SRC ! 0 >IN ! -1 \ Always successful from terminal.
   THEN 
  THEN
; 


: PARSE ( c --- addr len )
\G Find a character sequence in the current source that is delimited by
\G character c. Adjust >IN to 1 past the end delimiter character.
  >R SOURCE >IN @ - SWAP >IN @ + R> OVER >R >R SWAP 
  R@ SKIP OVER R> SWAP >R SCAN IF 1 >IN +! THEN 
  DUP R@ - R> SWAP 
  ROT R> - >IN +! ;

: PLACE ( addr len c-addr --- )
\G Place the string starting at addr with length len at c-addr as
\G a counted string.
  OVER OVER C! 
  1+ SWAP CMOVE ;


: WORD ( c --- addr )
\G Parse a character sequence delimited by character c and return the
\G address of a counted string that is a copy of it. The counted
\G string is actually placed at HERE. The character after the counted
\G string is set to a space.
  PARSE HERE PLACE HERE BL HERE COUNT + C! ;

VARIABLE CURFILENAME ( --- addr)
\G Buffer to store currently loaded file name.
77 ALLOT-T

: OPEN ( "ccc" --- )
\G Make the specified file the current file.
 BL WORD COUNT CURFILENAME PLACE ;

VARIABLE CAPS ( --- a-addr)
\G This variable contains a nonzero number if input is case insensitive.

: UPPERCASE? ( --- )
\G Convert the parsed word to uppercase is CAPS is true.
   CAPS @ HERE C@ AND IF
   HERE COUNT 0 DO 
    DUP I + C@ DUP 96 > SWAP 123 < AND IF DUP I + DUP C@ 32 - SWAP C! THEN
   LOOP DROP
  THEN
;


\ PART 8: INTERPRETER HELPER WORDS

\ First we need FIND and related words.

\ Each word list consists of a number of linked list of definitions (number
\ is a power of 2). Hashing
\ is used to speed up dictionary search. All names in the dictionary
\ are at aligned addresses and FIND is optimized to compare one 4-byte
\ cell at a time.  

\ Dictionary definitions are built as follows:
\ 
\ LINK field: 1 cell, aligned, contains name field of previous word in thread.
\ NAME field: counted string of at most 31 characters.
\             bits 5-7 of length byte have special meaning.
\                   7 is always set to mark start of name ( for >NAME)
\                   6 is set if the word is immediate.
\                   5 is set if the word is a macro.
\ CODE field: first aligned address after name, is execution token for word.
\             here the executable code for the word starts. (is 1 cell for
\             variables etc.)
\ PARAMETER field: (body) Contains the data of constants and variables etc.


VARIABLE FORTH-WORDLIST ( --- addr)
33 CELLS-T ALLOT-T
\G This array holds pointers to the last definition of each thread in the Forth
\G word list.

VARIABLE LAST ( --- addr)
\G This variable holds a pointer to the last definition created.

VARIABLE CONTEXT 28 ALLOT-T ( --- a-addr)
\G This variable holds the addresses of up to 8 word lists that are
\G in the search order.

VARIABLE #ORDER ( --- addr)
\G This variable holds the number of word list that are in the search order.

VARIABLE CURRENT ( --- addr)
\G This variable holds the address of the word list to which new definitions
\G are added.

: HASH ( c-addr u #threads --- n)
\G Compute the hash function for the name c-addr u with the indicated number
\G of threads.
  >R OVER C@ 1 LSHIFT OVER 1 > IF ROT CHAR+ C@ 2 LSHIFT XOR ELSE ROT DROP 
   THEN XOR 
  R> 1- AND 
;  

: SEARCH-WORDLIST ( c-addr u wid --- 0 | xt 1 xt -1)
\G Search the wordlist with address wid for the name c-addr u.
\G Return 0 if not found, the execution token xt and -1 for non-immediate
\G words and xt and 1 for immediate words.    
  CELL+ >R
  2DUP R@ @ HASH 1+ CELLS R> + @ \ Get the right thread.
  DUP IF
    (FIND) DUP 0= IF 2DROP 0 THEN EXIT
  THEN
  2DROP DROP 0 \ Not found.
;

: FIND ( c-addr --- c-addr 0| xt 1|xt -1 )
\G Search all word lists in the search order for the name in the
\G counted string at c-addr. If not found return the name address and 0.
\G If found return the execution token xt and -1 if the word is non-immediate
\G and 1 if the word is immediate.
  #ORDER @ DUP 1 > IF
   CONTEXT #ORDER @ 1- CELLS + DUP @ SWAP 4 - @ = 
  ELSE 0 THEN
  IF 1- THEN \ If last wordlist is double, don't search it twice.
  BEGIN
   DUP
  WHILE
   1- >R
   DUP COUNT 
   R@ CELLS CONTEXT + @ SEARCH-WORDLIST
   DUP
   IF
    R> DROP ROT DROP EXIT \ Exit if found.     
   THEN 
   DROP R>
  REPEAT
;

\ The following words are related to numeric input.

: DIGIT? ( c -- 0| c--- n -1)
\G Convert character c to its digit value n and return true if c is a
\G digit in the current base. Otherwise return false.
  48 - DUP 0< IF DROP 0 EXIT THEN
  DUP 9 > OVER 17 < AND IF DROP 0 EXIT THEN
  DUP 9 > IF 7 - THEN
  DUP BASE @ < 0= IF DROP 0 EXIT THEN
  -1  
;

: >NUMBER ( ud1 c-addr1 u1 --- ud2 c-addr2 u2 )
\G Convert the string at c-addr with length u1 to binary, multiplying ud1
\G by the number in BASE and adding the digit value to it for each digit.
\G c-addr2 u2 is the remainder of the string starting at the first character
\G that is no digit.
  BEGIN
   DUP
  WHILE
   1 - >R
   COUNT DIGIT? 0= 
   IF
    R> 1+ SWAP 1 - SWAP  EXIT
   THEN  
   SWAP >R 
   >R 
   SWAP BASE @ UM* ROT BASE @ * 0 SWAP D+ \ Multiply ud by base.
   R> 0 D+                                \ Add new digit.
   R> R> 
  REPEAT
;  
  
: CONVERT ( ud1 c-addr1 --- ud2 c-addr2)
\G Convert the string starting at c-addr1 + 1 to binary. c-addr2 is the
\G address of the first non-digit. Digits are added into ud1 as in >NUMBER
  1 - -1 >NUMBER DROP ;

: NUMBER? ( c-addr ---- d f)
\G Convert the counted string at c-addr to a double binary number.
\G f is true if and only if the conversion was successful. DPL contains
\G -1 if there was no point in the number, else the position of the point 
\G from the right. Special prefixes: # means decimal, $ means hex.
  -1 DPL !
  BASE @ >R
  COUNT   
  OVER C@ 45 = DUP >R IF 1 - SWAP 1 + SWAP THEN \ Get any - sign 
  OVER C@ 36 = IF 16 BASE ! 1 - SWAP 1 + SWAP THEN   \ $ sign for hex.
  OVER C@ 35 = IF 10 BASE ! 1 - SWAP 1 + SWAP THEN   \ # sign for decimal
  OVER C@ 39 = IF R> DROP R> BASE ! DROP 1+ C@ 0 -1 EXIT THEN \ ' for character literal.  DUP  0 > 0= IF  R> DROP R> BASE ! 0 EXIT THEN   \ Length 0 or less?
  >R >R 0 0 R> R>
  BEGIN  
   >NUMBER  
   DUP IF OVER C@ 46 = IF 1 - DUP DPL ! SWAP 1 + SWAP ELSE \ handle point. 
         R> DROP R> BASE ! 0 EXIT THEN   \ Error if anything but point  
       THEN    
  DUP 0= UNTIL DROP DROP R> IF DNEGATE THEN    
  R> BASE ! -1  
;

\ PART 9: THE COMPILER

VARIABLE ERROR$ ( --- a-addr )
\G Variable containing string address of ABORT" message.

VARIABLE HANDLER ( --- a-addr )
\G Variable containing return stack address where THROW should return.

: (ABORT") ( f -- - )
\G Runtime part of ABORT"
           IF R>  ERROR$ ! -2 THROW  
           ELSE R> COUNT + >R THEN ;

: THROW ( n --- )
\G If n is nonzero, cause the corresponding CATCH to return with n.
DUP IF
 HANDLER @ IF
  HANDLER @ RP!
  RP@ 4 + @ HANDLER ! \ point to previous exception frame.
  R>                  \ get old stack pointer. 
  SWAP >R SP! DROP R> \ save throw code temp. on ret. stack set old sp.
  R> DROP             \ remove address of handler.
                      \ return stack points to return address of CATCH.
     R> LP! R> FP! 
 ELSE
  WARM \ Warm start if no exception frame on stack.
 THEN
ELSE
 DROP \ continue if zero.
THEN  
;  

: DIV-EX ( --- )
\G Divide excpetion handler
  -10 THROW ;

: TIMER-EX ( --- )
\G Timer interrupt handler
  RTI ;

: BREAK-EX ( --- )
\G Break key handler
  -28 THROW ;

: SEG-EX ( --- )
\G Break key handler
  -28 THROW ;


: CATCH ( xt --- n )
\G Execute the word with execution token xt. If it returns normally, return
\G 0. If it executes a THROW, return the throw parameter.
 FP@ >R LP@ >R \ push FP and LP stack pointers.    
 HANDLER @ >R  \ push handler on ret stack.
 SP@ >R        \ push stack pointer on ret stack,
 RP@ HANDLER ! 
 EXECUTE 
 RP@ 4 + @ HANDLER ! \ set handler to previous exception frame.
 R> DROP R> DROP R> DROP R> DROP \ remove exception frame.
 0 \ return 0
;

: ALLOT ( n --- )
\G Allot n extra bytes of memory, starting at HERE to the dictionary.
  DP +! SP@ HERE - 128 < -5 ?THROW ;

: , ( x --- )
\G Append cell x to the dictionary at HERE.
  HERE !U 1 CELLS ALLOT ;

: T, ( x --- )
\G Append triple byte x to the dictionary at HERE.
  HERE T!U 3 ALLOT ;

: H, ( x --- )
\G Append halfword x to the dictionary at HERE.
  HERE H!U 2 ALLOT ;

: C, ( n --- )
\G Append character c to the dictionary at HERE.
  HERE C! 1 ALLOT ;

: ALIGN ( --- )
\G Add as many bytes to the dictionary as needed to align dictionary pointer.
  BEGIN HERE 03 AND WHILE 0 C, REPEAT ;

: >NAME ( addr1 --- addr2 )
\G Convert execution token addr1 (address of code) to address of name.
  CELL- BEGIN 1- DUP C@ 128 AND UNTIL ;

: NAME> ( addr1 --- addr2 )
\G Convert address of name to address of code.
  COUNT 31 AND + ALIGNED CELL+ ;

: HEADER ( --- )
\G Create a header for a new definition without a code field. 
  ALIGN 0 , \ Create link field.
  HERE LAST !         \ Set LAST so definition can be linked by REVEAL
  32 WORD UPPERCASE?
           DUP FIND IF ." Redefining: " HERE COUNT TYPE CR THEN DROP
                       \ Give warning if existing word redefined.
  DUP COUNT CURRENT @ CELL+ @ HASH 2 + CELLS CURRENT @ + @ HERE CELL- !
                       \ Set link field to point to the right thread
  C@ 1+ HERE C@ 128 + HERE C! ALLOT ALIGN 
	   \ Allot the name and set bit 7 in length byte.
  0 , \ Create setter/macro size field.
; 

: REVEAL ( --- )
\G Add the last created definition to the CURRENT wordlist.
  LAST @ DUP COUNT 31 AND \ Get address and length of name 
  CURRENT @ CELL+ @ HASH        \ compute hash code.
  2 + CELLS CURRENT @ + ! ;

: CREATE ( "ccc" --- )
\G Create a definition that returns its parameter field address when 
\G executed. Storage can be added to it with ALLOT.
  HEADER REVEAL POSTPONE DOVAR ;

: VARIABLE ( "ccc" --- )
\G Create a variable where 1 cell can be stored. When executed it
\G returns the address.
  CREATE 0 , ;

: CONSTANT ( x "ccc" ---)
\G Create a definition that returns x when executed.
\ Definition contains lit & return in its code field.
  HEADER REVEAL POSTPONE DOCON , ;


VARIABLE STATE ( --- a-addr)
\G Variable that holds the compiler state, 0 is interpreting 1 is compiling.

VARIABLE TO-STATE ( --- a-addr)
\G Variable that holds the desired behavior of VALUEs and similar words.

: ]  ( --- )
\G Start compilation mode.
  1 STATE ! ;

: [  ( --- )
\G Leave compilation mode. 
  0 STATE ! ; IMMEDIATE

: TO ( --- )
\G Store a new value into a VALUE object.    
    1 TO-STATE ! ; IMMEDIATE

: +TO ( --- )
\G Add to the value of a VALUE object.    
    2 TO-STATE ! ; IMMEDIATE

\ There are several literal opcodes, for positive and negative numbers and
\ for various size numbers. 
: LITERAL ( n --- ) 
\G Add a literal to the current definition.
    DUP 0< IF
	NEGATE
	DUP 1 = IF
	    DROP $85 C, \ special opcode for constant -1
	ELSE
	    DUP $100 U< IF
		$07 C, C, \ 8-bit negative literal opcode.
	    ELSE
		DUP $10000 U< IF
		    $09 C, H, \ 16-bit negative literal opcdeo.
		ELSE
		    DUP $1000000 U< IF
			$0B C, T, \ 24-bit negative literal opcode
		    ELSE
			$0C C, , \ Full 32-bit literal
		    THEN
		THEN
	    THEN
	THEN
    ELSE
	DUP 5 < IF
	    $80 + C, \ Dedicated opcodes for constants 0..4
	ELSE
	    DUP $100 < IF
		$06 C, C, \ 8-bit literal opcode
	    ELSE
		DUP $10000 < IF
		    $08 C, H, \ 16-bit literal opcode
		ELSE
		    DUP $1000000 < IF
			$0A C, T, \ 24-bit literal opcode
		    ELSE
			$0C C, , \ Full 32-bit literal
		    THEN
		THEN
	    THEN
	THEN
    THEN
; IMMEDIATE

: EXPAND-MACRO ( xt --- )
\G Copy the code contained in the definition xt into the current definition.
    DUP CELL- @ \ Get length of macro.
    SWAP HERE ROT DUP ALLOT CMOVE \ Copy and allot
;

: COMPILE, ( xt --- )
\G Add the execution semantics of the definition xt to the current definition.
  DUP >NAME C@ 32 AND 
  IF 
   EXPAND-MACRO
  ELSE
   DUP @ LIT DOVAR = IF
    4 + LITERAL \ Convert variable to literal.
   ELSE
    DUP @ LIT DOCON = 
    IF
     4 + @ LITERAL  \ Convert constant to literal.
    ELSE
     $01 C, T,      \ Lay down subroutine call. 
    THEN
   THEN
  THEN
;

VARIABLE CSP ( --- a-addr )
\G This variable is used for stack checking between : and ; 

VARIABLE 'LEAVE ( --- a-addr) 
\ This variable is used for LEAVE address resolution.

VARIABLE LVARS ( --- a-addr)
\G Variable containing the number of Local variable of the currently compiled
\G definition.

: !CSP ( --- )
\G Store current stack pointer in CSP.
   SP@ CSP ! ;

: ?CSP ( --- )
\G Check that stack pointer is equal to value contained in CSP. 
   SP@ CSP @ - -22 ?THROW ;

: ; ( --- )
\G Finish the current definition by adding a return to it, make it
    \G visible and leave compilation mode.
    LAST @ NAME> DUP HERE SWAP - SWAP CELL- ! \ Store definition size.
    LVARS @ IF
	$89 C, \ add LP+ instruction.
	LVARS @ C, \ And the number of local slots to pop
	0 LVARS ! 
    THEN
    $15 C, \ Add return instruction.
    [ \ Quit compilation state.
    ?CSP REVEAL
; IMMEDIATE

: (POSTPONE) ( --- )
\G Runtime for POSTPONE.
\ has inline argument.
  R> DUP @ SWAP CELL+ >R 
  DUP >NAME C@ 64 AND IF EXECUTE ELSE COMPILE, THEN 
;  

: : ( "ccc" --- )
\G Start a new definition, enter compilation mode.
  !CSP HEADER ] 0 LVARS ! ;

: EXIT ( ---)
\G Exit the definition that calls EXIT.
   LVARS @ IF $89 C, LVARS @ C, THEN  $15 C, ; IMMEDIATE

: ?PAIRS ( n1 n2 ---)
\G Check that n1 matches n2, throw an error if not, used to check
\G correct pairing of control structures.
    - -22 ?THROW ;

\ The following words are for control structures. They use the conditional
\ branch instruction (bit 1 is set). Unconditional branch is made by
\ opcode 28 (constant 0) and conditional branch.

: BEGIN ( --- x n )
\G Start a BEGIN UNTIL or BEGIN WHILE REPEAT loop.
  HERE 1 ; IMMEDIATE

: UNTIL ( x n --- )
\G Form a loop with matching BEGIN. 
\G Runtime: A flag is take from the stack
\G each time UNTIL is encountered and the loop iterates until it is nonzero. 
 1 ?PAIRS $05 C, HERE 2 + SWAP - H,  ; IMMEDIATE 

: IF    ( --- x 2)
\G Start an IF THEN or IF ELSE THEN construction. 
\G Runtime: At IF a flag is taken from
\G the stack and if it is true the part between IF and ELSE is executed,
\G otherwise the part between ELSE and THEN. If there is no ELSE, the part
\G between IF and THEN is executed only if flag is true.
  $04 C, HERE 2 ALLOT 2 ; IMMEDIATE

: THEN ( x n ---)
\G End an IF THEN or IF ELSE THEN construction.
 2 ?PAIRS HERE OVER - 2 -  SWAP H!U ; IMMEDIATE

: ELSE ( x1 n --- x2 n)
\G part of IF ELSE THEN construction.e
  $02 C, HERE 2 ALLOT 2 2SWAP POSTPONE THEN ; IMMEDIATE 

: WHILE  ( x1 n1  --- x2 n2 x1 n3 )
\G part of BEGIN WHILE REPEAT construction.
\G Runtime: At WHILE a flag is taken from the stack. If it is false,
\G  the program jumps out of the loop, otherwise the part between WHILE
\G  and REPEAT is executed and the loop iterates to BEGIN.
  POSTPONE IF 2SWAP ; IMMEDIATE

: REPEAT ( x1 n1 x2 n2 --- )
\G part of BEGIN WHILE REPEAT construction.
  1 ?PAIRS $03 C, HERE 2 + SWAP - H,  POSTPONE THEN ; IMMEDIATE

VARIABLE POCKET ( --- a-addr )
\G Buffer for S" strings that are interpreted.
  252 ALLOT-T

: '  ( "ccc" --- xt)
\G Find the word with name ccc and return its execution token.
  32 WORD UPPERCASE? FIND 0= -13 ?THROW ;

: ['] ( "ccc" ---)
\G Compile the execution token of the word with name ccc as a literal.
  ' LITERAL ; IMMEDIATE

: CHAR ( "ccc" --- c)
\G Return the first character of "ccc".
  BL WORD 1 + C@ ;

: [CHAR] ( "ccc" --- )
\G Compile the first character of "ccc" as a literal.
  CHAR LITERAL ; IMMEDIATE

: DO ( --- x n)
\G Start a DO LOOP.
\G Runtime: ( n1 n2 --- ) start a loop with initial count n2 and 
\G limit n1.
  $11 C, 'LEAVE @ HERE 0 'LEAVE ! 3 ; IMMEDIATE

: ?DO ( --- x )
\G Start a ?DO LOOP.
\G Runtime: ( n1 n2 --- ) start a loop with initial count n2 and
\G limit n1. Exit immediately if n1 = n2.  
  $10 C, 'LEAVE @ HERE 'LEAVE ! 0 H, HERE 3 ; IMMEDIATE

: LEAVE ( --- )
\G Runtime: leave the matching DO LOOP immediately.
\ All places where a leave address for the loop is needed are in a linked
\ list, starting with 'LEAVE variable, the other links in the cells where
\ the leave addresses will come.
  $14 C, HERE 'LEAVE @ DUP 0<> IF HERE SWAP - THEN H, 'LEAVE ! ; IMMEDIATE

: RESOLVE-LEAVE 
\G Resolve the references to the leave addresses of the loop.
          'LEAVE @ 
          BEGIN DUP WHILE DUP H@U DUP 0<> IF OVER SWAP - THEN HERE 2 PICK - 2 - ROT H!U REPEAT DROP ; 

: LOOP  ( x n --- )
\G End a DO LOOP.
\G Runtime: Add 1 to the count and if it is equal to the limit leave the loop.
  3 ?PAIRS $12 C, HERE 2 + SWAP -  H, RESOLVE-LEAVE 'LEAVE ! ; IMMEDIATE

: +LOOP ( x n --- )
\G End a DO +LOOP 
\G Runtime: ( n ---) Add n to the count and exit if this crosses the 
\G boundary between limit-1 and limit. 
  3 ?PAIRS $13 C, HERE 2 + SWAP -  H, RESOLVE-LEAVE 'LEAVE ! ; IMMEDIATE

: RECURSE ( --- )
\G Compile a call to the current (not yet finished) definition.
  LAST @ NAME> COMPILE, ; IMMEDIATE

: ."  ( "ccc<quote>" --- )
\G Parse a string delimited by " and compile the following runtime semantics.
\G Runtime: type that string.
   POSTPONE (.") 34 WORD C@ 1+ ALLOT ; IMMEDIATE 


: S"  ( "ccc<quote>" --- )
\G Parse a string delimited by " and compile the following runtime semantics.
\G Runtime: ( --- c-addr u) Return start address and length of that string. 
  STATE @ IF POSTPONE (S") 34 WORD C@ 1+ ALLOT
             ELSE 34 WORD COUNT POCKET PLACE POCKET COUNT THEN ; IMMEDIATE 

: ABORT"  ( "ccc<quote>" --- )
\G Parse a string delimited by " and compile the following runtime semantics.
\G Runtime: ( f --- ) if f is nonzero, print the string and abort program.
  POSTPONE (ABORT") 34 WORD C@ 1+ ALLOT  ; IMMEDIATE

: ABORT ( --- )
\G Abort unconditionally without a message.
 -1 THROW ;

: POSTPONE ( "ccc" --- )
\G Parse the next word delimited by spaces and compile the following runtime.
\G Runtime: depending on immediateness EXECUTE or compile the execution 
\G semantics of the parsed word.
  POSTPONE (POSTPONE) ' , ; IMMEDIATE

: IMMEDIATE ( --- )
\G Make last definition immediate, so that it will be executed even in
\G compilation mode.
  LAST @ DUP C@ 64 OR SWAP C! ;

: ( ( "ccc<rparen>" --- )
\G Comment till next ).
  41 PARSE DROP DROP ; IMMEDIATE  

: \
\G Comment till end of line. 
  SOURCE >IN ! DROP ; IMMEDIATE

M: >BODY ( xt --- a-addr)
\G Convert execution token to parameter field address.
  CELL+ ;

: (;CODE) ( --- )
\G Runtime for DOES>, exit calling definition and make last defined word
\G execute the calling definition after (;CODE) 
  R> 8 LSHIFT $01 OR LAST @ NAME> ! ;

: DOES>  ( --- )
\G Word that contains DOES> will change the behavior of the last created
\G word such that it pushes its parameter field address onto the stack
\G and then executes whatever comes after DOES> 
  POSTPONE (;CODE)  
  $18 C,      \ Compile the R> primitive, which is the first
              \ instruction that the defined word performs.                   
; IMMEDIATE

\ PART 10: TOP LEVEL OF INTERPRETER 

: ?STACK ( ---)
\G Check for stack over/underflow and abort with an error if needed.
  DEPTH DUP 0< -4 ?THROW 10000 > -3 ?THROW FP@ F0 @ U> -54 ?THROW ;

VARIABLE FNUMBER-VECTOR
: INTERPRET ( ---)
\G Interpret words from the current source until the input source is exhausted.
  BEGIN
   32 WORD UPPERCASE?  DUP C@ 
  WHILE
   FIND DUP 
   IF 
    -1 = STATE @ AND 
    IF
     COMPILE,
    ELSE 
     EXECUTE 
    THEN
   ELSE DROP
    FNUMBER-VECTOR @ IF
	FNUMBER-VECTOR @ EXECUTE	    
    ELSE
	0 \ Always fals, FP literal not handled.
    THEN
    0= IF
     NUMBER? 0= -13 ?THROW  
     DPL @ 1+ IF
      STATE @ IF SWAP LITERAL LITERAL THEN  
     ELSE  
      DROP STATE @ IF LITERAL THEN 
     THEN
    THEN
   THEN  ?STACK  
  REPEAT   DROP
;

: EVALUATE ( c-addr u --- )
\G Evaluate the string c-addr u as if it were typed on the terminal.
  SID @ >R SRC @ >R #SRC @ >R  >IN @ >R
  #SRC ! SRC ! 0 >IN ! -1 SID ! INTERPRET
  R> >IN ! R> #SRC ! R> SRC ! R> SID ! ;  

VARIABLE INCLUDE-BUFFER ( --- a-addr)
\G This is the buffer where the lines of included files are stored.
508 ALLOT-T

VARIABLE INCLUDE-POINTER ( --- a-addr)
\G This variable holds the address where the included line is stored.


: INCLUDE-FILE ( fid --- ) 
\G Read lines from the file identified by fid and interpret them.
\G INCLUDE and EVALUATE nest in arbitrary order.
  INCLUDE-POINTER @ >R SID @ >R SRC @ >R #SRC @ >R >IN @ >R
  LOADLINE @ >R
  #SRC @ INCLUDE-POINTER +! INCLUDE-POINTER @ SRC !
  SID ! 0 LOADLINE !
  BEGIN
   REFILL
  WHILE
   INTERPRET
  REPEAT
  R> LOADLINE !
  R> >IN ! R> #SRC ! R> SRC ! R> SID ! R> INCLUDE-POINTER ! 
;

: INCLUDED  ( c-addr u ---- )
\G Open the file with name c-addr u and interpret all lines contained in it.
  R/O OPEN-FILE -38 ?THROW 
  DUP >R INCLUDE-FILE
  R> CLOSE-FILE DROP      
; 

: INCLUDE ( "ccc")
\G Open the file with name "ccc" and interpret all lines contained in it.    
    BL WORD COUNT INCLUDED ;

: OK ( ---)
\G Load the file opened with OPEN    
    CURFILENAME C@ 0= -38 ?THROW
    CURFILENAME COUNT INCLUDED ;

: FLOAD ( "ccc" --- )
\G Make the file on the command line the current file and include it.
  OPEN OK ;

VARIABLE ERRORS ( --- a-addr)
\G This variable contains the head of a linked list of error messages.

: ERROR-SOURCE ( --- )
\G Print location of error source.
     SID @ 0 > IF
      ." in line " LOADLINE @ . 
     THEN
     HERE COUNT TYPE CR WARM
;  

VARIABLE NESTING
\G Variable to hold nesting for conditional compilation.

VARIABLE COLDSTARTUP
: QUIT ( --- )
\G This word resets the return stack, resets the compiler state, the include
\G buffer and then it reads and interprets terminal input.
  NESTING OFF    
  R0 @ RP! TO-STATE OFF [ 
  TIB SRC ! 0 SID !
  INCLUDE-BUFFER INCLUDE-POINTER !  
  BEGIN
      CURFILENAME C@ COLDSTARTUP @ AND COLDSTARTUP OFF IF
	  ['] OK \ Load any file on command line.
      ELSE
	  REFILL DROP ['] INTERPRET
      THEN CATCH DUP 0= IF 
	  DROP STATE @ 0= IF ." OK" THEN CR
   ELSE \ throw occured.
     DUP -2 = IF
      ERROR$ @ COUNT TYPE SPACE
     ELSE
      ERRORS @
      BEGIN DUP WHILE
       OVER OVER @ = IF 8 + COUNT TYPE SPACE ERROR-SOURCE THEN 4 + @   
      REPEAT DROP       
      ." Error " . 
     THEN ERROR-SOURCE
   THEN      
  0 UNTIL
;

: WARM ( ---) 
\G This word is called when an error occurs. Clears the stacks, sets
\G BASE to decimal, closes the files and resets the search order.
  R0 @ RP! S0 @ SP! F0 @ FP! L0 @ LP! DECIMAL 
  21 1 DO I CLOSE-FILE DROP LOOP  
  2 #ORDER !
  FORTH-WORDLIST CONTEXT ! 
  FORTH-WORDLIST CONTEXT CELL+ !
  FORTH-WORDLIST CURRENT ! 
  SETRAW
  0 HANDLER !
  QUIT ;

: COLD ( --- )
    \G The first word that is called at the start of Forth.
    CURFILENAME 1+ 80 0 ARG@ SWAP 1- C!
    CURFILENAME C@ IF
	COLDSTARTUP ON
    ELSE
	." Welcome to Embeddable Forth version 0.1" CR
	." Copyright 2025 L.C. Benschop MIT license" CR
    THEN
  WARM ;

END-CROSS

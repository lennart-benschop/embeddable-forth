\ FORTH graphics primitive (for term_bitmap library)
\ Copyright 2025-2026 L.C. Benschop Eindhoven, The Netherlands.
\ The program is released under the MIT license
\ There is NO WARRANTY.

$C0C0
2OPCODE GRAPHICS-PARAMS ( --- w h ncol)
\G Return the supported width, height and number of colours for graphics mode.

GRAPHICS-PARAMS 2DROP
[IF]
.( Graphics is supported)

$C0C1
2OPCODE GRAPHICS-MODE ( ---)
\G Enable graphics mode

$C0C2
2OPCODE TEXT-MODE ( ---)
\G Go back to text mode

$C0C3
2OPCODE REDRAW ( ---)
\G Redraw the graphics screen 

$C0C4
2OPCODE CLG ( ---)
\G Clear the graphics screen

$C0C5
2OPCODE SETFG-G ( n ---)
\G Set the graphics foreground colour

$C0C6
2OPCODE SETPEN ( fg bg fgmode bgmode ---)
\G Set the graphics foreground and background colours and modes.

$C0C7
2OPCODE PLOTDOT ( x y ---)
\G Plot a single dot at coordinates x, y

$C0C8
2OPCODE MOVETO ( x y ---)
\G Move current graphics position to x, y

$C0C9
2OPCODE LINETO ( x y ---)
\G Plot line from current graphics position to  x, y

$C0CA
2OPCODE TRIANGLE ( x2 y2 x3 y3 ---)
\G Plot a solid triangle between current position, the point x2, y2 and
\G the point x3, y3

$C0CB
2OPCODE CIRCLE ( x y r ---)
\G Plot a circle outline with x, y as centre and radius r

$C0CC
2OPCODE CIRCLE-F ( x y r ---)
\G Plot a solid circle with x, y as centre and radius r

$C0CD
2OPCODE PLOTTEXT ( c-addr u ---)
\G Plot text string c-addr u into graphics window at current position

$C0CE
2OPCODE GETDOT ( x y --- c)
\G Return colour of dot at position x, y

$C0CF
2OPCODE GETPOS ( --- x y)
\G Return current graphics position

[THEN]

: SETFG ( n ---0
\G Set text foreground colour.
  27 EMIT [CHAR] [ EMIT DUP 3 RSHIFT 1 AND 60 * SWAP 7 AND + 30 + 0 .R [CHAR] m EMIT ;

: SETBG ( n ---0
\G Set text background colour.
  27 EMIT [CHAR] [ EMIT DUP 3 RSHIFT 1 AND 60 * SWAP 7 AND + 40 + 0 .R [CHAR] m EMIT ;

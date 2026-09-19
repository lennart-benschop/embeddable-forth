\ Test for the put-bitmap and get-bitmap functions

GRAPHICS-MODE
CREATE PATTERN $55 C, $AA C, $55 C, $AA C, $55 C, $AA C, $55 C, $AA C,

PATTERN 0 0 8 8 PUT-BITMAP-MONO
\ Draw the checkerboard 8x8 pattern

CREATE SAVEBUF 64 ALLOT
SAVEBUF 0 0 8 8 GET-BITMAP
\ Copy it into a buffer

SAVEBUF 8 8 8 8 PUT-BITMAP
\ Draw at a different position.

REDRAW

BYE

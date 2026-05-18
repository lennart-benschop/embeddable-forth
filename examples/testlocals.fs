\ tests for modern local variable support.
\ Stack-comment-like notiation delimieted by {: :} specifies local variables,
\ iniitialized from stack. The act like VALUEs and can be assigned to with TO.
\ Legacy ANS locals are not supported: (LOCAL) and LOCALS| 


: TEST1 {: a b c -- d :} \ a b and c are local variables, everyhing after -- is ignored
  a c * b - ;

CR .( TEST1 should print 155: )
12 13 14 TEST1 .

: TEST2 {: F: a F: b c -- F: d :} \ F: denotes vars of type FP
  A B F* C S>F F+ ;

CR .( TEST2 should print 170.0: )
12e 13e 14 TEST2 F.

: TEST3 {: F^ x -- F: y :} \ x is a pointer to the stored local FP variable.
  x f@ ;

CR .( TEST3 should print 15.0: )
15e TEST3 F.

: TEST4 {: a b c -- d e :}
  a +to b
  b +to c
  c
  19 to c c ;

CR .( TEST4 should print 19 63: )
20 21 22 TEST4 . .

: TEST5 {: a b c -- d e :}
  a 2*  b 4 * {: d e :} \ A second block of local vars d and e
  \ a . b . c . d . e . CR
  d e + a + b +  c ;

CR .( TEST5 should print 11 2649: )
123 456 11 TEST5 . .

: TEST6 {: a b c -- d F: e :}
  0e 0e {: F: d F: e :} \ A second block of local vars d and e

  a b + c -

  154 12.6e {: period F: angle :} \ A third block of local vars period and angle.

  period 0 DO
      angle +to d
      d to e
  LOOP 
  e
;

CR .( TEST6 should print 1940.4 6:)
1 8 3 TEST6 f. . 

\ ' test6 100 dump

CR BYE

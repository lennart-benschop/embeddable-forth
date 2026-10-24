\ Read teh current clock

: HOURS 3600 * ;
\ 2 HOURS CONSTANT TIMEZONE \ Summer time central Europe
1 HOURS CONSTANT TIMEZONE   \ Winter time central Europe

: UNIX-TIME ( --- u )
  CURRENT-TIME 1000000. D/MOD DROP ROT ROT 2DROP ;


: LOCAL-TIME ( --- u)
  UNIX-TIME TIMEZONE + ;

: TIME>DAYTIME ( u --- ndays nsecs)
  86400 /MOD SWAP ;

: SECS>HMS ( u --- u1 u2 u3)
  60 /MOD 60 /MOD ;

: DAY>YMD ( u --- u1 u2 u3)
\ Convert Julian date to Y/M/D according to https://en.wikipedia.org/wiki/Julian_day
  2440587 + \ Convert 1970 based day to Julian day
  1401 + 14 + {: f :} \ Just use Julian algorithm to correct for timeframe 1901..2099
  f 4 * 3 + {: e :}
  e 1461 MOD 4 / {: g :}
  5 g * 2 + {: h :}
  h 153 MOD 5 / 1 + ( day )
  h 153 / 2 + 12 MOD 1 + ( month )
  e 1461 / 4716 -  14 2 PICK - 12 / + ( year)
;


: SHOW-TIME
   LOCAL-TIME TIME>DAYTIME SWAP DAY>YMD
   0  <# # # # # #> TYPE '-' EMIT
   0  <# # # #> TYPE '-' EMIT
   0  <# # # #> TYPE SPACE
   SECS>HMS
   0 <# # # #> TYPE ':' EMIT
   0 <# # # #> TYPE ':' EMIT
   0 <# # # #> TYPE SPACE ;

